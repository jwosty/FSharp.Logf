``` ini

BenchmarkDotNet=v0.13.3, OS=macOS Monterey 12.6 (21G115) [Darwin 21.6.0]
Apple M1 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK=7.0.101
  [Host] : .NET 7.0.1 (7.0.122.56804), Arm64 RyuJIT AdvSIMD DEBUG

Job=.NET 6.0  Runtime=.NET 6.0  InvocationCount=1  
UnrollFactor=1  

```
|   Method |  size |   provider | Mean | Error | Ratio | RatioSD |
|--------- |------ |----------- |-----:|------:|------:|--------:|
| NoParams | 10000 | NullLogger |   NA |    NA |     ? |       ? |

Benchmarks with issues:
  Benchmarks.NoParams: .NET 6.0(Runtime=.NET 6.0, InvocationCount=1, UnrollFactor=1) [size=10000, provider=NullLogger]
