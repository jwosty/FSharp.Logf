module Benchmark
open System
open System.IO
open FSharp.Logf
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Logging.Abstractions
open Serilog
open Serilog.Sinks
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open BenchmarkDotNet.Jobs
open BenchmarkDotNet.Diagnosers

[<RequireQualifiedAccess>]
type Provider = | NullLogger = 0 | SerilogFileLogger = 1 

type MLogger = Microsoft.Extensions.Logging.ILogger

// FS0104: Enums may take values outside known cases
#nowarn "0104"

[<SimpleJob(RuntimeMoniker.Net90)>]
// [<EventPipeProfiler(EventPipeProfile.CpuSampling)>]
type Benchmarks() =
    [<Params(10_000)>]
    member val size = 0 with get, set
    
    [<Params(Provider.NullLogger, Provider.SerilogFileLogger)>]
    member val provider = Unchecked.defaultof<Provider> with get, set
    
    member val providerInst = Unchecked.defaultof<Microsoft.Extensions.Logging.ILogger> with get, set
    member val cleanup: Option<IDisposable> = None with get, set
    
    [<IterationSetup>]
    member this.Setup () =
        match this.provider with
        | Provider.NullLogger -> this.providerInst <- NullLogger.Instance
        | Provider.SerilogFileLogger ->
            let tmpFile = Path.GetTempFileName()
            Console.WriteLine ("Creating temp log file: " + tmpFile)
            let cfg =
                (Serilog.LoggerConfiguration())
                    .WriteTo.File(Serilog.Formatting.Json.JsonFormatter(), tmpFile)
                    .MinimumLevel.Information()
            let sl = cfg.CreateLogger()
            let factory = new Serilog.Extensions.Logging.SerilogLoggerFactory(sl)
            this.providerInst <- factory.CreateLogger()
            this.cleanup <- Some {
                new IDisposable with
                    override this.Dispose () =
                        (factory : IDisposable).Dispose ()
                        (sl : IDisposable).Dispose ()
                        File.Delete tmpFile
            }
            
    [<IterationCleanup>]
    member this.Cleanup () =
        match this.cleanup with
        | Some c ->
            c.Dispose ()
            this.cleanup <- None
        | None -> ()
    
    member this.LogMethodNoParams (ml: MLogger, ?l) =
        let l = defaultArg l LogLevel.Critical
        for _ in 0 .. this.size do
            ml.Log (l, "Hello, world!")
    
    member this.LogfNoParams (ml: MLogger, ?l) =
        let l = defaultArg l LogLevel.Critical
        for _ in 0 .. this.size do
            logf ml l "Hello, world!"
    
    member this.LogMethodOneParam (ml: MLogger, ?l) =
        let l = defaultArg l LogLevel.Critical
        for i in 0 .. this.size do
            ml.Log (l, "Hello, world! {a}", i)
    
    member this.LogfOneParam (ml: MLogger, ?l) =
        let l = defaultArg l LogLevel.Critical
        for i in 0 .. this.size do
            logf ml l "Hello, world! %i{a}" i
            
    member this.LogMethodTwoParams (ml: MLogger, ?l) =
        let l = defaultArg l LogLevel.Critical
        for i in 0 .. this.size do
            ml.Log (l, "Hello, world! {a} {b}", i, "yo")
    
    member this.LogfTwoParams (ml: MLogger, ?l) =
        let l = defaultArg l LogLevel.Critical
        for i in 0 .. this.size do
            logf ml l "Hello, world! %i{a} %s{b}" i "yo"
            
    member this.LogMethodTenParams (ml: MLogger, ?l) =
        let l = defaultArg l LogLevel.Critical
        // generated from:
        // printfn "ml.Log (l, \"%s\", %s)"
        //     ([for i in 0..9 -> sprintf "param %i: {%c}" i (char 'a' + char i)] |> String.concat ", ")
        //     ([for i in 0..9 -> sprintf "i-%i" i] |> String.concat ", ")
        for i in 0 .. this.size do
            ml.Log (l, "param 0: {a}, param 1: {b}, param 2: {c}, param 3: {d}, param 4: {e}, param 5: {f}, param 6: {g}, param 7: {h}, param 8: {i}, param 9: {j}", i-0, i-1, i-2, i-3, i-4, i-5, i-6, i-7, i-8, i-9)
    
    member this.LogfTenParams (ml: MLogger, ?l) =
        let l = defaultArg l LogLevel.Critical
        // generated with:
        // printfn "logf ml l \"%s\" %s"
        //     ([for i in 0..9 -> sprintf "param %i: %%i{%c}" i (char 'a' + char i)] |> String.concat ", ")
        //     ([for i in 0..9 -> sprintf "(i-%i)" i] |> String.concat " ")
        for i in 0 .. this.size do
            logf ml l "param 0: %i{a}, param 1: %i{b}, param 2: %i{c}, param 3: %i{d}, param 4: %i{e}, param 5: %i{f}, param 6: %i{g}, param 7: %i{h}, param 8: %i{i}, param 9: %i{j}" (i-0) (i-1) (i-2) (i-3) (i-4) (i-5) (i-6) (i-7) (i-8) (i-9)
    
    // do some %A for bonus points
    member this.LogfTenParamsPercentA (ml: MLogger, ?l) =
        let l = defaultArg l LogLevel.Critical
        // generated with:
        // printfn "let o = new Object()\nlogfc ml \"%s\" %s"
        //     ([for i in 0..9 -> sprintf "param %i: %%A{%c}" i (char 'a' + char i)] |> String.concat ", ")
        //     ([for i in 0..9 -> "o"] |> String.concat " ")
        for i in 0 .. this.size do
            let o = new Object()
            logf ml l "param 0: %A{a}, param 1: %A{b}, param 2: %A{c}, param 3: %A{d}, param 4: %A{e}, param 5: %A{f}, param 6: %A{g}, param 7: %A{h}, param 8: %A{i}, param 9: %A{j}" o o o o o o o o o o
    
    [<Benchmark(Baseline = true)>]
    member this.NoParams () = this.LogMethodNoParams this.providerInst
    [<Benchmark>]
    member this.LogfNoParams () = this.LogfNoParams this.providerInst
    [<Benchmark>]
    member this.OneParam () = this.LogMethodOneParam this.providerInst
    [<Benchmark>]
    member this.LogfOneParam () = this.LogfOneParam this.providerInst
    [<Benchmark>]
    member this.TwoParams () = this.LogMethodTwoParams this.providerInst
    [<Benchmark>]
    member this.LogfTwoParams () = this.LogfTwoParams this.providerInst
    [<Benchmark>]
    member this.TwoParamsNoop () = this.LogMethodTwoParams (this.providerInst, LogLevel.Debug)
    [<Benchmark>]
    member this.LogfTwoParamsNoop () = this.LogfTwoParams (this.providerInst, LogLevel.Debug)
    [<Benchmark>]
    member this.TenParams () = this.LogMethodTwoParams this.providerInst
    [<Benchmark>]
    member this.LogfTenParams () = this.LogfTwoParams this.providerInst
    [<Benchmark>]
    member this.LogfTenParamsPercentA () = this.LogfTwoParams this.providerInst
    

module Main =
    [<EntryPoint>]
    let main args =
        BenchmarkRunner.Run<Benchmarks>(args = args) |> ignore
        0
