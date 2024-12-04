#if DOTNET_LIB
module FSharp.Logf
#else
module Fable.FSharp.Logf
#endif
open FSharp.Core.Printf
open System
open System.Text
open System.Text.RegularExpressions
open System.Collections.Generic
#if DOTNET_LIB
open Microsoft.Extensions.Logging
open BlackFox.MasterOfFoo
#else
open Fable.Microsoft.Extensions.Logging
#endif

type StringFormat<'T, 'Result, 'Tuple> = Format<'T, unit, string, 'Result, 'Tuple>

// see: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/plaintext-formatting#format-specifiers-for-printf
// TODO: This probably isn't quite perfect -- there are likely still some obscure corner cases that this doesn't
// handle correctly. For anyone looking into this, you could prod it with things that aren't quite valid printf format
// specifiers and make sure that it doesn't identify them as such. For example: I suspect flags are supposed to only
// appear once (+++++ is probably not valid). Another example: flags, width, precision can only be there for certain
// format types, like %f, but not others, like %s. Finally, not all a-zA-Z chars are format types. For example, %z
// shouldn't be recognized as a format specifier.
// These edge cases are getting pretty out there though, so I don't expect them to come up often... If you run into one,
// filing an issue would be great!  
let printfFmtSpecPattern =
    """%"""
    // flags
    + """(0|-|\+| )*"""
    // width
    + """[0-9]*"""
    // precision
    + """(\.\d+)?"""
    // type (interpolated string holes are special -- they show up as "%P()" -- note the extra parens)
    + """(P\(\)|[a-zA-Z])"""

let netMsgHolePattern =
    """(?<start>"""
        + """\{(?<argName>@?"""
        + """[a-zA-Z0-9_]+)"""
    + """)"""
    + """(?<fmt>"""
        + """(,[^:\}]+)?"""
        + """(:[^\}]+)?"""
    + """)"""
    + """(?<end>"""
        + """\}"""
    + """)"""

