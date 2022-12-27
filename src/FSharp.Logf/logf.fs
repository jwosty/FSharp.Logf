module FSharp.Logf
open System
open System.Text
open System.Text.RegularExpressions
#if !FABLE_COMPILER
open Microsoft.Extensions.Logging
open BlackFox.MasterOfFoo
#endif

// TODO: write tests

#if FABLE_COMPILER
/// Fable proxy/stub for Microsoft.Extensions.Logging.Logger
type ILogger = ILogger
type LogLevel = | Trace = 0 | Debug = 1 | Information = 2 | Warning = 3 | Error = 4 | Critical = 5 | None = 6
#endif
[<Sealed; AbstractClass>]
type Log private() =
    static let mutable log: ILogger option = None
    static member Default
        with get () =
            match log with
            | Some l -> l
            | None -> raise (InvalidOperationException("Logger not initialized"))
        and set l =
            match log with
            | Some _ -> raise (InvalidOperationException("Logger already initialized"))
            | None -> log <- Some l

#if FABLE_COMPILER
Log.Default <- ILogger
#endif

#if !FABLE_COMPILER
type private LogfEnvParent<'Unit>(logger: ILogger, logLevel: LogLevel, ?exn: Exception) =
    inherit PrintfEnv<unit, string, 'Unit>()
    let msgBuf = StringBuilder()
    //let mutable justEmittedArgument = false
    let mutable lastArg : PrintableElement option = None
    let logFormatSpecifierRegex = Regex("""\A{[^}]+}""")
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
    override this.Write (s: PrintableElement) =
        if s.ElementType = PrintableElementType.FromFormatSpecifier then
            match lastArg with
            | Some lastArg ->
                // now we know the prev arg should be baked into the message template
                msgBuf.Append (lastArg.FormatAsPrintF ()) |> ignore
            | None -> ()
            lastArg <- Some s
        else
            match lastArg with
            | Some lastArg ->
                if logFormatSpecifierRegex.IsMatch (s.Value :?> string) then
                    // now we know the prev arg should be a message param
                    args.Add lastArg.Value
                else
                    // now we know the prev arg should be baked into the message template
                    msgBuf.Append (lastArg.FormatAsPrintF ()) |> ignore
            | None -> ()
            msgBuf.Append (s.Value :?> string) |> ignore
            lastArg <- None
    
    override this.WriteT(s : string) =
        msgBuf.Append("{arg")
            .Append(args.Count)
            .Append('}') |> ignore
        args.Add s

type private LogfEnv(logger, logLevel, ?exn) =
    inherit LogfEnvParent<unit>(logger, logLevel, ?exn = exn)


let logf logger logLevel format =
    doPrintfFromEnv format (LogfEnv(logger, logLevel))
let elogf logger logLevel exn format =
    doPrintfFromEnv format (LogfEnv(logger, logLevel, exn))

#else

// matches a printf-style format specifier (like %s or %+6.4d) followed
// immediately by a log message param specifier (like {myValue}) matches a log
// message param specifier (like {myValue}) coming immediately after a
// printf-style format specifier (like %s or %+6.4d)
let private logMsgParamNameRegex =
    new Regex("""(%[0\-+ ]?\d*(\.\d+)?[a-zA-Z])(\{[^}]+\})""", RegexOptions.ECMAScript)

// For the JS implementation, just print to console. First, however, we have to
// strip any log message param specifiers or they would show up in the console
// output unintentionally.

let inline private stripLogMsgParamNames (format: Format<'T, unit, string, unit>) =
    // TODO: amend to include .Captures and .CaptureTypes - apparently whatever version of Fable I'm using doesn't provide that overload
    (new Format<'T, unit, string, unit>(logMsgParamNameRegex.Replace(format.Value, "$1")))

let private printToConsole logLevel (m: obj) =
    match logLevel with
    | LogLevel.Critical | LogLevel.Error -> Fable.Core.JS.console.error m
    | LogLevel.Warning -> Fable.Core.JS.console.warn m
    | LogLevel.Information -> Fable.Core.JS.console.info m
    | LogLevel.Trace -> Fable.Core.JS.console.debug (string m)
    | _ -> Fable.Core.JS.console.log m

let logf (_: ILogger) logLevel (format: Format<'T, unit, string, unit>) =
    Printf.ksprintf (printToConsole logLevel) (stripLogMsgParamNames format)
let elogf (_: ILogger) logLevel (exn: Exception) (format: Format<'T, unit, string, unit>) =
    Printf.ksprintf (fun s ->
        printToConsole logLevel s
        printToConsole logLevel exn
    ) format

#endif

let logft logger format = logf logger LogLevel.Trace format
let logfd logger format = logf logger LogLevel.Debug format
let logfi logger format = logf logger LogLevel.Information format
let logfw logger format = logf logger LogLevel.Warning format
let elogfw logger exn format = elogf logger LogLevel.Error exn format
let logfe logger format = logf logger LogLevel.Error format
let elogfe logger exn format = elogf logger LogLevel.Error exn format
let logfc logger format = logf logger LogLevel.Critical format
let elogfc logger exn format = elogf logger LogLevel.Critical exn format
let logfn logger format = logf logger LogLevel.None format
