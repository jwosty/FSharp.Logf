#if DOTNET_LIB
module FSharp.Logf
#else
module Fable.FSharp.Logf
#endif
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

// TODO: write tests

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
    + """(0|-|\+)*"""   // flags
    + """[0-9]*"""      // width
    + """(\.\d+)?"""    // precision
    + """[a-zA-Z]"""    // type

let netMsgHolePattern =
    """(?<start>"""
        + """{@?"""
        + """[a-zA-Z0-9_]+"""
    + """)"""
    + """(?<fmt>"""
        + """(,[^:\}]+)?"""
        + """(:[^\}]+)?"""
    + """)"""
    + """(?<end>"""
        + """}"""
    + """)"""

#if DOTNET_LIB
type private LogfEnvParent<'Unit>(logger: ILogger, logLevel: LogLevel, ?exn: Exception) =
    inherit PrintfEnv<unit, string, 'Unit>()
    let msgBuf = StringBuilder()
    let mutable lastArg : PrintableElement option = None
    static let logFormatSpecifierRegex = Regex("""\A""" + netMsgHolePattern)
    
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
        match exn with
        | Some exn ->
            logger.Log(logLevel, exn, msgBuf.ToString (), args.ToArray ())
        | None ->
            logger.Log(logLevel, msgBuf.ToString (), args.ToArray ())
        Unchecked.defaultof<'Unit>
    
    member this.TryTranslatePrintfSpecToNetFormatSpec (printfSpec: FormatSpecifier) =
        System.Diagnostics.Debugger.Break ()
        match printfSpec.TypeChar with
        | 'x' -> Some ":x"
        | 'X' -> Some ":X"
        | 'f' when printfSpec.IsWidthSpecified || printfSpec.IsPrecisionSpecified ->
            let sb = StringBuilder(1 + (max 0 printfSpec.Width) + (max 0 printfSpec.Precision))
            
            let width, precision =
                if not printfSpec.IsWidthSpecified && printfSpec.Precision = 0 then
                    // special case for %0.0f
                    Some 1, Some 0
                else (if printfSpec.IsWidthSpecified then Some printfSpec.Width else None), (if printfSpec.IsPrecisionSpecified then Some printfSpec.Precision else None)
            
            let buildSection () =
                width |> Option.iter (fun w ->
                    for _ in 0 .. w - 1 do
                        sb.Append '0' |> ignore)
                
                sb.Append '.' |> ignore
                
                precision |> Option.iter (fun p ->
                    for _ in 0 .. p - 1 do
                        sb.Append '0' |> ignore)
            
            sb.Append ':' |> ignore
            
            match printfSpec.Flags with
            | FormatFlags.PlusForPositives ->
                sb.Append '+' |> ignore
                buildSection ()
                sb.Append ";-" |> ignore
            | _ -> ()
            
            buildSection ()
            
            Some (sb.ToString())
        | _ -> None
    
    override this.Write (s: PrintableElement) =
        System.Diagnostics.Debugger.Break ()
        if s.ElementType = PrintableElementType.FromFormatSpecifier then
            match lastArg with
            | Some lastArg ->
                // now we know the prev arg should be baked into the message template
                msgBuf.Append (lastArg.FormatAsPrintF ()) |> ignore
            | None -> ()
            lastArg <- Some s
        else
            let sValue =
                match lastArg with
                | Some lastArg ->
                    let m = logFormatSpecifierRegex.Match (s.Value :?> string)
                    
                    if m.Success then
                        let netFormatSpec =
                            lastArg.Specifier
                            |> Option.bind this.TryTranslatePrintfSpecToNetFormatSpec
                        
                        // now we know the prev arg should be a message param
                        args.Add lastArg.Value
                        
                        match netFormatSpec with
                        | Some fmt ->
                            m.Groups["start"].Value + fmt + (s.Value :?> string).Substring(m.Groups["end"].Index)
                            
                        | None -> s.Value :?> string
                    else
                        // now we know the prev arg should be baked into the message template
                        msgBuf.Append (lastArg.FormatAsPrintF ()) |> ignore
                        s.Value :?> string
                | None -> s.Value :?> string
            msgBuf.Append sValue |> ignore
            lastArg <- None
    
    override this.WriteT(s : string) =
        msgBuf.Append("{arg")
            .Append(args.Count)
            .Append('}') |> ignore
        args.Add s

