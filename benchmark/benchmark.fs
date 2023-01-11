module Benchmark
open System
open System.IO
open FSharp.Logf
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Logging.Abstractions
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

[<SimpleJob(RuntimeMoniker.Net60)>]
type Benchmarks() =
    // Serilog.Sinks.InMemory.InMemorySink()
    [<Params(100, 1_000)>]
    member val size = 0 with get, set
    
    member this.LogMethodNoParams (ml: ILogger) =
        for _ in 0 .. this.size do
            ml.LogCritical "Hello, world!"
    
    member this.LogfNoParams (ml: ILogger) =
        for _ in 0 .. this.size do
            logfc ml "Hello, world!"
    
    member this.LogMethodOneParam (ml: ILogger) =
        for i in 0 .. this.size do
            ml.LogCritical ("Hello, world! {a}", i)
    
    member this.LogfOneParam (ml: ILogger) =
        for i in 0 .. this.size do
            logfc ml "Hello, world! %i{a}" i
            
    member this.LogMethodTwoParams (ml: ILogger) =
        for i in 0 .. this.size do
            ml.LogCritical ("Hello, world! {a} {b}", i, "yo")
    
    member this.LogfTwoParams (ml: ILogger) =
        for i in 0 .. this.size do
            logfc ml "Hello, world! %i{a} %s{b}" i "yo"
    
    [<Benchmark(Baseline = true)>]
    member this.NullLoggerNoParams () = this.LogMethodNoParams NullLogger.Instance
    [<Benchmark>]
    member this.NullLoggerLogfNoParams () = this.LogfNoParams NullLogger.Instance
    [<Benchmark>]
    member this.NullLoggerOneParam () = this.LogMethodOneParam NullLogger.Instance
    [<Benchmark>]
    member this.NullLoggerLogfOneParam () = this.LogfOneParam NullLogger.Instance
    [<Benchmark>]
    member this.NullLoggerTwoParams () = this.LogMethodTwoParams NullLogger.Instance
    [<Benchmark>]
    member this.NullLoggerLogfTwoParams () = this.LogfTwoParams NullLogger.Instance

BenchmarkRunner.Run<Benchmarks>() |> ignore
