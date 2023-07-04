module FSharp.Logf.Tests
open System
open System.Collections.Generic
open System.Globalization


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
                let f (structure: IEnumerable<KeyValuePair<string, obj>>) =
                    let msgKv = structure |> Seq.find (fun x -> x.Key = "{OriginalFormat}")
                    let msg = msgKv.Value :?> string
                    let args = structure |> Seq.filter (fun x -> x.Key <> "{OriginalFormat}") |> Seq.map (fun x -> x.Key, x.Value) |> Seq.toList
                    msg, args
                
#if FABLE_COMPILER
                try
                    f ((state :> obj) :?> IEnumerable<KeyValuePair<string, obj>>)
                with _ ->
                    formatter.Invoke(state, error), []
#else
                match state :> obj with
                // See FSharp.Logf.ExpectoMsLoggerAdapter
                | :? IEnumerable<KeyValuePair<string, obj>> as structure ->
                    f structure
                | _ ->
                    formatter.Invoke(state, error), []
#endif
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

type Helpers =
    static member StringFormat (format: string, [<ParamArray>] args: obj[]) = String.Format (CultureInfo.InvariantCulture, format, args)

[<AutoOpen>]
module Helpers =
    open System.IO
    
#if !FABLE_COMPILER
    open Serilog
    open Serilog.Sinks
    open Serilog.Extensions.Logging
    
    let serilog2Mel (sl: Serilog.ILogger) : Microsoft.Extensions.Logging.ILogger<'a> =
        use lf = (new SerilogLoggerFactory(sl))
        lf.CreateLogger<'a>()
#endif
    
