``` ini

BenchmarkDotNet=v0.13.5, OS=macOS Monterey 12.6 (21G115) [Darwin 21.6.0]
Apple M1 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK=7.0.102
  [Host]   : .NET 7.0.4 (7.0.423.11508), Arm64 RyuJIT AdvSIMD DEBUG
  .NET 7.0 : .NET 7.0.4 (7.0.423.11508), Arm64 RyuJIT AdvSIMD

Job=.NET 7.0  Runtime=.NET 7.0  InvocationCount=1  
UnrollFactor=1  

```
|                Method |  size |          provider |        Mean |       Error |       StdDev |      Median |  Ratio | RatioSD |
|---------------------- |------ |------------------ |------------:|------------:|-------------:|------------:|-------:|--------:|
|              **NoParams** | **10000** |        **NullLogger** |    **122.0 μs** |     **1.59 μs** |      **1.33 μs** |    **122.3 μs** |   **1.00** |    **0.00** |
|          LogfNoParams | 10000 |        NullLogger |  2,073.9 μs |    40.49 μs |     64.22 μs |  2,066.9 μs |  17.23 |    0.59 |
|              OneParam | 10000 |        NullLogger |    584.0 μs |     8.80 μs |     10.48 μs |    579.2 μs |   4.81 |    0.09 |
|          LogfOneParam | 10000 |        NullLogger | 12,381.4 μs |   167.44 μs |    139.82 μs | 12,375.2 μs | 101.46 |    1.43 |
|             TwoParams | 10000 |        NullLogger |    642.2 μs |    12.77 μs |     14.70 μs |    640.3 μs |   5.29 |    0.15 |
|         LogfTwoParams | 10000 |        NullLogger | 14,343.1 μs |   143.47 μs |    305.74 μs | 14,281.5 μs | 118.72 |    3.42 |
|         TwoParamsNoop | 10000 |        NullLogger |    643.6 μs |    12.56 μs |     12.33 μs |    642.2 μs |   5.29 |    0.12 |
|     LogfTwoParamsNoop | 10000 |        NullLogger | 15,760.4 μs |   821.98 μs |  2,423.63 μs | 14,351.6 μs | 168.37 |    7.27 |
|             TenParams | 10000 |        NullLogger |    625.7 μs |     1.19 μs |      0.93 μs |    626.0 μs |   5.13 |    0.06 |
|         LogfTenParams | 10000 |        NullLogger | 14,126.6 μs |    88.08 μs |    193.34 μs | 14,032.5 μs | 116.22 |    1.98 |
| LogfTenParamsPercentA | 10000 |        NullLogger | 14,195.1 μs |   153.53 μs |    333.76 μs | 14,064.1 μs | 117.76 |    4.90 |
|                       |       |                   |             |             |              |             |        |         |
|              **NoParams** | **10000** | **SerilogFileLogger** | **32,374.2 μs** | **4,941.73 μs** | **13,938.22 μs** | **25,409.6 μs** |   **1.00** |    **0.00** |
|          LogfNoParams | 10000 | SerilogFileLogger | 26,232.1 μs |   576.45 μs |  1,606.90 μs | 25,817.1 μs |   0.91 |    0.25 |
|              OneParam | 10000 | SerilogFileLogger | 26,390.6 μs |   492.75 μs |  1,323.75 μs | 25,857.3 μs |   0.90 |    0.25 |
|          LogfOneParam | 10000 | SerilogFileLogger | 35,333.3 μs |   673.83 μs |  1,197.74 μs | 34,926.7 μs |   0.97 |    0.36 |
|             TwoParams | 10000 | SerilogFileLogger | 27,587.1 μs |   458.73 μs |    872.78 μs | 27,492.8 μs |   0.80 |    0.29 |
|         LogfTwoParams | 10000 | SerilogFileLogger | 43,478.5 μs |   824.18 μs |  1,826.33 μs | 42,630.2 μs |   1.38 |    0.44 |
|         TwoParamsNoop | 10000 | SerilogFileLogger |    729.8 μs |     6.53 μs |      5.45 μs |    728.3 μs |   0.02 |    0.00 |
|     LogfTwoParamsNoop | 10000 | SerilogFileLogger | 14,447.3 μs |   150.60 μs |    336.83 μs | 14,404.2 μs |   0.46 |    0.15 |
|             TenParams | 10000 | SerilogFileLogger | 27,524.1 μs |   547.17 μs |    767.06 μs | 27,485.7 μs |   0.61 |    0.22 |
|         LogfTenParams | 10000 | SerilogFileLogger | 43,576.9 μs |   824.61 μs |  1,810.05 μs | 42,777.8 μs |   1.37 |    0.44 |
| LogfTenParamsPercentA | 10000 | SerilogFileLogger | 42,997.8 μs |   817.12 μs |    637.96 μs | 42,865.1 μs |   1.00 |    0.31 |
