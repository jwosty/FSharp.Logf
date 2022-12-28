module FSharp.Logf
open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Logging

type MILogger = Microsoft.Extensions.Logging.ILogger
type MLogLevel = Microsoft.Extensions.Logging.LogLevel
type SLogLevel = Suave.Logging.LogLevel
type SLogger = Suave.Logging.Logger
type SMessage = Suave.Logging.Message
module SMessage = Suave.Logging.Message

/// Exposes an MS ILogger as a Suave Logger
type MsSuaveLoggerAdapter(ml: MILogger) =
    let sToMLogLevel = function
        | SLogLevel.Verbose -> MLogLevel.Trace
        | SLogLevel.Debug -> MLogLevel.Debug
        | SLogLevel.Info -> MLogLevel.Information
        | SLogLevel.Warn -> MLogLevel.Warning
        | SLogLevel.Error -> MLogLevel.Error
        | SLogLevel.Fatal | _ -> MLogLevel.Critical

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
                    ml.Log (mLogLevel, EventId(0), exn, fmt, fmtParams)
                | _ ->
#if DEBUG
                    raise (NotImplementedException())
#else
                    ml.Log (mLogLevel, EventId(0), exn, "Unknown log message value: {value}", [|msg.value|])
#endif
            with
            | :? NotImplementedException -> reraise ()
            | e ->
                // If we don't do this, this class doesn't show up in the stacktrace at all! Makes it way harder to debug...
                raise (Exception("Error while logging", e))

[<AutoOpen>]
module Extensions =
    type Microsoft.Extensions.Logging.ILogger with
        member ml.AsSuaveLogger () = MsSuaveLoggerAdapter(ml) :> SLogger
