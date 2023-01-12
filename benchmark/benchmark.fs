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

// [<SimpleJob (RuntimeMoniker.Net60)>]
// type Benchmarks() =
//     [<Params(100, 1000, 10000, 100000, 1000000)>]
//     member val size = 0 with get, set
//
//     [<Benchmark(Baseline = true)>]
//     member this.Array () = [| 0 .. this.size |] |> Array.map ((+) 1)
//     [<Benchmark>]
//     member this.List () = [ 0 .. this.size ] |> List.map ((+) 1)
//     [<Benchmark>]
//     member this.Seq () = seq { 0 .. this.size } |> Seq.map ((+) 1) |> Seq.length // force evaluation

[<RequireQualifiedAccess>]
type Provider = | NullLogger = 0 | SerilogFileLogger = 1 

type MLogger = Microsoft.Extensions.Logging.ILogger

// FS0104: Enums may take values outside known cases
#nowarn "0104"

[<SimpleJob(RuntimeMoniker.Net60)>]
type Benchmarks() =
    // Serilog.Sinks.InMemory.InMemorySink()
    [<Params(1_000, 10_000, 100_000)>]
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
            let cfg = (new Serilog.LoggerConfiguration()).WriteTo.File(Serilog.Formatting.Json.JsonFormatter(), tmpFile)
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
    
    member this.LogMethodNoParams (ml: MLogger) =
        for _ in 0 .. this.size do
            ml.LogCritical "Hello, world!"
    
    member this.LogfNoParams (ml: MLogger) =
        for _ in 0 .. this.size do
            logfc ml "Hello, world!"
    
    member this.LogMethodOneParam (ml: MLogger) =
        for i in 0 .. this.size do
            ml.LogCritical ("Hello, world! {a}", i)
    
    member this.LogfOneParam (ml: MLogger) =
        for i in 0 .. this.size do
            logfc ml "Hello, world! %i{a}" i
            
    member this.LogMethodTwoParams (ml: MLogger) =
        for i in 0 .. this.size do
            ml.LogCritical ("Hello, world! {a} {b}", i, "yo")
    
    member this.LogfTwoParams (ml: MLogger) =
        for i in 0 .. this.size do
            logfc ml "Hello, world! %i{a} %s{b}" i "yo"
            
    member this.LogMethodTenParams (ml: MLogger) =
        // generated from:
        // printfn "ml.LogCritical (\"%s\", %s)"
        //     ([for i in 0..9 -> sprintf "param %i: {%c}" i (char 'a' + char i)] |> String.concat ", ")
        //     ([for i in 0..9 -> sprintf "i-%i" i] |> String.concat ", ")
            
        for i in 0 .. this.size do
            ml.LogCritical ("param 0: {a}, param 1: {b}, param 2: {c}, param 3: {d}, param 4: {e}, param 5: {f}, param 6: {g}, param 7: {h}, param 8: {i}, param 9: {j}", i-0, i-1, i-2, i-3, i-4, i-5, i-6, i-7, i-8, i-9)
    
    member this.LogfTenParams (ml: MLogger) =
        // generated with:
        // printfn "logfc ml \"%s\" %s"
        //     ([for i in 0..9 -> sprintf "param %i: %%i{%c}" i (char 'a' + char i)] |> String.concat ", ")
        //     ([for i in 0..9 -> sprintf "(i-%i)" i] |> String.concat " ")
        for i in 0 .. this.size do
            logfc ml "param 0: %i{a}, param 1: %i{b}, param 2: %i{c}, param 3: %i{d}, param 4: %i{e}, param 5: %i{f}, param 6: %i{g}, param 7: %i{h}, param 8: %i{i}, param 9: %i{j}" (i-0) (i-1) (i-2) (i-3) (i-4) (i-5) (i-6) (i-7) (i-8) (i-9)
    
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
    member this.TenParams () = this.LogMethodTwoParams this.providerInst
    [<Benchmark>]
    member this.LogfTenParams () = this.LogfTwoParams this.providerInst

BenchmarkRunner.Run<Benchmarks>() |> ignore
