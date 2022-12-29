﻿module FSharp.Logf.Tests
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

type LogLine = { logLevel: LogLevel; eventId: EventId; message: string; args: list<string*obj>; error: exn option }

module LogLine =
    let logLevel logLine = logLine.logLevel
    let eventId logLine = logLine.eventId
    let message logLine = logLine.message
    let args logLine = logLine.args
    let error logLine = logLine.error
    
    let empty = { logLevel = LogLevel.Information; eventId = EventId(0); message = ""; args = List.empty; error = None }

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
#if !FABLE_COMPILER
                // See FSharp.Logf.ExpectoMsLoggerAdapter
                | :? IEnumerable<KeyValuePair<string, obj>> as structure ->
                    let msgKv = structure |> Seq.find (fun x -> x.Key = "{OriginalFormat}")
                    let msg = msgKv.Value :?> string
                    let args = structure |> Seq.filter (fun x -> x.Key <> "{OriginalFormat}") |> Seq.map (fun x -> x.Key, x.Value) |> Seq.toList
                    msg, args
#endif
                | _ ->
                    formatter.Invoke(state, error), []
            this.Lines.Add { logLevel = logLevel; eventId = eventId; message = msg; args = args; error = Option.ofObj error }

let mkLogger () = AssertableLogger()

type Point = { x: float; y: float }
type Shape = | Rectangle of w:float * h:float | Circle of r:float | Triangle of b:float * w:float

type DummyException() =
    inherit Exception("This is a fake exception for testing that did not come from real code.")