#if DOTNET_LIB
// type private LogfEnv<'Result>(continuation: (LogLevel -> EventId option * Exception option * string * obj[] -> 'Result), logger: ILogger, logLevel: LogLevel, eventId: EventId option, exn: Exception option) =
type private LogfEnv<'Result>(continuation: string -> obj[] -> 'Result) =
    inherit PrintfEnv<unit, string, 'Result>()
    let msgBuf = StringBuilder()
    let mutable lastArg : PrintableElement option = None
    static let logFormatSpecifierRegex = Regex("\A" + netMsgHolePattern)
    
    let args = new System.Collections.Generic.List<obj>()
    // We actually want to override PrintfEnv.Finalize: unit -> unit, but that conflicts with Object.Finalize: unit -> unit,
    // but the F# compiler doesn't give us a way to disambiguate this. So, to work around, we make it generic and
    // override in this superclass, then make a child class which specifies 'Unit as the actual `unit` type.
    // Annoying, but necessary.
    override this.Finalize() =
        match lastArg with
        | Some lastArg -> msgBuf.Append (lastArg.FormatAsPrintF ()) |> ignore
        | None -> ()
        lastArg <- None
        continuation (msgBuf.ToString ()) (args.ToArray ())
    
    member this.TryTranslatePrintfSpecToNetFormatSpec (printfSpec: FormatSpecifier) =
        let flags = printfSpec.Flags
        let flags =
            // .NET format specifiers have no way to left-justify (aka right-pad) with zeros; spaces are hardcoded
            // as the padding character. Do the next best thing instead, which is to left-justify with spaces.
            // Right-justify with zeros is possible because we can use format strings like {arg:000.0} to pad to 5
            // chars. The reason the same approach doesn't work for left-justify with zeros is left as an exercise for
            // the reader
            if flags.HasFlag FormatFlags.LeftJustify && flags.HasFlag FormatFlags.PadWithZeros then
                flags &&& ~~~FormatFlags.PadWithZeros
            else flags
        match printfSpec.TypeChar with
        | 'B' -> Some ":B"
        | 'x' -> Some ":x"
        | 'X' -> Some ":X"
        // It seems that this 'M' branch is technically unnecessary, because it equivalent to
        // System.Double.ToString("G"), and System.Double.ToString() I think uses "G" by default. So I guess we could
        // technically just "{MyDecimal}" as the formatter rather than "{MyDecimal:G}" (in fact -- try commenting this
        // case out, and observe that the corresponding test case still passes)... But I'm still choosing to defensively
        // emit :G in case there's some corner case I don't know about.
        | 'M' -> Some ":G"
        | 'e' -> Some ":0.000000e+000"
        | 'E' -> Some ":0.000000E+000"
        | 'f' when printfSpec.IsWidthSpecified || printfSpec.IsPrecisionSpecified ->
            let sb = StringBuilder(1 + (max 0 printfSpec.Width) + (max 0 printfSpec.Precision))
            
            let width, precision =
                if not printfSpec.IsWidthSpecified && printfSpec.Precision = 0 then
                    // special case for %0.0f
                    Some 1, 0
                else (if printfSpec.IsWidthSpecified then Some printfSpec.Width else None), (if printfSpec.IsPrecisionSpecified then printfSpec.Precision else 6)
            
            let p = precision
            
            match width, (flags.HasFlag FormatFlags.PadWithZeros) with
            | Some w, false ->
                sb.Append ',' |> ignore
                if (flags.HasFlag FormatFlags.LeftJustify) then
                    sb.Append '-' |> ignore
                sb.Append w |> ignore
            | _ ->
                ()
            
            let buildSection lpadZeros =
                let lpadZeros = max lpadZeros 1
                for _ in 0 .. (lpadZeros - 1) do
                    sb.Append '0' |> ignore
                sb.Append '.' |> ignore
                
                for _ in 0 .. p - 1 do
                    sb.Append '0' |> ignore
            
            let lpadZeros =
                match width, flags.HasFlag FormatFlags.PadWithZeros, precision with
                | Some w, true, p -> w - p - 1 |> max 1
                | _ -> 1
            
            // space and plus are mutually exclusive; trying to use both gives a compile error
            let posSign =
                if flags.HasFlag FormatFlags.PlusForPositives then Some '+'
                elif flags.HasFlag FormatFlags.SpaceForPositives then Some ' '
                else None
            
            sb.Append ':' |> ignore
            posSign |> Option.iter (sb.Append >> ignore)
            buildSection (if Option.isSome posSign then lpadZeros - 1 else lpadZeros)
            
            sb.Append ";-" |> ignore
            buildSection (lpadZeros - 1)
            
            // Work around https://github.com/dotnet/runtime/issues/70460 by printing both +0.0 and -0.0 as +0 (and
            // similarly for SpaceForPositives). Better than getting -+0.
            match posSign with
            | Some s ->
                sb.Append ';' |> ignore
                sb.Append s |> ignore
                buildSection (lpadZeros - 1)
            | None -> ()
            
            Some (sb.ToString())
        | _ -> None
    
    override this.Write (s: PrintableElement) =
        if s.ElementType = PrintableElementType.FromFormatSpecifier then
            match lastArg with
            | Some lastArg ->
                // now we know the prev arg should be baked into the message template
                msgBuf.Append (lastArg.FormatAsPrintF ()) |> ignore
            | None -> ()
            lastArg <- Some s
        else
            let sAsStr = s.Value :?> string
            
            let sValue =
                match lastArg with
                | Some lastArg ->
                    let m = logFormatSpecifierRegex.Match sAsStr
                    
                    if m.Success then
                        let netFormatSpec =
                            lastArg.Specifier
                            |> Option.bind this.TryTranslatePrintfSpecToNetFormatSpec
                        
                        // now we know the prev arg should be a message param
                        args.Add lastArg.Value
                        
                        match netFormatSpec with
                        | Some fmt ->
                            let sb = StringBuilder()
                            let endI = m.Groups["end"].Index
                            sb  .Append(m.Groups["start"].Value)
                                .Append(fmt)
                                .Append(sAsStr, endI, sAsStr.Length - endI)
                                |> ignore
                            sb.ToString()
                        | None -> sAsStr
                    else
                        // now we know the prev arg should be baked into the message template
                        msgBuf.Append (lastArg.FormatAsPrintF ()) |> ignore
                        sAsStr
                | None -> s.Value :?> string
            msgBuf.Append sValue |> ignore
            lastArg <- None
    
    override this.WriteT(s : string) =
        msgBuf.Append("{arg")
            .Append(args.Count)
            .Append('}') |> ignore
        args.Add s

