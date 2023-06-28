module FSharp.Logf.Tests
open System
open System.Collections.Generic


#if !FABLE_COMPILER
open Expecto
open Expecto.Flip
open Microsoft.Extensions.Logging
open FSharp.Logf
#else
open Fable.Mocha
open Fable.Mocha.Flip
open Fable.Microsoft.Extensions.Logging
open Fable.FSharp.Logf
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

type AssertableLogger<'a>(?level) =
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

    interface ILogger<'a>

let mkLogger () = AssertableLogger()

type Point = { x: float; y: float }
type Shape = | Rectangle of w:float * h:float | Circle of r:float | Triangle of b:float * w:float

type DummyException() =
    inherit Exception("This is a fake exception for testing that did not come from real code.")

let makeDummyException () =
    try raise (DummyException())
    with e -> e

[<AutoOpen>]
module Helpers =
#if !FABLE_COMPILER
    open Serilog
    open Serilog.Sinks
    open Serilog.Extensions.Logging
    open System.IO
    
    let serilog2Mel (sl: Serilog.ILogger) : Microsoft.Extensions.Logging.ILogger<'a> =
        use lf = (new SerilogLoggerFactory(sl))
        lf.CreateLogger<'a>()
#endif
    
    /// Fully renders a logf call, and asserts that the resulting messages are the same as the given output message
    /// (using a Serilog TextWriter sink to compare)
    let assertEquivalentOutputM msg expectedRenderedMessage logfCall =
        let pt2 = if String.IsNullOrEmpty msg then "" else $" (%s{msg})"
        
        use logfTw = new StringWriter()
        let outputTemplate = "{Message:lj}"
        let logfLogger = LoggerConfiguration().WriteTo.TextWriter(textWriter = logfTw, outputTemplate = outputTemplate).CreateLogger() |> serilog2Mel
        logfCall logfLogger
        let logfRender = logfTw.ToString()
        
        logfRender |> Expect.equal ("Rendered logf call should match expected value" + pt2) expectedRenderedMessage
        
    let assertEquivalentOutput expectedRenderedMessage logfCall = assertEquivalentOutputM "" expectedRenderedMessage logfCall
    
    /// Checks that the logf calls .Log() with a particular set of parameters, and also check that the rendered message
    /// matches a given expected output
    let assertEquivalentM msg (logMethodCall: ILogger<'a> -> unit) expectedRenderedMsg (logfCall: ILogger<'a> -> unit) =
        let pt2 = if String.IsNullOrEmpty msg then "" else $" (%s{msg})"
        
        let mutable exns = []
        let collectExn (f: unit -> unit) =
            try f ()
            with e -> exns <- e :: exns
        
    #if !FABLE_COMPILER
        do
            use logMethodTw = new StringWriter()
            use logfTw = new StringWriter()
            let outputTemplate = "{Message:lj}"
            let logMethodLogger = LoggerConfiguration().WriteTo.TextWriter(textWriter = logMethodTw, outputTemplate = outputTemplate).CreateLogger() |> serilog2Mel
            let logfLogger = LoggerConfiguration().WriteTo.TextWriter(textWriter = logfTw, outputTemplate = outputTemplate).CreateLogger() |> serilog2Mel
            logMethodCall logMethodLogger
            logfCall logfLogger
            let logfRender = logfTw.ToString()
            let logMethodRender = logMethodTw.ToString()
            
            collectExn (fun () -> logfRender |> Expect.equal ("Rendered logf call should match expected value" + pt2) expectedRenderedMsg)
            collectExn (fun () -> logMethodRender |> Expect.equal ("Rendered log method call should match expected value" + pt2) expectedRenderedMsg)
    #endif
        
        do
            let logMethodLogger = AssertableLogger<'a>()
            let logfLogger = AssertableLogger<'a>()
            logMethodCall logMethodLogger
            logfCall logfLogger
            collectExn (fun () -> logfLogger.Lines |> Expect.sequenceEqual ("logf call should be equivalent to Log call" + pt2) logMethodLogger.Lines)
        
        if not (List.isEmpty exns) then
            raise (AggregateException(exns))
        
    let assertEquivalent (logMethodCall: ILogger<'a> -> unit) expectedRenderedMsg (logfCall: ILogger<'a> -> unit) =
        assertEquivalentM "" logMethodCall expectedRenderedMsg logfCall

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
// Didn't feel like making direct .Log calls work under Fable because they require dealing with MessageFormatter func
#if !FABLE_COMPILER
        testList "Oracle tests" [
            testCase "No parameters" (fun () ->
                (fun l -> logfi l "Hello, world!")
                |> assertEquivalent
                    (fun l -> l.LogInformation "Hello, world!")
                    "Hello, world!"
            )
            testCase "One named parameter" (fun () ->
                (fun l -> logfi l "Hello, %s{Person}" "Sam")
                |> assertEquivalent
                    (fun l -> l.LogInformation ("Hello, {Person}", "Sam"))
                    "Hello, Sam"
            )
            testCase "Many named parameters" (fun () ->
                (fun l -> logfi l "A is %s{A}, B is %d{B}, C is %b{C}" "foo" 42 false)
                |> assertEquivalent
                    (fun l -> l.LogInformation ("A is {A}, B is {B}, C is {C}", "foo", 42, false))
                    "A is foo, B is 42, C is false"
            )
            testCase "One unnamed parameter" (fun () ->
                (fun l -> logfi l "Hello, %s" "Sam")
                |> assertEquivalent
                    (fun l -> l.LogInformation "Hello, Sam")
                    "Hello, Sam"
            )
            testCase "Many unnamed parameters" (fun () ->
                (fun l -> logfi l "A is %s, B is %d, C is %b" "foo" 42 false)
                |> assertEquivalent
                    (fun l -> l.LogInformation ("A is foo, B is 42, C is false", "foo", 42, false))
                    "A is foo, B is 42, C is false"
            )
            testCase "Many named and unnamed parameters" (fun () ->
                (fun l -> logfi l "A is %s{A}, B is %d, C is %b{C}, D is %s" "foo" 42 false "bar")
                |> assertEquivalent
                    (fun l -> l.LogInformation ("A is {A}, B is 42, C is {C}, D is bar", "foo", false))
                    "A is foo, B is 42, C is false, D is bar"
            )
            testCase "Named parameter with destructure operator" (fun () ->
                let x = {| Latitude = 25; Longitude = 134 |}
                (fun l -> logfi l "Processing %A{@sensorInput}" x)
                |> assertEquivalent
                    (fun l -> l.LogInformation ("Processing {@sensorInput}", x))
                    """Processing {"Latitude":25,"Longitude":134}"""
            )
            testList ".NET-style format specifiers" [
                testCase "Float case 1" (fun () ->
                    (fun l -> logfi l "Duration: %f{durationMs:0.#}" (5. / 3.))
                    |> assertEquivalent
                        (fun l -> l.LogInformation ("Duration: {durationMs:0.#}", (5. / 3.)))
                        "Duration: 1.7"
                )
                testCase "Float case 2" (fun () ->
                    (fun l -> logfi l "Duration: %f{durationMs:0.##}" (5. / 3.))
                    |> assertEquivalent
                        (fun l -> l.LogInformation ("Duration: {durationMs:0.##}", (5. / 3.)))
                        "Duration: 1.67"
                )
                testCase "Alignment" (fun () ->
                    (fun l -> logfi l "%f{balance,-10}" 12345.98m)
                    |> assertEquivalent
                        (fun l -> l.LogInformation ("{balance,-10}", 12345.98m))
                        "12345.98  "
                )
                testCase "Alignment with currency format" (fun () ->
                    (fun l -> logfi l "%f{balance,-10:C}" 12345.98m)
                    |> assertEquivalent
                        (fun l -> l.LogInformation ("{balance,-10:C}", 12345.98m))
                        "¤12,345.98"
                )
                testCase "Hex format" (fun () ->
                    (fun l -> logfi l "%i{value:X}" 0xdeadbeef)
                    |> assertEquivalent
                        (fun l -> l.LogInformation ("{value:X}", 0xdeadbeef))
                        "DEADBEEF"
                )
                testCase "Corner case 1" (fun () ->
                    (fun l -> logfi l "%i{value}:X}" 0xdeadbeef)
                    |> assertEquivalent
                        (fun l -> l.LogInformation ("{value}:X}}", 0xdeadbeef))
                        (string 0xdeadbeef + ":X}")
                )
                testCase "Corner case 2" (fun () ->
                    (fun l -> logfi l "%i{value},3}" 0xdeadbeef)
                    |> assertEquivalent
                        (fun l -> l.LogInformation ("{value},3}}", 0xdeadbeef))
                        (string 0xdeadbeef + ",3}")
                )
            ]
            testList "printf format specifiers" [
                testCase "Hex format" (fun () ->
                    (fun l -> logfi l "%x{value}" 0xdeadbeef)
                    |> assertEquivalentM "little x"
                        (fun (l: ILogger<_>) -> l.LogInformation ("{value:x}", 0xdeadbeef))
                        "deadbeef"
                    (fun l -> logfi l "%X{value}" 0xdeadbeef)
                    |> assertEquivalentM "big X"
                        (fun (l: ILogger<_>) -> l.LogInformation ("{value:X}", 0xdeadbeef))
                        "DEADBEEF"
                )
                testCase "Float with zero left and zero right decimal places" (fun () ->
                    (fun l -> logfi l "%0.0f{value}" (5. / 3.))
                    |> assertEquivalent
                        (fun (l: ILogger<_>) -> l.LogInformation ("{value:0.}", 5. / 3.))
                        "2"
                )
                testCase "Float with one right decimal place" (fun () ->
                    (fun l -> logfi l "%.1f{value}" (5. / 3.))
                    |> assertEquivalent
                        (fun (l: ILogger<_>) -> l.LogInformation ("{value:.0}", 5. / 3.))
                        "1.7"
                )
                testCase "Float with two right decimal places" (fun () ->
                    (fun l -> logfi l "%.2f{value}" (5. / 3.))
                    |> assertEquivalent
                        (fun (l: ILogger<_>) -> l.LogInformation ("{value:.00}", 5. / 3.))
                        "1.67"
                )
                testCase "Float with ten right decimal places" (fun () ->
                    (fun l -> logfi l "%.10f{value}" (5. / 3.))
                    |> assertEquivalent
                        (fun (l: ILogger<_>) -> l.LogInformation ("{value:.0000000000}", 5. / 3.))
                        "1.6666666667"
                )
                testCase "Float with one left decimal place" (fun () ->
                    (fun l -> logfi l "%1f{value}" (5. / 3.))
                    |> assertEquivalent
                        (fun (l: ILogger<_>) -> l.LogInformation ("{value:0.}", 5. / 3.))
                        "2"
                )
                testCase "Float with two left decimal places" (fun () ->
                    (fun l -> logfi l "%2f{value}" (5. / 3.))
                    |> assertEquivalent
                        (fun (l: ILogger<_>) -> l.LogInformation ("{value:00.}", 5. / 3.))
                        "02"
                )
                testCase "Float with ten left decimal places" (fun () ->
                    (fun l -> logfi l "%10f{value}" (5. / 3.))
                    |> assertEquivalent
                        (fun (l: ILogger<_>) -> l.LogInformation ("{value:0000000000.}", 5. / 3.))
                        "0000000002"
                )
                testCase "Float with two left and three right decimal places" (fun () ->
                    (fun l -> logfi l "%2.3f{value}" (5. / 3.))
                    |> assertEquivalent
                        (fun (l: ILogger<_>) -> l.LogInformation ("{value:00.000}", 5. / 3.))
                        "01.667"
                )
                testList "Float with + flag" [
                    for value, expected in [ -42., "-42.000"; 0., "+00.000"; 42., "+42.000" ] ->
                        testCase (string value) (fun () ->
                            // sprintf "%+2.3f" 0.
                            (fun l -> logfi l "%+2.3f{value}" value)
                            |> assertEquivalentOutput expected
                        )
                ]
                testCase "Several interspersed format specifiers" (fun () ->
                    (fun l -> logfi l "%4.1f{float}, %b{boolean}, %x{hex}" 42.59 false 0xcafebabe)
                    |> assertEquivalent
                        (fun l -> l.LogInformation ("{float:0000.0}, {boolean}, {hex:x}", 42.59, false, 0xcafebabe))
                        "0042.6, false, cafebabe"
                ) 
            ]
        ]