type private LogfEnv(logger, logLevel, ?exn) =
    inherit LogfEnvParent<unit>(logger, logLevel, ?exn = exn)

// This regex matches either a valid format specifier (i.e. things like "%s{foo}"), and also matches lone curly braces.
// Valid format specifiers will be captured by the named capture group "a", and lone curly braces will be captured
// by the named capture group "b". Later, using the replacement pattern "${a}${b}${b}" causes any occurrences of "a"
// (valid format specifiers) to be placed back into the string as-is, while occurrences of "b" will be doubled (having
// the effect of escaping those lone curly braces.
// Examples:
//  * Input: "%s{foo}"
//      * 1st match: "a" = "%s{foo}", "b" = "", "${a}${b}${b}" = "$s{foo}"
//      * Output: "$s{foo}"
//  * Input: "foo{bar}"
//      * 1st match: "a" = "", "b" = "{", "${a}${b}${b}" = "{{"
//      * 2nd match: "a" = "", "b" = "}", "${a}${b}${b}" = "}}"
//      * Output: foo{{bar}}
let bracketGroupOrUnpairedBracketRegex = Regex("""(?<a>""" + printfFmtSpecPattern + netMsgHolePattern + """)|(?<b>[\{\}])""")

let escapeUnpairedBrackets (format: Format<'T, unit, string, unit>) =
    let fmtValue' = bracketGroupOrUnpairedBracketRegex.Replace (format.Value, "${a}${b}${b}")
    Format<'T, unit, string, unit>(fmtValue')

let logf logger logLevel format =
    doPrintfFromEnv (escapeUnpairedBrackets format) (LogfEnv(logger, logLevel))
let elogf logger logLevel exn format =
    doPrintfFromEnv (escapeUnpairedBrackets format) (LogfEnv(logger, logLevel, exn))

#else

// matches a printf-style format specifier (like %s or %+6.4d) followed immediately by a log message param specifier
// (like {myValue}) matches a log message param specifier (like {myValue}) coming immediately after a printf-style
// format specifier (like %s or %+6.4d)
let logMsgParamNameRegex =
    Regex("""(""" + printfFmtSpecPattern + """)(\{[a-zA-Z0-9_]+\})""", RegexOptions.ECMAScript)

// For the JS implementation, just print to console. First, however, we have to strip any log message param specifiers
// or they would show up in the console output unintentionally.

let inline stripLogMsgParamNames (format: Format<'T, unit, string, unit>) =
    // TODO: amend to include .Captures and .CaptureTypes - apparently whatever version of Fable I'm using doesn't provide that overload
    (new Format<'T, unit, string, unit>(logMsgParamNameRegex.Replace(format.Value, "$1")))

// Use a fallback implementation where we never attempt to provide structured logging parameters and just flatten
// everything to a string and print it, since BlackFox.MasterOfFoo uses kinds of reflection that don't work in Fable
let logf (logger: ILogger) logLevel (format: Format<'T, unit, string, unit>) =
    Printf.ksprintf (fun x -> logger.Log (logLevel, EventId(0), null, null, Func<_,_,_>(fun s e -> x))) (stripLogMsgParamNames format)
let elogf (logger: ILogger) (logLevel: LogLevel) (exn: Exception) (format: Format<'T, unit, string, unit>) =
    Printf.ksprintf (fun (x:string) ->
        logger.Log (logLevel, EventId(0), null, exn, Func<_,_,_>(fun _ _ -> x))
    ) (stripLogMsgParamNames format)

#endif

/// <summary>
/// 
/// </summary>
/// <param name="logger"></param>
/// <param name="format"></param>
let logft logger format = logf logger LogLevel.Trace format
let logfd logger format = logf logger LogLevel.Debug format
let logfi logger format = logf logger LogLevel.Information format
let logfw logger format = logf logger LogLevel.Warning format
let elogfw logger exn format = elogf logger LogLevel.Warning exn format
let logfe logger format = logf logger LogLevel.Error format
let elogfe logger exn format = elogf logger LogLevel.Error exn format
let logfc logger format = logf logger LogLevel.Critical format
let elogfc logger exn format = elogf logger LogLevel.Critical exn format
let logfn logger format = logf logger LogLevel.None format