// This regex matches either a valid format specifier (i.e. things like "%s{foo}"), and also matches lone curly braces.
// Valid format specifiers will be captured by the named capture group "a", and lone curly braces will be captured
// by the named capture group "b". Later, using the replacement pattern "${a}${b}${b}" causes any occurrences of "a"
// (valid format specifiers) to be placed back into the string as-is, while occurrences of "b" will be doubled (having
// the effect of escaping those lone curly braces).
// Examples:
//  * Input: "%s{foo}"
//      * 1st match: "a" = "%s{foo}", "b" = "", "${a}${b}${b}" = "$s{foo}"
//      * Output: "$s{foo}"
//  * Input: "foo{bar}"
//      * 1st match: "a" = "", "b" = "{", "${a}${b}${b}" = "{{"
//      * 2nd match: "a" = "", "b" = "}", "${a}${b}${b}" = "}}"
//      * Output: foo{{bar}}
let bracketGroupOrUnpairedBracketRegex = Regex("""(?<a>""" + printfFmtSpecPattern + netMsgHolePattern + """)|(?<b>[\{\}])""")

//  [FS0057] Experimental library feature, requires '--langversion:preview'. This warning can be disabled using '--nowarn:57' or '#nowarn "57"'.
#nowarn "57"
let escapeUnpairedBrackets (format: Format<'Printer, 'State, 'Residue, 'Result, 'Tuple>) : Format<'Printer, 'State, 'Residue, 'Result, 'Tuple> =
    let fmtValue' = bracketGroupOrUnpairedBracketRegex.Replace (format.Value, "${a}${b}${b}")
    Format<'Printer, 'State, 'Residue, 'Result, 'Tuple>(fmtValue', format.Captures, format.CaptureTypes)

let klogf (continuation: string -> obj[] -> 'Result) (format: Format<'T, unit, string, 'Result, 'Tuple>) : 'T =
    let fmt' = escapeUnpairedBrackets format
    doPrintfFromEnv fmt' (LogfEnv(continuation))

let logf (logger: ILogger) logLevel format =
    klogf (fun msg args -> logger.Log(logLevel, msg, args)) format
    
let vlogf (logger: ILogger) (logLevel: LogLevel) (eventId: EventId) (exn: Exception) format =
    klogf (fun msg args -> logger.Log (logLevel, eventId, exn, msg, args)) format

let elogf (logger: ILogger) (logLevel: LogLevel) (exn: Exception) format =
    klogf (fun msg args -> logger.Log(logLevel, exn, msg, args)) format

#else

// matches a printf-style format specifier (like %s or %+6.4d) followed immediately by a log message param specifier
// (like {myValue}) matches a log message param specifier (like {myValue}) coming immediately after a printf-style
// format specifier (like %s or %+6.4d)
let logMsgParamNameRegex =
    Regex("""(?<printfFmt>""" + printfFmtSpecPattern + """)(""" + netMsgHolePattern + """)?""", RegexOptions.ECMAScript)

// For the JS implementation, we use a shim which doesn't pass true structured parameters, and flattens everything to a
// string by the time the logger recieves it (because PrintfEnv doesn't work in Fable).
// First, however, we have to strip any log message param specifiers or they would show up in the console output
// unintentionally. We also must do a little bit of magic to handle .NET-style format specifiers as well (see
// mapReplacementsDynamic)