#endif
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
                sharedTestCase "case 5" (fun logfOrElogf emptyLogLine ->
                    let l = mkLogger ()
                    // Note: %f.5 isn't a valid format specifier, which is why the {x} should get escaped
                    logfOrElogf l LogLevel.Information "{%.4f{}%d}%f.10{x}" 100.2 -797 1.
                    
                    l.LastLine |> Expect.equal "Log lines"
#if !FABLE_COMPILER
                        { emptyLogLine with message = "{{100.2000{{}}-797}}1.000000.10{{x}}" }
#else
                        { emptyLogLine with message = "{100.2000{}-797}1.000000.10{x}" }
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
                sharedTestCase "param names may contain alphanumerics or underscores" (fun logfOrElogf emptyLogLine ->
                    let l = mkLogger ()
                    
                    logfOrElogf l LogLevel.Information "%f{param1},%f{2param},%f{foo_bar}" 234. 100. 3.
                    l.LastLine |> Expect.equal "Log lines" 
#if !FABLE_COMPILER
                        { emptyLogLine with message = "{param1},{2param},{foo_bar}"; args = ["param1", 234.0; "2param", 100.0; "foo_bar", 3.] }
#else
                        { emptyLogLine with message = "234.000000,100.000000,3.000000" }
#endif
                )
                sharedTestCase "fmt specs with precision" (fun logfOrElogf emptyLogLine ->
                    let l = mkLogger ()
                    
                    logfOrElogf l LogLevel.Information "Params: %.2f{a},%.3f{b},%.10f{c}" 234.2 100.3 544.5
                    l.LastLine |> Expect.equal "Log lines"
#if !FABLE_COMPILER
                        { emptyLogLine with message = "Params: {a:.00},{b:.000},{c:.0000000000}"; args = ["a", 234.2; "b", 100.3; "c", 544.5] }
#else
                        { emptyLogLine with message = "Params: 234.20,100.300,544.5000000000" }
#endif
                )
                sharedTestCase "fmt specs with flags, width, and precision" (fun logfOrElogf emptyLogLine ->
                    let l = mkLogger ()
                    
                    logfOrElogf l LogLevel.Information "%0-2.3f{xyz} %0+-10f{abc} %+.5f{d} %5.5f{w}" 1. 2. 3. 4.
#if !FABLE_COMPILER
                    l.LastLine |> Expect.equal "Log lines"
                        { emptyLogLine with message = "{xyz:00.000} {abc:0000000000.} {d:+.00000;-.00000} {w:00000.00000}"; args = ["xyz", 1.; "abc", 2.; "d", 3.; "w", 4.] }
#else
                    l.LastLine |> Expect.equal "Log lines"
                        { emptyLogLine with message = "1.000 +2.000000  +3.00000 4.00000" }
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
        // Uncomment these for a sort of manual "integration" test
        // let ml = ConsoleLogger()
        // let x = "world"
        // logft ml "Trace %s{arg}." x
        // logfd ml "Debug %s{arg}." x
        // logfi ml "Info %s{arg}." x
        // logfw ml "Warning %s{arg}." x
        // logfe ml "Error %s{arg}." x
        // logfc ml "Critical %s{arg}." x
        // let err = try raise (DummyException()) with e -> e
        // elogfw ml err "(exn) Warning %s{arg}." x
        // elogfe ml err "(exn) Error %s{arg}." x
        // elogfc ml err "(exn) Critical %s{arg}." x
        // 0
#else
        runTestsWithArgs defaultConfig args allTests
#endif