let makeDummyException () =
    try raise (DummyException())
    with e -> e

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
            testCase "Can print an unnamed string parameter" (fun () ->
                let l = mkLogger ()
                logf l LogLevel.Information "Hello, %s." "Jim"
                l.LastLine.message |> Expect.equal "message" "Hello, Jim."
            )
            testCase "Can print various unnamed parameters" (fun () ->
                let l = mkLogger ()
                logf l LogLevel.Information "Some params: %s,%d,%.3f,%i" "foo" 42 43.5 -1
                l.LastLine.message |> Expect.equal "message" "Some params: foo,42,43.500,-1"
            )
            testCase "Can print unnamed record parameter" (fun () ->
                let l = mkLogger ()
                logf l LogLevel.Information "Point: %O" { x = 42; y = 43 }
                logf l LogLevel.Information "Point: %A" { x = 42; y = 43 }
                l.Lines.[0].message |> Expect.equal "message 0"
#if !FABLE_COMPILER
                    "Point: { x = 42.0\n  y = 43.0 }"
#else
                    "Point: { x = 42\n  y = 43 }"
#endif
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
#if !FABLE_COMPILER
                    "Shape 0(O): Rectangle (1.0, 2.0)"
                    "Shape 0(A): Rectangle (1.0, 2.0)"
                    "Shape 1(O): Circle 42.5"
                    "Shape 1(A): Circle 42.5"
#else
                    "Shape 0(O): Rectangle (1, 2)"
                    "Shape 0(A): Rectangle (1, 2)"
                    "Shape 1(O): Circle 42.5"
                    "Shape 1(A): Circle 42.5"
#endif
                ]
            )
            testCase "Can print a mix of named and unnamed parameters" (fun () ->
                let l = mkLogger ()
                logf l LogLevel.Information "%s%s{namedParam}%s" "a" "b" "c"
                logf l LogLevel.Information "%s{namedParam}%c" "a" 'z'
                
                l.Lines |> Expect.sequenceEqual "Log lines" [
#if !FABLE_COMPILER
                    { LogLine.empty with message = "a{namedParam}c"; args = ["namedParam", "b"] }
                    { LogLine.empty with message = "{namedParam}z"; args = ["namedParam", "a"] }
#else
                    { LogLine.empty with message = "abc" }
                    { LogLine.empty with message = "az" }
#endif
                ]
            )
        ]
        testList "SharedTests" [
            let inline sharedTestCase name code =
                let err = makeDummyException ()
                testList name [
                    for (funcName, logfOrElogf, emptyLogLine) in [nameof(logf), logf, LogLine.empty; nameof(elogf), (fun logger logLevel args -> elogf logger logLevel err args), { LogLine.empty with error = Some err }] ->
                        testCase funcName (fun () -> code logfOrElogf emptyLogLine)
                ]
                
            testList "Escapes curly braces not part of a named parameter" [
                // .NET impl should escape the curly braces lest they be interpreted as a message template by the ILogger object.
                // Fable impl doesn't need to do this since the thing printing won't actually output named parameters anyway.
                sharedTestCase "case 1" (fun logfOrElogf emptyLogLine ->
                    let l = mkLogger ()
                    logfOrElogf l LogLevel.Information "%s{" "Yo"
                    
                    l.LastLine |> Expect.equal "Log lines"
#if !FABLE_COMPILER
                        { emptyLogLine with message = "Yo{{" }
#else
                        { emptyLogLine with message = "Yo{" }
#endif
                )
                sharedTestCase "case 2" (fun logfOrElogf emptyLogLine ->
                    let l = mkLogger ()
                    logfOrElogf l LogLevel.Information "%s{%d}" "SomeString" 42
                    
                    l.LastLine |> Expect.equal "Log lines"
#if !FABLE_COMPILER
                        { emptyLogLine with message = "SomeString{{42}}" }
#else
                        { emptyLogLine with message = "SomeString{42}" }
#endif
                )
                sharedTestCase "case 3" (fun logfOrElogf emptyLogLine ->
                    let l = mkLogger ()
                    logfOrElogf l LogLevel.Information "%s{}%d {iaminvalid}" "XYZ" -797
                    
                    l.LastLine |> Expect.equal "Log lines"
#if !FABLE_COMPILER
                        { emptyLogLine with message = "XYZ{{}}-797 {{iaminvalid}}" }
#else
                        { emptyLogLine with message = "XYZ{}-797 {iaminvalid}" }
#endif
                )
                sharedTestCase "case 4" (fun logfOrElogf emptyLogLine ->
                    let l = mkLogger ()
                    logfOrElogf l LogLevel.Information "{%s{}%d}" "XYZ" -797
                    
                    l.LastLine |> Expect.equal "Log lines"
#if !FABLE_COMPILER
                        { emptyLogLine with message = "{{XYZ{{}}-797}}" }
#else
                        { emptyLogLine with message = "{XYZ{}-797}" }
#endif
                )
            ]
            
            testList "Can print various named parameters" [
                sharedTestCase "basic case" (fun logfOrElogf emptyLogLine ->
                    let l = mkLogger ()
                    
                    logfOrElogf l LogLevel.Information "Drawing rectangle with dimensions: %f{width},%f{height}" 100. 234.
                    l.LastLine |> Expect.equal "Log lines"
#if !FABLE_COMPILER
                        { emptyLogLine with message = "Drawing rectangle with dimensions: {width},{height}"; args = ["width", 100.0; "height", 234.0] }
#else
                        { emptyLogLine with message = "Drawing rectangle with dimensions: 100.000000,234.000000" }
#endif
                )
                sharedTestCase "params reversed" (fun logfOrElogf emptyLogLine ->
                    let l = mkLogger ()
                    
                    logfOrElogf l LogLevel.Information "Drawing rectangle with dimensions (reversed): %f{height},%f{width}" 234. 100.
                    l.LastLine |> Expect.equal "Log lines" 
#if !FABLE_COMPILER
                        { emptyLogLine with message = "Drawing rectangle with dimensions (reversed): {height},{width}"; args = ["height", 234.0; "width", 100.0] }
#else
                        { emptyLogLine with message = "Drawing rectangle with dimensions (reversed): 234.000000,100.000000" }
#endif
                )
                sharedTestCase "advanced fmt specifiers" (fun logfOrElogf emptyLogLine ->
                    let l = mkLogger ()
                    
                    logfOrElogf l LogLevel.Information "Params: %.2f{a},%.3f{b},%.10f{c}" 234.2 100.3 544.5
                    l.LastLine |> Expect.equal "Log lines" 
#if !FABLE_COMPILER
                        { emptyLogLine with message = "Params: {a},{b},{c}"; args = ["a", 234.2; "b", 100.3; "c", 544.5] }
#else
                        { emptyLogLine with message = "Params: 234.20,100.300,544.5000000000" }
#endif
                )
            ]
        ]
        testList "Functions log at the correct level" [
            for (logLevel, logfVariant, elogfVariantOpt) in [
                LogLevel.Trace, logft, None
                LogLevel.Debug, logfd, None
                LogLevel.Information, logfi, None
                LogLevel.Warning, logfw, Some elogfw
                LogLevel.Error, logfe, Some elogfe
                LogLevel.Critical, logfc, Some elogfc
            ] do
                // see https://github.com/fable-compiler/Fable/issues/3315
                yield testCase (Enum.GetName(typeof<LogLevel>, logLevel)) (fun () ->
                    let l = mkLogger ()
                    logf l logLevel "Hello, %s!" "world"
                    logfVariant l "Hello, %s!" "world"
                    l.Lines[0].logLevel |> Expect.equal "logLevel" logLevel
                    l.Lines[0].message |> Expect.equal "message" "Hello, world!"
                    l.Lines[1] |> Expect.equal "logf variant should be the same as calling logf with the corresponding level" l.Lines[0]
                    
                    let err = makeDummyException ()
                    
                    match elogfVariantOpt with
                    | Some elogfVariant ->
                        elogfVariant l err "Hello, %s!" "world"
                        l.Lines[2] |> Expect.equal "elogf variant should be the same as calling logf with the corresponding level and exception" { l.Lines[0] with error = Some err }
                    | None -> ()
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