// Scans through a format literal (the first parameter to a printf-style function), removing parameter names (i.e.
// changing "%s{foo}" to "%s", and collecting all custom .Net-style formatters (i.e. from "%f{foo:#.#}" we grab ":#.#")
let processLogMsgParams (format: Format<'Printer, 'State, 'Residue, 'Result, 'Tuple>) : string option list * Format<'Printer, 'State, 'Residue, 'Result, 'Tuple> =
    let paramReplacementFmt = System.Collections.Generic.List<string option>()
    let result: string =
        logMsgParamNameRegex.Replace (format.Value, (fun m ->
            let replacement =
                m.Groups["fmt"]
                |> Option.ofObj
                |> Option.map (fun g -> g.Value)
                |> Option.filter (String.IsNullOrWhiteSpace >> not)
            paramReplacementFmt.Add replacement
            m.Groups["printfFmt"].Value
        ))
    // TODO: amend to include .Captures and .CaptureTypes - apparently whatever version of Fable I'm using doesn't provide that overload
    // (new Format<'T, unit, string, unit>(logMsgParamNameRegex.Replace(format.Value, "$1")))
    Seq.toList paramReplacementFmt, (new Format<'Printer, 'State, 'Residue, 'Result, 'Tuple>(result))

// Takes a list of .Net custom formatters (like ":#.#"), and a constructed printf function, and constructs a new
// function (wrapping around the printf function) but which applies these custom formatters to the appropriate
// params before passing it through. It's possible to write a version of this that works in .NET (leveraging reflection),
// but this version only works in Fable.
let rec mapReplacementsDynamic (fmts: string option list) (f: obj) : obj =
    // Since Fable = Javascript, we can just "reinterpret" cast the function. This approach wouldn't work on the .NET
    // runtime because you can't runtime-cast a (int -> string) to a (object -> object).
    let f' = f :?> obj -> obj
    match fmts with
    | Some(hd)::tl ->
        // Again, since we're in Javascript land we don't actually have to make this wrapper lambda have the same
        // parameter and return type as f. Just tell the compiler, "trust me, this works."
        (fun (x: obj) ->
            let fmt = "{0" + hd + "}"
            let x' = String.Format(fmt, x)
            let y = f' x'
            let cont = mapReplacementsDynamic tl y
            cont) :> obj
    | None::tl ->
        mapReplacementsDynamic tl f'
    | [] ->
        f

let klogf (continuation: string -> obj[] -> 'Result) (format: Format<'T, unit, string, 'Result, 'Tuple>) : 'T =
    let replacements, processedFmt = processLogMsgParams format
    let f =
        Printf.ksprintf (fun msg -> continuation msg [||]) processedFmt
        |> unbox<'T>
    let f' = mapReplacementsDynamic replacements f
    f' |> unbox<'T>

// Use a fallback implementation where we never attempt to provide structured logging parameters and just flatten
// everything to a string and print it, since BlackFox.MasterOfFoo uses kinds of reflection that don't work in Fable
let logf (logger: ILogger) logLevel (format: StringFormat<'T, unit, 'Tuple>) : 'T =
    klogf
        (fun msg _ -> logger.Log (logLevel, EventId(0), null, null, Func<_,_,_>(fun _ _ -> msg)))
        format
        
let vlogf (logger: ILogger) (logLevel: LogLevel) (eventId: EventId) (exn: Exception) format =
    klogf (fun msg args -> logger.Log (logLevel, eventId, null, exn, Func<_,_,_>(fun _ _ -> msg))) format
        
let elogf (logger: ILogger) (logLevel: LogLevel) (exn: Exception) (format: StringFormat<'T, unit, 'Tuple>) : 'T =
    klogf
        (fun msg _ -> logger.Log (logLevel, EventId(0), null, exn, Func<_,_,_>(fun _ _ -> msg)))
        format

#endif

let logft logger format = logf logger LogLevel.Trace format
let vlogft logger eventId exn format = vlogf logger LogLevel.Trace eventId exn format
let logfd logger format = logf logger LogLevel.Debug format
let vlogfd logger eventId exn format = vlogf logger LogLevel.Debug eventId exn format
let logfi logger format = logf logger LogLevel.Information format
let vlogfi logger eventId exn format = vlogf logger LogLevel.Information eventId exn format
let logfw logger format = logf logger LogLevel.Warning format
let vlogfw logger eventId exn format = vlogf logger LogLevel.Warning eventId exn format
let elogfw logger exn format = elogf logger LogLevel.Warning exn format
let logfe logger format = logf logger LogLevel.Error format
let vlogfe logger eventId exn format = vlogf logger LogLevel.Error eventId exn format
let elogfe logger exn format = elogf logger LogLevel.Error exn format
let logfc logger format = logf logger LogLevel.Critical format
let vlogfc logger eventId exn format = vlogf logger LogLevel.Critical eventId exn format
let elogfc logger exn format = elogf logger LogLevel.Critical exn format
let logfn logger format = logf logger LogLevel.None format
