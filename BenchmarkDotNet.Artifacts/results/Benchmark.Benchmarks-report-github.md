``` ini

BenchmarkDotNet=v0.13.3, OS=macOS Monterey 12.6 (21G115) [Darwin 21.6.0]
Apple M1 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK=6.0.302
  [Host]   : .NET 6.0.12 (6.0.1222.56807), Arm64 RyuJIT AdvSIMD DEBUG
  .NET 6.0 : .NET 6.0.12 (6.0.1222.56807), Arm64 RyuJIT AdvSIMD

Job=.NET 6.0  Runtime=.NET 6.0  InvocationCount=1  
UnrollFactor=1  

```
|        Method |   size |          provider |          Mean |         Error |        StdDev |        Median |  Ratio | RatioSD |
|-------------- |------- |------------------ |--------------:|--------------:|--------------:|--------------:|-------:|--------:|
|      **NoParams** |   **1000** |        **NullLogger** |      **11.93 μs** |      **0.239 μs** |      **0.255 μs** |      **11.81 μs** |   **1.00** |    **0.00** |
|  LogfNoParams |   1000 |        NullLogger |   2,364.24 μs |     44.332 μs |     41.468 μs |   2,363.31 μs | 198.25 |    5.67 |
|      OneParam |   1000 |        NullLogger |      89.01 μs |      1.762 μs |      2.411 μs |      87.63 μs |   7.53 |    0.28 |
|  LogfOneParam |   1000 |        NullLogger |   3,439.13 μs |     68.667 μs |    141.809 μs |   3,379.31 μs | 300.81 |   14.64 |
|     TwoParams |   1000 |        NullLogger |      90.96 μs |      1.405 μs |      1.173 μs |      90.62 μs |   7.61 |    0.22 |
| LogfTwoParams |   1000 |        NullLogger |   3,963.52 μs |     44.286 μs |     36.981 μs |   3,944.75 μs | 331.69 |    8.56 |
|     TenParams |   1000 |        NullLogger |      90.71 μs |      0.537 μs |      0.476 μs |      90.87 μs |   7.60 |    0.16 |
| LogfTenParams |   1000 |        NullLogger |   3,969.54 μs |     34.020 μs |     31.822 μs |   3,961.50 μs | 332.88 |    8.60 |
|               |        |                   |               |               |               |               |        |         |
|      **NoParams** |   **1000** | **SerilogFileLogger** |   **4,228.14 μs** |     **83.639 μs** |    **142.026 μs** |   **4,189.58 μs** |   **1.00** |    **0.00** |
|  LogfNoParams |   1000 | SerilogFileLogger |   7,612.46 μs |     94.853 μs |     79.207 μs |   7,600.38 μs |   1.76 |    0.06 |
|      OneParam |   1000 | SerilogFileLogger |   4,819.75 μs |    114.839 μs |    325.779 μs |   4,750.67 μs |   1.21 |    0.07 |
|  LogfOneParam |   1000 | SerilogFileLogger |  10,104.81 μs |    280.172 μs |    808.359 μs |  10,048.73 μs |   2.53 |    0.15 |
|     TwoParams |   1000 | SerilogFileLogger |   5,234.28 μs |    164.147 μs |    470.969 μs |   5,175.79 μs |   1.35 |    0.08 |
| LogfTwoParams |   1000 | SerilogFileLogger |  11,129.11 μs |    188.321 μs |    231.275 μs |  11,041.35 μs |   2.60 |    0.09 |
|     TenParams |   1000 | SerilogFileLogger |   5,277.71 μs |     64.968 μs |     57.593 μs |   5,264.94 μs |   1.22 |    0.04 |
| LogfTenParams |   1000 | SerilogFileLogger |  10,980.58 μs |    122.658 μs |     95.763 μs |  10,963.92 μs |   2.53 |    0.07 |
|               |        |                   |               |               |               |               |        |         |
|      **NoParams** |  **10000** |        **NullLogger** |     **115.42 μs** |      **2.073 μs** |      **1.838 μs** |     **114.35 μs** |   **1.00** |    **0.00** |
|  LogfNoParams |  10000 |        NullLogger |  14,777.76 μs |    277.620 μs |    731.362 μs |  14,726.50 μs | 135.39 |    8.76 |
|      OneParam |  10000 |        NullLogger |     870.41 μs |     11.849 μs |     11.084 μs |     869.62 μs |   7.54 |    0.16 |
|  LogfOneParam |  10000 |        NullLogger |  24,748.06 μs |  1,930.610 μs |  5,692.447 μs |  21,104.46 μs | 302.34 |    9.65 |
|     TwoParams |  10000 |        NullLogger |     915.33 μs |     10.822 μs |     10.123 μs |     917.42 μs |   7.93 |    0.20 |
| LogfTwoParams |  10000 |        NullLogger |  25,140.62 μs |    337.173 μs |    754.136 μs |  25,012.12 μs | 221.97 |   11.69 |
|     TenParams |  10000 |        NullLogger |     896.48 μs |     12.944 μs |     12.108 μs |     895.38 μs |   7.77 |    0.17 |
| LogfTenParams |  10000 |        NullLogger |  25,089.21 μs |    358.318 μs |    830.457 μs |  24,745.52 μs | 229.55 |    5.12 |
|               |        |                   |               |               |               |               |        |         |
|      **NoParams** |  **10000** | **SerilogFileLogger** |  **29,458.14 μs** |    **255.072 μs** |    **453.391 μs** |  **29,335.75 μs** |   **1.00** |    **0.00** |
|  LogfNoParams |  10000 | SerilogFileLogger |  48,105.27 μs |    397.057 μs |    569.447 μs |  47,893.25 μs |   1.63 |    0.03 |
|      OneParam |  10000 | SerilogFileLogger |  34,364.64 μs |    662.969 μs |  1,412.841 μs |  33,787.98 μs |   1.17 |    0.06 |
|  LogfOneParam |  10000 | SerilogFileLogger |  62,366.69 μs |  1,232.673 μs |  2,626.927 μs |  61,550.92 μs |   2.13 |    0.11 |
|     TwoParams |  10000 | SerilogFileLogger |  36,528.52 μs |    476.154 μs |    858.604 μs |  36,280.75 μs |   1.24 |    0.04 |
| LogfTwoParams |  10000 | SerilogFileLogger |  68,287.10 μs |    886.338 μs |  1,152.490 μs |  67,760.60 μs |   2.32 |    0.05 |
|     TenParams |  10000 | SerilogFileLogger |  36,206.38 μs |    388.697 μs |    477.355 μs |  36,093.54 μs |   1.23 |    0.02 |
| LogfTenParams |  10000 | SerilogFileLogger |  67,849.00 μs |    694.276 μs |    771.686 μs |  67,597.35 μs |   2.30 |    0.04 |
|               |        |                   |               |               |               |               |        |         |
|      **NoParams** | **100000** |        **NullLogger** |   **1,134.63 μs** |     **22.652 μs** |     **23.262 μs** |   **1,130.08 μs** |   **1.00** |    **0.00** |
|  LogfNoParams | 100000 |        NullLogger | 141,744.01 μs |    391.365 μs |    366.083 μs | 141,775.12 μs | 124.86 |    2.76 |
|      OneParam | 100000 |        NullLogger |   8,589.84 μs |     78.360 μs |     73.298 μs |   8,583.21 μs |   7.57 |    0.17 |
|  LogfOneParam | 100000 |        NullLogger | 210,982.00 μs |    656.486 μs |    548.195 μs | 211,009.92 μs | 186.33 |    3.83 |
|     TwoParams | 100000 |        NullLogger |   9,080.50 μs |     69.637 μs |     65.139 μs |   9,082.29 μs |   8.00 |    0.19 |
| LogfTwoParams | 100000 |        NullLogger | 249,480.98 μs |    492.135 μs |    460.343 μs | 249,440.69 μs | 219.76 |    4.81 |
|     TenParams | 100000 |        NullLogger |   5,468.15 μs |     90.405 μs |    192.661 μs |   5,422.33 μs |   4.99 |    0.15 |
| LogfTenParams | 100000 |        NullLogger | 249,649.49 μs |    904.652 μs |    755.425 μs | 249,897.90 μs | 220.47 |    4.30 |
|               |        |                   |               |               |               |               |        |         |
|      **NoParams** | **100000** | **SerilogFileLogger** | **290,219.73 μs** |  **1,383.448 μs** |  **1,080.105 μs** | **290,052.77 μs** |   **1.00** |    **0.00** |
|  LogfNoParams | 100000 | SerilogFileLogger | 480,186.33 μs |  9,490.989 μs | 11,298.347 μs | 474,039.75 μs |   1.65 |    0.03 |
|      OneParam | 100000 | SerilogFileLogger | 331,077.06 μs |  1,235.300 μs |  1,031.532 μs | 330,783.21 μs |   1.14 |    0.01 |
|  LogfOneParam | 100000 | SerilogFileLogger | 601,871.33 μs | 11,993.135 μs | 12,316.066 μs | 594,543.29 μs |   2.08 |    0.05 |
|     TwoParams | 100000 | SerilogFileLogger | 359,443.47 μs |  2,886.054 μs |  2,253.241 μs | 358,915.40 μs |   1.24 |    0.01 |
| LogfTwoParams | 100000 | SerilogFileLogger | 673,104.63 μs | 13,298.810 μs | 14,781.592 μs | 665,508.33 μs |   2.32 |    0.06 |
|     TenParams | 100000 | SerilogFileLogger | 357,380.67 μs |  1,022.571 μs |    798.356 μs | 357,611.75 μs |   1.23 |    0.01 |
| LogfTenParams | 100000 | SerilogFileLogger | 671,026.55 μs | 12,729.530 μs | 12,502.100 μs | 663,720.27 μs |   2.32 |    0.04 |