#if FABLE_COMPILER
    let assertEquivalentM msg expectedRenderedMsg (expectedLogArgs: (string * obj) list) (logfCall: ILogger<'a> -> unit) =
        let pt2 = if String.IsNullOrEmpty msg then "" else $" (%s{msg})"
        
        let logger = AssertableLogger<_>()
        logfCall logger
        Expect.hasLength logger.Lines 1 "should log exactly one line"
        let line = logger.Lines[0]
        line.message |> Expect.equal ("Rendered (fable) logf call should match expected value" + pt2) expectedRenderedMsg
#else
    
    /// check that the rendered message matches a given expected output, and also checks that the logf outputs an
    /// expected set of parameters
    let assertEquivalentM msg expectedRenderedMsg (expectedLogArgs: (string * obj) list) (logfCall: ILogger<'a> -> unit) =
        let pt2 = if String.IsNullOrEmpty msg then "" else $" (%s{msg})"
        
        let mutable exns = []
        let collectExn (f: unit -> unit) =
            try f ()
            with e -> exns <- e :: exns
        
        do
            let logger = AssertableLogger<_>()
            logfCall logger
            collectExn (fun () -> Expect.hasLength logger.Lines 1 "should log exactly one line")
            let line = logger.Lines[0]
            collectExn (fun () -> line.args |> Expect.sequenceEqual "should contain expected log args" expectedLogArgs)
        
        do
            use logfTw = new StringWriter()
            let outputTemplate = "{Message:lj}"
            let logfLogger = LoggerConfiguration().WriteTo.TextWriter(textWriter = logfTw, outputTemplate = outputTemplate).CreateLogger() |> serilog2Mel
            logfCall logfLogger
            let logfRender = logfTw.ToString()
            
            collectExn (fun () -> logfRender |> Expect.equal ("Rendered logf call should match expected value" + pt2) expectedRenderedMsg)
        
        if not (List.isEmpty exns) then
            raise (AggregateException(exns))
    #endif
        
    let assertEquivalent expectedRenderedMsg expectedLogArgs (logfCall: ILogger<'a> -> unit) =
        assertEquivalentM "" expectedRenderedMsg expectedLogArgs logfCall

let theoryAssertEquivalent name values logfCall expectedRenderedMsg expectedLogArgs =
    testList name [
        for value in values ->
            testCase (sprintf "(%A)" value) (fun () ->
                logfCall
                |> assertEquivalent
                    expectedRenderedMsg
                    expectedLogArgs
            )
    ]

let theory name (cases: (string * 'a) list) testCode =
    testList name [
        for name, value in cases ->
            testCase (sprintf "(%s)" name) (fun () -> testCode value)
    ]

let caseData (values: 'a list) = values |> List.map (fun x -> string x, x)

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
        testList "Oracle tests" [
            testCase "No parameters" (fun () ->
                (fun l -> logfi l "Hello, world!")
                |> assertEquivalent
                    "Hello, world!"
                    []
            )
            testCase "One named parameter" (fun () ->
                (fun l -> logfi l "Hello, %s{Person}" "Sam")
                |> assertEquivalent
                    (sprintf "Hello, %s" "Sam")
                    ["Person", "Sam"]
            )
            testCase "Many named parameters" (fun () ->
                (fun l -> logfi l "A is %s{A}, B is %d{B}, C is %b{C}" "foo" 42 false)
                |> assertEquivalent
                    (sprintf "A is %s, B is %d, C is %b" "foo" 42 false)
                    ["A", "foo"; "B", 42; "C", false]
            )
            testCase "One unnamed parameter" (fun () ->
                (fun l -> logfi l "Hello, %s" "Sam")
                |> assertEquivalent
                    (sprintf "Hello, %s" "Sam")
                    []
            )
            testCase "Many unnamed parameters" (fun () ->
                (fun l -> logfi l "A is %s, B is %d, C is %b" "foo" 42 false)
                |> assertEquivalent
                    (sprintf "A is %s, B is %d, C is %b" "foo" 42 false)
                    []
            )
            testCase "Many named and unnamed parameters" (fun () ->
                (fun l -> logfi l "A is %s{A}, B is %d, C is %b{C}, D is %s" "foo" 42 false "bar")
                |> assertEquivalent
                    (sprintf "A is %s, B is %d, C is %b, D is %s" "foo" 42 false "bar")
                    ["A", "foo"; "C", false]
            )
            testCase "Named parameter with destructure operator" (fun () ->
                let x = {| Latitude = 25; Longitude = 134 |}
                (fun l -> logfi l "Processing %A{@sensorInput}" x)
                |> assertEquivalent
#if !FABLE_COMPILER
                    """Processing {"Latitude":25,"Longitude":134}"""
#else
                    // This is just how Fable formats %A objects... Not going to override its behavior.
                    """Processing [object Object]"""
#endif
                    ["@sensorInput", x]
            )
            let valuesF = caseData [ 5. / 3.; 50. / 3.; 500. / 3.; -(5. / 3.); -42.; 0.; 42. ]
            let valuesD = caseData [ 0m; 12345.98m; -10m; 0.012m ]
            let valuesI = caseData [ 0xdeadbeef; 42 ]
            testList ".NET-style format specifiers" [
                theory "Float case 1" valuesF (fun x ->
                    (fun l -> logfi l "Duration: %f{durationMs:0.#}" x)
                    |> assertEquivalent
                        (sprintf "Duration: %s" (Helpers.StringFormat("{0:0.#}", x)))
                        ["durationMs", x]
                )
                theory "Float case 2" valuesF (fun x ->
                    (fun l -> logfi l "Duration: %f{durationMs:0.##}" x)
                    |> assertEquivalent
                        (sprintf "Duration: %s" (Helpers.StringFormat ("{0:0.##}", x)))
                        ["durationMs", x]
                )
                theory "Alignment" valuesD (fun x ->
                    (fun l -> logfi l "%f{balance,-10}" x)
                    |> assertEquivalent
                        (Helpers.StringFormat("{0,-10}", x))
                        ["balance", x]
                )
                theory "Alignment with currency format" valuesD (fun x ->
                    (fun l -> logfi l "%f{balance,-10:C}" x)
                    |> assertEquivalent
                        (Helpers.StringFormat("{0,-10:C}", x))
                        ["balance", x]
                )
                theory "Hex format" valuesI (fun x ->
                    (fun l -> logfi l "%i{value:X}" x)
                    |> assertEquivalent
                        (Helpers.StringFormat("{0:X}", x))
                        ["value", x]
                )
                theory "Corner case 1" valuesI (fun x ->
                    (fun l -> logfi l "%i{value}:X}" x)
                    |> assertEquivalent
                        (sprintf "%i:X}" x)
                        ["value", x]
                )
                theory "Corner case 2" valuesI (fun x ->
                    (fun l -> logfi l "%i{value},3}" x)
                    |> assertEquivalent
                        (sprintf "%i,3}" x)
                        ["value", x]
                )
            ]
            testList "printf format specifiers" [
                theory "Hex format" valuesI (fun x ->
                    (fun l -> logfi l "%x{value}" x)
                    |> assertEquivalentM "little x"
                        (sprintf "%x" x)
                        ["value", x]
                    (fun l -> logfi l "%X{value}" x)
                    |> assertEquivalentM "big X"
                        (sprintf "%X" x)
                        ["value", x]
                )
                theory "Float with width 0 and 0 right decimal places" valuesF (fun x ->
                    (fun l -> logfi l "%0.0f{value}" x)
                    |> assertEquivalent
                        (sprintf "%0.0f" x)
                        ["value", x]
                )
                theory "Float with 1 right decimal place" valuesF (fun x ->
                    (fun l -> logfi l "%.1f{value}" x)
                    |> assertEquivalent
                        (sprintf "%0.1f" x)
                        ["value", x]
                )
                theory "Float with 2 right decimal places" valuesF (fun x ->
                    (fun l -> logfi l "%.2f{value}" x)
                    |> assertEquivalent
                        (sprintf "%.2f" x)
                        ["value", x]
                )
                theory "Float with 10 right decimal places" valuesF (fun x ->
                    (fun l -> logfi l "%.10f{value}" x)
                    |> assertEquivalent
                        (sprintf "%.10f" x)
                        ["value", x]
                )
                theory "Float with width of 1 and default number of right decimal places" valuesF (fun x ->
                    (fun l -> logfi l "%1f{value}" x)
                    |> assertEquivalent
                        (sprintf "%1f" x)
                        ["value", x]
                )
                theory "Float with width of 2 and default number of right decimal places" valuesF (fun x ->
                    (fun l -> logfi l "%2f{value}" x)
                    |> assertEquivalent
                        (sprintf "%2f" x)
                        ["value", x]
                )
                theory "Float with width of 10 and default number of right decimal places" valuesF (fun x ->
                    (fun l -> logfi l "%10f{value}" x)
                    |> assertEquivalent
                        (sprintf "%10f" x)
                        ["value", x]
                )
                theory "Float with width of 2 and 3 right decimal places" valuesF (fun x ->
                    (fun l -> logfi l "%2.3f{value}" x)
                    |> assertEquivalent
                        (sprintf "%2.3f" x)
                        ["value", x]
                )
                theory "Float with 0-padded width of 10 and 3 right decimal places" valuesF (fun x ->
                    // sprintf "%010.3f" 42.0
                    // String.Format("{0:000000.000}", 42.0)
                    (fun l -> logfi l "%010.3f{value}" x)
                    |> assertEquivalent
                        (sprintf "%010.3f" x)
                        ["value", x]
                )
                theory "Float with + flag" valuesF (fun x ->
                    (fun l -> logfi l "%+2.3f{value}" x)
                    |> assertEquivalent
                        (sprintf "%+2.3f" x)
                        ["value", x]
                )
                theory "Several interspersed format specifiers"
                    (caseData [
                        42.59, false, 0xcafebabe
                        123.45, true, 0xdeadbeef ])
                    (fun (x,y,z) ->
                        (fun l -> logfi l "%4.1f{float}, %b{boolean}, %x{hex}" x y z)
                        |> assertEquivalent
                            (sprintf "%4.1f, %b, %x" x y z)
                            ["float", x; "boolean", y; "hex", z]
                    )
                theory "Exponential notation" valuesF (fun x ->
                    (fun l -> logfi l "%e{value}" x)
                    |> assertEquivalentM "little e"
                        (sprintf "%e" x)
                        ["value", x]
                    (fun l -> logfi l "%E{value}" x)
                    |> assertEquivalentM "big E"
                        (sprintf "%E" x)
                        ["value", x]
                )
            ]
        ]
        testList "SharedTests" [
            let dummyExn = makeDummyException ()
            let mkLogfCaseData () =
                let funcs =
                    [
                        nameof(logf), (logf, None)
                        nameof(elogf), ((fun logger level args -> elogf logger level dummyExn args), Some dummyExn)
                    ]
                let levels = [ LogLevel.Trace; LogLevel.Critical ]
                List.allPairs funcs levels
                |> List.map (fun ((logfFuncName, (logfFunc, err)), level) ->
                    (sprintf "%s,%O" logfFuncName level), (logfFunc, { LogLine.empty with error = err; logLevel = level}, level))
            testList "Escapes curly braces not part of a named parameter" [
                // .NET impl should escape the curly braces lest they be interpreted as a message template by the ILogger object.
                // Fable impl doesn't need to do this since the thing printing won't actually output named parameters anyway.
                theory "case 1" (mkLogfCaseData ()) (fun (logfFunc, emptyLogLine, level) ->
                    let l = mkLogger ()
                    logfFunc l level "%s{" "Yo"
                    
                    l.LastLine |> Expect.equal "Log lines"
#if !FABLE_COMPILER
                        { emptyLogLine with message = "Yo{{" }
#else
                        { emptyLogLine with message = "Yo{" }
#endif
                )
                theory "case 2" (mkLogfCaseData ()) (fun (logfFunc, emptyLogLine, level) ->
                    let l = mkLogger ()
                    logfFunc l level "%s{%d}" "SomeString" 42
                    
                    l.LastLine |> Expect.equal "Log lines"
#if !FABLE_COMPILER
                        { emptyLogLine with message = "SomeString{{42}}" }
#else
                        { emptyLogLine with message = "SomeString{42}" }
#endif
                )
                theory "case 3" (mkLogfCaseData ()) (fun (logfFunc, emptyLogLine, level) ->
                    let l = mkLogger ()
                    logfFunc l level "%s{}%d {iaminvalid}" "XYZ" -797
                    
                    l.LastLine |> Expect.equal "Log lines"
#if !FABLE_COMPILER
                        { emptyLogLine with message = "XYZ{{}}-797 {{iaminvalid}}" }
#else
                        { emptyLogLine with message = "XYZ{}-797 {iaminvalid}" }
#endif
                )
                theory "case 4" (mkLogfCaseData ()) (fun (logfFunc, emptyLogLine, level) ->
                    let l = mkLogger ()
                    logfFunc l level "{%s{}%d}" "XYZ" -797
                    
                    l.LastLine |> Expect.equal "Log lines"
#if !FABLE_COMPILER
                        { emptyLogLine with message = "{{XYZ{{}}-797}}" }
#else
                        { emptyLogLine with message = "{XYZ{}-797}" }
#endif
                )
                theory "case 5" (mkLogfCaseData ()) (fun (logfFunc, emptyLogLine, level) ->
                    let l = mkLogger ()
                    // Note: %f.10 isn't a valid format specifier, which is why the {x} should get escaped
                    logfFunc l level "{%.4f{}%d}%f.10{x}" 100.2 -797 1.
                    
                    l.LastLine |> Expect.equal "Log lines"
#if !FABLE_COMPILER
                        { emptyLogLine with message = "{{100.2000{{}}-797}}1.000000.10{{x}}" }
#else
                        { emptyLogLine with message = "{100.2000{}-797}1.000000.10{x}" }
#endif
                )
            ]
            
            testList "Can print various named parameters" [
                theory "basic case" (mkLogfCaseData ()) (fun (logfFunc, emptyLogLine, level) ->
                    let l = mkLogger ()
                    
                    logfFunc l level "Drawing rectangle with dimensions: %f{width},%f{height}" 100. 234.
                    l.LastLine |> Expect.equal "Log lines"
#if !FABLE_COMPILER
                        { emptyLogLine with message = "Drawing rectangle with dimensions: {width},{height}"; args = ["width", 100.0; "height", 234.0] }
#else
                        { emptyLogLine with message = "Drawing rectangle with dimensions: 100.000000,234.000000" }
#endif
                )
                theory "params reversed" (mkLogfCaseData ()) (fun (logfFunc, emptyLogLine, level) ->
                    let l = mkLogger ()
                    
                    logfFunc l level "Drawing rectangle with dimensions (reversed): %f{height},%f{width}" 234. 100.
                    l.LastLine |> Expect.equal "Log lines" 
#if !FABLE_COMPILER
                        { emptyLogLine with message = "Drawing rectangle with dimensions (reversed): {height},{width}"; args = ["height", 234.0; "width", 100.0] }
#else
                        { emptyLogLine with message = "Drawing rectangle with dimensions (reversed): 234.000000,100.000000" }
#endif
                )
                theory "param names may contain alphanumerics or underscores" (mkLogfCaseData ()) (fun (logfFunc, emptyLogLine, level) ->
                    let l = mkLogger ()
                    
                    logfFunc l level "%f{param1},%f{2param},%f{foo_bar}" 234. 100. 3.
                    l.LastLine |> Expect.equal "Log lines" 
#if !FABLE_COMPILER
                        { emptyLogLine with message = "{param1},{2param},{foo_bar}"; args = ["param1", 234.0; "2param", 100.0; "foo_bar", 3.] }
#else
                        { emptyLogLine with message = "234.000000,100.000000,3.000000" }
#endif
                )
                theory "fmt specs with precision" (mkLogfCaseData ()) (fun (logfFunc, emptyLogLine, level) ->
                    let l = mkLogger ()
                    
                    logfFunc l level "Params: %.2f{a},%.3f{b},%.10f{c}" 234.2 100.3 544.5
                    l.LastLine |> Expect.equal "Log lines"
#if !FABLE_COMPILER
                        { emptyLogLine with message = "Params: {a:0.00;-.00},{b:0.000;-.000},{c:0.0000000000;-.0000000000}"; args = ["a", 234.2; "b", 100.3; "c", 544.5] }
#else
                        { emptyLogLine with message = "Params: 234.20,100.300,544.5000000000" }
#endif
                )
                theory "fmt specs with flags, width, and precision" (mkLogfCaseData ()) (fun (logfFunc, emptyLogLine, level) ->
                    let l = mkLogger ()
                    
                    logfFunc l level "%0-2.3f{xyz} %0+-10f{abc} %+.5f{d} %5.5f{w}" 1. 2. 3. 4.
#if !FABLE_COMPILER
                    l.LastLine |> Expect.equal "Log lines"
                        { emptyLogLine with message = "{xyz:0.000;-.000} {abc:000.000000;-00.000000} {d:+0.00000;-.00000} {w,5:0.00000;-.00000}"; args = ["xyz", 1.; "abc", 2.; "d", 3.; "w", 4.] }
#else
                    l.LastLine |> Expect.equal "Log lines"
                        { emptyLogLine with message = "1.000 +2.000000  +3.00000 4.00000" }
#endif
                )
            ]
        ]
        testList "Functions log at the correct level" [
            for (level, logfVariant, elogfVariantOpt) in [
                LogLevel.Trace, logft, None
                LogLevel.Debug, logfd, None
                LogLevel.Information, logfi, None
                LogLevel.Warning, logfw, Some elogfw
                LogLevel.Error, logfe, Some elogfe
                LogLevel.Critical, logfc, Some elogfc
            ] do
                // see https://github.com/fable-compiler/Fable/issues/3315
                yield testCase (Enum.GetName(typeof<LogLevel>, level)) (fun () ->
                    let l = mkLogger ()
                    logf l level "Hello, %s!" "world"
                    logfVariant l "Hello, %s!" "world"
                    l.Lines[0].logLevel |> Expect.equal (nameof(Unchecked.defaultof<LogLine>.logLevel)) level
                    l.Lines[0].message |> Expect.equal (nameof(Unchecked.defaultof<LogLine>.message)) "Hello, world!"
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
