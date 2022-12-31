namespace FSharp.Logf.Suave
open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Logging


/// <summary>
///     Adapts a Suave <see cref="Suave.Logging.Logger" /> to the Microsoft
///     <see cref="T:Microsoft.Extensions.Logging.ILogger" /> interface.
/// </summary>
type MsSuaveLoggerAdapter(ml: Microsoft.Extensions.Logging.ILogger) =
    let sToMLogLevel = function
        | Suave.Logging.LogLevel.Verbose -> Microsoft.Extensions.Logging.LogLevel.Trace
        | Suave.Logging.LogLevel.Debug -> Microsoft.Extensions.Logging.LogLevel.Debug
        | Suave.Logging.LogLevel.Info -> Microsoft.Extensions.Logging.LogLevel.Information
        | Suave.Logging.LogLevel.Warn -> Microsoft.Extensions.Logging.LogLevel.Warning
        | Suave.Logging.LogLevel.Error -> Microsoft.Extensions.Logging.LogLevel.Error
        | Suave.Logging.LogLevel.Fatal | _ -> Microsoft.Extensions.Logging.LogLevel.Critical

    // matches the FOO part of things like {FOO} and {FOO:#.#}
    let fmtParamRegex = Regex """\{([^}^:]+)(:[^}]*)?\}"""
    let getFmtParamNamesInOrder fmtStr =
        fmtParamRegex.Matches fmtStr |> Seq.map (fun m -> m.Groups.[1].Value)

    interface Suave.Logging.Logger with
        override _.name = [||]
        override this.logWithAck sLogLevel msgFactory = async {
            return (this :> Suave.Logging.Logger).log sLogLevel msgFactory
        }
        override _.log sLogLevel msgFactory =
            try
                let mLogLevel = sToMLogLevel sLogLevel
                let msg = msgFactory sLogLevel
                let exn =
                    match msg.fields |> Map.tryFind Suave.Logging.Literals.FieldExnKey with
                    | Some (:? exn as e) -> e
                    | _ -> null

                match msg.value with
                | Suave.Logging.PointValue.Event fmt ->
                    let fmtParams =
                        getFmtParamNamesInOrder fmt
                        // Be liberal in what we accept and conservative in what we emit: handle when an argument is
                        // in the message, but whose value is not given. Just turn it into null in that case so that
                        // *something* gets logged.
                        |> Seq.map (fun fmtArg -> msg.fields |> Map.tryFind fmtArg |> Option.toObj) 
                        |> Seq.toArray
                    // TODO: Set EventId to something based off the Logary message name or what have you
                    ml.Log (mLogLevel, Microsoft.Extensions.Logging.EventId(0), exn, fmt, fmtParams)
                | _ ->
#if DEBUG
                    raise (NotImplementedException())
#else
                    ml.Log (Microsoft.Extensions.Logging.LogLevel.Warning, EventId(0), exn,
                            "Unimplemented log message type. The following entry may not display correctly. Please report this at: {reportUrl}",
                            [|"https://github.com/jwosty/FSharp.Logf/issues"|])
                    ml.Log (mLogLevel, EventId(0), exn, "{value}", [|msg.value|])
#endif
            with
            | :? NotImplementedException -> reraise ()
            | e ->
                // If we don't do this, this class doesn't show up in the stacktrace at all! Makes it way harder to debug...
                raise (Exception("Error while logging", e))

/// <summary>
///     Extension methods for Suave <see cref="T:Suave.Logging.Logger" /> objects.
/// </summary>
[<AutoOpen>]
module Extensions =
    type Microsoft.Extensions.Logging.ILogger with
        /// <summary>
        ///     Turns a Suave <see cref="Expecto.Logging.Logger" /> into a Microsoft
        ///     <see cref="T:Microsoft.Extensions.Logging.ILogger" />.
        /// </summary>
        member ml.AsSuaveLogger () = MsSuaveLoggerAdapter(ml) :> Suave.Logging.Logger
