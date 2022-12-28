module FSharp.Logf.Tests
open System
open System.Collections.Generic
open FSharp.Logf
open Fable.Mocha
#if FABLE_COMPILER
open Fable.Microsoft.Extensions.Logging
#else
open Expecto
open Expecto.Flip
open Microsoft.Extensions.Logging
#endif

type LogLine = { logLevel: LogLevel; eventId: EventId; message: string; args: seq<KeyValuePair<string,obj>>; error: exn option }

type AssertableLogger(level) =
    member val Lines = List<LogLine>()
    
    interface ILogger with
        override this.BeginScope<'TState> (state: 'TState) = raise (NotImplementedException())
        override this.IsEnabled (level': LogLevel) = level' >= level
        override this.Log<'TState> (logLevel, eventId, state: 'TState, error, formatter) =
            let msg, args =
                match state :> obj with
                // See FSharp.Logf.ExpectoMsLoggerAdapter
                | :? IEnumerable<KeyValuePair<string, obj>> as structure ->
                    let msgKv = structure |> Seq.find (fun x -> x.Key = "{OriginalFormat}")
                    let msg = msgKv.Value :?> string
                    let args = structure |> Seq.filter (fun x -> x.Key <> "{OriginalFormat}")
                    msg, args
                | _ ->
                    formatter.Invoke(state, error), Seq.empty
            this.Lines.Add { logLevel = logLevel; eventId = eventId; message = msg; args = args; error = Option.ofObj error }

[<Tests>]
let allTests =
    testList "FSharp_Logf_sln" [
        
    ]

module Main =
    [<EntryPoint>]
    let main args =
        runTestsWithArgs defaultConfig args allTests
