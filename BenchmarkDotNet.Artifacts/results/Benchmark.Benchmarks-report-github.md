``` ini

BenchmarkDotNet=v0.13.3, OS=macOS Monterey 12.6 (21G115) [Darwin 21.6.0]
Apple M1 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK=7.0.102
  [Host]   : .NET 7.0.2 (7.0.222.60605), Arm64 RyuJIT AdvSIMD DEBUG
  .NET 7.0 : .NET 7.0.2 (7.0.222.60605), Arm64 RyuJIT AdvSIMD

Job=.NET 7.0  Runtime=.NET 7.0  InvocationCount=1  
UnrollFactor=1  

```
|                Method |  size |          provider |        Mean |       Error |      StdDev |      Median |  Ratio | RatioSD |
|---------------------- |------ |------------------ |------------:|------------:|------------:|------------:|-------:|--------:|
|              **NoParams** | **10000** |        **NullLogger** |    **123.4 μs** |     **2.42 μs** |     **4.43 μs** |    **122.4 μs** |   **1.00** |    **0.00** |
|          LogfNoParams | 10000 |        NullLogger | 13,417.9 μs |   264.00 μs |   343.28 μs | 13,430.4 μs | 107.97 |    6.02 |
|              OneParam | 10000 |        NullLogger |    585.9 μs |     8.62 μs |     7.64 μs |    584.5 μs |   4.80 |    0.17 |
|          LogfOneParam | 10000 |        NullLogger | 15,556.3 μs | 1,143.58 μs | 3,371.88 μs | 13,450.7 μs | 151.36 |   26.07 |
|             TwoParams | 10000 |        NullLogger |    638.7 μs |    12.44 μs |    11.64 μs |    636.6 μs |   5.24 |    0.18 |
|         LogfTwoParams | 10000 |        NullLogger | 18,695.5 μs | 1,396.10 μs | 4,116.43 μs | 16,300.2 μs | 180.13 |   35.22 |
|         TwoParamsNoop | 10000 |        NullLogger |    635.1 μs |     4.56 μs |     3.56 μs |    632.9 μs |   5.20 |    0.19 |
|     LogfTwoParamsNoop | 10000 |        NullLogger | 18,951.0 μs | 1,521.57 μs | 4,486.37 μs | 16,394.4 μs | 183.36 |   39.87 |
|             TenParams | 10000 |        NullLogger |    646.6 μs |    12.68 μs |    14.60 μs |    645.0 μs |   5.23 |    0.28 |
|         LogfTenParams | 10000 |        NullLogger | 19,119.2 μs | 1,513.73 μs | 4,463.26 μs | 16,515.6 μs | 185.71 |   38.49 |
| LogfTenParamsPercentA | 10000 |        NullLogger | 19,050.2 μs | 1,427.10 μs | 4,207.83 μs | 16,667.1 μs | 183.06 |   36.82 |
|                       |       |                   |             |             |             |             |        |         |
|              **NoParams** | **10000** | **SerilogFileLogger** | **26,658.4 μs** |   **514.25 μs** | **1,212.14 μs** | **26,448.1 μs** |   **1.00** |    **0.00** |
|          LogfNoParams | 10000 | SerilogFileLogger | 36,615.5 μs |   347.45 μs |   669.41 μs | 36,498.0 μs |   1.37 |    0.06 |
|              OneParam | 10000 | SerilogFileLogger | 28,490.8 μs |   329.41 μs |   585.53 μs | 28,346.2 μs |   1.06 |    0.06 |
|          LogfOneParam | 10000 | SerilogFileLogger | 47,948.0 μs |   941.61 μs | 2,593.47 μs | 47,367.1 μs |   1.79 |    0.11 |
|             TwoParams | 10000 | SerilogFileLogger | 31,074.8 μs |   597.77 μs | 1,151.70 μs | 30,618.9 μs |   1.16 |    0.06 |
|         LogfTwoParams | 10000 | SerilogFileLogger | 52,581.7 μs |   920.23 μs | 1,750.83 μs | 52,077.8 μs |   1.96 |    0.10 |
|         TwoParamsNoop | 10000 | SerilogFileLogger |    749.3 μs |    12.56 μs |    11.75 μs |    750.2 μs |   0.03 |    0.00 |
|     LogfTwoParamsNoop | 10000 | SerilogFileLogger | 20,534.4 μs |   279.60 μs |   613.73 μs | 20,395.3 μs |   0.77 |    0.04 |
|             TenParams | 10000 | SerilogFileLogger | 31,775.0 μs |   634.57 μs | 1,789.82 μs | 31,391.3 μs |   1.20 |    0.08 |
|         LogfTenParams | 10000 | SerilogFileLogger | 56,729.2 μs | 1,318.58 μs | 3,609.60 μs | 55,770.3 μs |   2.15 |    0.14 |
| LogfTenParamsPercentA | 10000 | SerilogFileLogger | 51,753.0 μs |   530.31 μs |   760.55 μs | 51,656.1 μs |   1.91 |    0.10 |
