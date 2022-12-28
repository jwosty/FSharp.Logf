namespace FSharp.Logf
open System
open System.Collections.Generic
open Microsoft.Extensions.Logging
open Expecto.Logging

type MILogger = Microsoft.Extensions.Logging.ILogger
type MLogLevel = Microsoft.Extensions.Logging.LogLevel
type ELogLevel = Expecto.Logging.LogLevel
type ELogger = Expecto.Logging.Logger

/// Exposes an Expecto Logger as an MS Logger
type ExpectoMsLoggerAdapter(el: ELogger) =
    let theName = el.name
    
    let mToELogLevel = function
        | MLogLevel.None -> ValueNone
        | MLogLevel.Trace -> ValueSome ELogLevel.Verbose
        | MLogLevel.Debug -> ValueSome ELogLevel.Debug
        | MLogLevel.Information -> ValueSome ELogLevel.Info
        | MLogLevel.Warning -> ValueSome ELogLevel.Warn
        | MLogLevel.Error -> ValueSome ELogLevel.Error
        | MLogLevel.Critical | _ -> ValueSome ELogLevel.Fatal
    
    interface MILogger with
        override this.BeginScope<'TState>(state: 'TState) = { new IDisposable with override this.Dispose () = () }
        override this.IsEnabled(logLevel: Microsoft.Extensions.Logging.LogLevel) = true
        override this.Log<'TState>(logLevel: MLogLevel, eventId: EventId, state: 'TState, exn: exn, formatter: Func<'TState, exn, string>) =
            let messageFactory eLogLevel : Message =
                let msg, props =
                    match state :> obj with
                    // sources to help see what's going on here:
                    //      https://github.com/dotnet/runtime/blob/8e8a62156ea459945b1d953c4fbe950d276bbc9b/src/libraries/Microsoft.Extensions.Logging.Abstractions/src/LoggerExtensions.cs#L410
                    //      https://github.com/dotnet/runtime/blob/8e8a62156ea459945b1d953c4fbe950d276bbc9b/src/libraries/Microsoft.Extensions.Logging.Abstractions/src/FormattedLogValues.cs#L16
                    //      https://github.com/serilog/serilog-extensions-logging/blob/39307172d5116e430e974aed03dcb002571b3276/src/Serilog.Extensions.Logging/Extensions/Logging/SerilogLogger.cs#L80
                    // Basically, when you write `msLogger.Log("Some {foo} message with more stuff: {bar}", arg1, arg2)`,
                    // under the hood it calls .Log (the one we're implementing right here) with an instance of
                    // FormattedLogValues as the state. Right here we're picking up any type with that same shape.
                    // That's how the structured parameters are passed into the log provider (hence why that Serilog
                    // adapter is a good reference)
                    | :? IEnumerable<KeyValuePair<string, obj>> as structure ->
                        // The example a few lines ago gives this structure:
                        // 0. foo => arg1
                        // 1. bar => arg2
                        // 2. {OriginalFormat} => "Some {foo} message with more stuff: {bar}"
                        
                        // Convert to a property Map, because that's what Expecto takes
                        let props = structure |> Seq.map (fun kv -> kv.Key, kv.Value) |> Map.ofSeq
                        let props' =
                            props
                            |> Map.remove "{OriginalFormat}"
                            // |> (fun p ->
                            //     if isNull exn then p
                            //     else Map.add Expecto.Logging.Literals.FieldExnKey exn p)
                        // The message format is exactly compatible with Expecto's message format, so just pass it
                        // right through. Also, it should end up as a string, so we could probably just cast it and be
                        // fine, but might as well engage in a little defensive programming here
                        let format =
                            props |> Map.tryFind "{OriginalFormat}"
                            |> Option.bind Option.ofObj |> Option.map (fun x -> x.ToString ()) |> Option.defaultValue ""
                        { Message.event eLogLevel format with fields = props' }, props'
                    | _ ->
                        Message.event eLogLevel (formatter.Invoke(state, exn)), Map.empty
                let name = theName
                let m =
                    { msg with
                        fields =
                            if isNull exn then props
                            else Map.add Expecto.Logging.Literals.FieldExnKey (upcast exn) props }
                    |> Message.setName name
                m
            match mToELogLevel logLevel with
            | ValueSome eLogLevel -> el.log eLogLevel messageFactory |> Async.Start
            | ValueNone -> ()

[<AutoOpen>]
module Extensions =
    type Expecto.Logging.Logger with
        member el.AsMsLogger () = ExpectoMsLoggerAdapter(el) :> MILogger
