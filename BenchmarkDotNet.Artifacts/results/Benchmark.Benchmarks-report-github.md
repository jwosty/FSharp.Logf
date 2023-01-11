``` ini

BenchmarkDotNet=v0.13.3, OS=macOS Monterey 12.6 (21G115) [Darwin 21.6.0]
Apple M1 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK=6.0.302
  [Host]   : .NET 6.0.12 (6.0.1222.56807), Arm64 RyuJIT AdvSIMD DEBUG
  .NET 6.0 : .NET 6.0.12 (6.0.1222.56807), Arm64 RyuJIT AdvSIMD

Job=.NET 6.0  Runtime=.NET 6.0  

```
|                  Method | size |           Mean |       Error |      StdDev |  Ratio | RatioSD |
|------------------------ |----- |---------------:|------------:|------------:|-------:|--------:|
|      **NullLoggerNoParams** |  **100** |       **743.6 ns** |     **9.19 ns** |     **8.59 ns** |   **1.00** |    **0.00** |
|  NullLoggerLogfNoParams |  100 |   140,884.8 ns |   184.38 ns |   143.95 ns | 189.52 |    2.40 |
|      NullLoggerOneParam |  100 |     5,045.6 ns |    71.76 ns |    63.61 ns |   6.79 |    0.13 |
|  NullLoggerLogfOneParam |  100 |   210,518.2 ns | 2,754.70 ns | 2,150.68 ns | 283.21 |    5.55 |
|     NullLoggerTwoParams |  100 |     5,435.6 ns |    14.95 ns |    13.98 ns |   7.31 |    0.09 |
| NullLoggerLogfTwoParams |  100 |   253,110.8 ns |   332.85 ns |   311.35 ns | 340.43 |    3.89 |
|                         |      |                |             |             |        |         |
|      **NullLoggerNoParams** | **1000** |     **7,196.9 ns** |     **7.15 ns** |     **6.69 ns** |   **1.00** |    **0.00** |
|  NullLoggerLogfNoParams | 1000 | 1,392,560.4 ns | 2,028.31 ns | 1,897.28 ns | 193.50 |    0.34 |
|      NullLoggerOneParam | 1000 |    48,681.7 ns |   325.32 ns |   304.31 ns |   6.76 |    0.04 |
|  NullLoggerLogfOneParam | 1000 | 2,086,859.8 ns | 4,876.35 ns | 4,322.75 ns | 289.97 |    0.67 |
|     NullLoggerTwoParams | 1000 |    53,379.5 ns |   509.95 ns |   452.05 ns |   7.42 |    0.06 |
| NullLoggerLogfTwoParams | 1000 | 2,510,886.5 ns | 4,580.71 ns | 4,060.68 ns | 348.89 |    0.62 |
