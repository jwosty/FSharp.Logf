module FSharp.Logf.Tests
open System
open System.Collections.Generic
open FSharp.Logf
#if FABLE_COMPILER
open Fable.Mocha
open Fable.Mocha.Flip
open Fable.Microsoft.Extensions.Logging
#else
open Expecto
open Expecto.Flip
open Microsoft.Extensions.Logging
#endif

#if FABLE_COMPILER
type TestsAttribute() = inherit Attribute()
#endif

type LogLine = { logLevel: LogLevel; eventId: EventId; message: string; args: seq<KeyValuePair<string,obj>>; error: exn option }

module LogLine =
    let logLevel logLine = logLine.logLevel
    let eventId logLine = logLine.eventId
    let message logLine = logLine.message
    let args logLine = logLine.args
    let error logLine = logLine.error
    
    let empty = { logLevel = LogLevel.Information; eventId = EventId(0); message = ""; args = Seq.empty; error = None }

type AssertableLogger(?level) =
    let level = defaultArg level LogLevel.Debug
    
    member val Lines = List<LogLine>()
    member this.LastLine = this.Lines |> Seq.last
    
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

let mkLogger () = AssertableLogger()

type Point = { x: float; y: float }
type Shape = | Rectangle of w:float * h:float | Circle of r:float | Triangle of b:float * w:float

[<Tests>]
let allTests =
    testList "FSharp_Logf_sln" [
        testList "logf" [
            testCase "Can print hello world" (fun () ->
                let l = mkLogger ()
                logf l LogLevel.Information "Hello, world!"
                l.LastLine.message |> Expect.equal "message" "Hello, world!"
            )
            testCase "Can print other strings" (fun () ->
                let l = mkLogger ()
                logf l LogLevel.Information "FooBar."
                logf l LogLevel.Information "BANANA!123"
                
                l.Lines |> Seq.map LogLine.message
                |> Expect.sequenceEqual "messages" ["FooBar."; "BANANA!123"]
            )
            testCase "Can print with an unnamed string parameter" (fun () ->
                let l = mkLogger ()
                logf l LogLevel.Information "Hello, %s." "Jim"
                l.LastLine.message |> Expect.equal "message" "Hello, Jim."
            )
            testCase "Can print with various unnamed parameters" (fun () ->
                let l = mkLogger ()
                logf l LogLevel.Information "Some params: %s,%d,%.3f,%i" "foo" 42 43.5 -1
                l.LastLine.message |> Expect.equal "message" "Some params: foo,42,43.500,-1"
            )
            testCase "Can print unnamed record parameter" (fun () ->
                let l = mkLogger ()
                logf l LogLevel.Information "Point: %O" { x = 42; y = 43 }
                logf l LogLevel.Information "Point: %A" { x = 42; y = 43 }
                l.Lines.[0].message |> Expect.equal "message 0" "Point: { x = 42\n  y = 43 }"
                l.Lines.[1].message |> Expect.equal "message 1" l.Lines.[0].message
            )
            testCase "Can print unnamed discriminated union parameter" (fun () ->
                let l = mkLogger ()
                logf l LogLevel.Information "Shape 0(O): %O" (Rectangle (1., 2.))
                logf l LogLevel.Information "Shape 0(A): %A" (Rectangle (1., 2.))
                logf l LogLevel.Information "Shape 1(O): %O" (Circle 42.5)
                logf l LogLevel.Information "Shape 1(A): %A" (Circle 42.5)
                
                l.Lines |> Seq.map LogLine.message
                |> Expect.sequenceEqual "messages" [
                    "Shape 0(O): Rectangle (1, 2)"
                    "Shape 0(A): Rectangle (1, 2)"
                    "Shape 1(O): Circle 42.5"
                    "Shape 1(A): Circle 42.5"
                ]
            )
        ]
    ]

module Main =
    [<EntryPoint>]
    let main args =
#if FABLE_COMPILER
        Mocha.runTests allTests
#else
        runTestsWithArgs defaultConfig args allTests
#endif
