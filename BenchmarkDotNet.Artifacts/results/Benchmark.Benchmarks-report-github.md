``` ini

BenchmarkDotNet=v0.13.3, OS=macOS Monterey 12.6 (21G115) [Darwin 21.6.0]
Apple M1 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK=6.0.302
  [Host]   : .NET 6.0.12 (6.0.1222.56807), Arm64 RyuJIT AdvSIMD DEBUG
  .NET 6.0 : .NET 6.0.12 (6.0.1222.56807), Arm64 RyuJIT AdvSIMD

Job=.NET 6.0  Runtime=.NET 6.0  InvocationCount=1  
UnrollFactor=1  

```
|                Method |   size |          provider |          Mean |        Error |       StdDev |        Median |  Ratio | RatioSD |
|---------------------- |------- |------------------ |--------------:|-------------:|-------------:|--------------:|-------:|--------:|
|              **NoParams** |   **1000** |        **NullLogger** |      **11.75 μs** |     **0.220 μs** |     **0.206 μs** |      **11.71 μs** |   **1.00** |    **0.00** |
|          LogfNoParams |   1000 |        NullLogger |   2,352.03 μs |    33.899 μs |    28.307 μs |   2,359.17 μs | 200.56 |    4.46 |
|              OneParam |   1000 |        NullLogger |      88.27 μs |     1.764 μs |     1.650 μs |      88.60 μs |   7.52 |    0.23 |
|          LogfOneParam |   1000 |        NullLogger |   3,336.65 μs |    47.080 μs |    41.735 μs |   3,336.21 μs | 284.42 |    6.85 |
|             TwoParams |   1000 |        NullLogger |      90.38 μs |     0.537 μs |     0.476 μs |      90.19 μs |   7.70 |    0.12 |
|         LogfTwoParams |   1000 |        NullLogger |   3,986.27 μs |    42.403 μs |    39.664 μs |   3,996.75 μs | 339.39 |    4.81 |
|             TenParams |   1000 |        NullLogger |      90.49 μs |     0.563 μs |     0.499 μs |      90.54 μs |   7.71 |    0.15 |
|         LogfTenParams |   1000 |        NullLogger |   3,998.50 μs |    71.294 μs |    66.689 μs |   3,997.75 μs | 340.51 |    9.81 |
| LogfTenParamsPercentA |   1000 |        NullLogger |   3,922.31 μs |    55.265 μs |    51.695 μs |   3,908.42 μs | 334.02 |    9.01 |
|                       |        |                   |               |              |              |               |        |         |
|              **NoParams** |   **1000** | **SerilogFileLogger** |   **4,219.59 μs** |    **79.520 μs** |    **81.661 μs** |   **4,205.38 μs** |   **1.00** |    **0.00** |
|          LogfNoParams |   1000 | SerilogFileLogger |   7,718.17 μs |    92.799 μs |    86.804 μs |   7,671.38 μs |   1.83 |    0.04 |
|              OneParam |   1000 | SerilogFileLogger |   4,800.41 μs |    46.491 μs |    43.487 μs |   4,810.25 μs |   1.14 |    0.02 |
|          LogfOneParam |   1000 | SerilogFileLogger |   9,916.02 μs |    93.745 μs |    78.282 μs |   9,896.04 μs |   2.35 |    0.05 |
|             TwoParams |   1000 | SerilogFileLogger |   5,254.49 μs |    69.687 μs |    65.186 μs |   5,222.67 μs |   1.24 |    0.03 |
|         LogfTwoParams |   1000 | SerilogFileLogger |  10,771.91 μs |   110.609 μs |    98.052 μs |  10,757.21 μs |   2.55 |    0.06 |
|             TenParams |   1000 | SerilogFileLogger |   5,228.22 μs |    65.928 μs |    61.669 μs |   5,206.37 μs |   1.24 |    0.03 |
|         LogfTenParams |   1000 | SerilogFileLogger |  10,898.32 μs |   160.221 μs |   133.792 μs |  10,889.08 μs |   2.58 |    0.06 |
| LogfTenParamsPercentA |   1000 | SerilogFileLogger |  11,015.16 μs |   163.421 μs |   144.869 μs |  10,990.00 μs |   2.61 |    0.07 |
|                       |        |                   |               |              |              |               |        |         |
|              **NoParams** |  **10000** |        **NullLogger** |     **112.14 μs** |     **1.269 μs** |     **1.060 μs** |     **111.79 μs** |   **1.00** |    **0.00** |
|          LogfNoParams |  10000 |        NullLogger |  16,146.67 μs | 1,125.598 μs | 3,318.850 μs |  14,188.25 μs | 204.01 |    4.70 |
|              OneParam |  10000 |        NullLogger |     851.38 μs |    13.893 μs |    12.316 μs |     850.88 μs |   7.61 |    0.13 |
|          LogfOneParam |  10000 |        NullLogger |  23,294.41 μs | 1,489.760 μs | 4,369.207 μs |  20,942.77 μs | 289.82 |   10.25 |
|             TwoParams |  10000 |        NullLogger |     882.81 μs |     7.103 μs |     5.931 μs |     883.58 μs |   7.87 |    0.09 |
|         LogfTwoParams |  10000 |        NullLogger |  24,976.05 μs |   267.312 μs |   597.882 μs |  24,873.38 μs | 226.46 |   11.16 |
|             TenParams |  10000 |        NullLogger |     901.05 μs |    12.717 μs |    11.895 μs |     901.44 μs |   8.04 |    0.10 |
|         LogfTenParams |  10000 |        NullLogger |  24,719.39 μs |   338.955 μs |   778.805 μs |  24,571.08 μs | 224.81 |   14.69 |
| LogfTenParamsPercentA |  10000 |        NullLogger |  25,238.14 μs |   374.312 μs |   764.621 μs |  25,064.46 μs | 230.00 |   13.18 |
|                       |        |                   |               |              |              |               |        |         |
|              **NoParams** |  **10000** | **SerilogFileLogger** |  **29,709.11 μs** |   **527.362 μs** | **1,201.070 μs** |  **29,250.38 μs** |   **1.00** |    **0.00** |
|          LogfNoParams |  10000 | SerilogFileLogger |  47,823.43 μs |   243.161 μs |   432.218 μs |  47,706.29 μs |   1.60 |    0.07 |
|              OneParam |  10000 | SerilogFileLogger |  33,044.33 μs |   190.102 μs |   379.654 μs |  32,958.58 μs |   1.11 |    0.04 |
|          LogfOneParam |  10000 | SerilogFileLogger |  61,131.03 μs |   975.790 μs | 1,234.062 μs |  60,694.00 μs |   2.05 |    0.08 |
|             TwoParams |  10000 | SerilogFileLogger |  35,640.60 μs |   215.080 μs |   398.665 μs |  35,524.50 μs |   1.19 |    0.05 |
|         LogfTwoParams |  10000 | SerilogFileLogger |  68,209.23 μs |   395.713 μs |   604.296 μs |  68,070.67 μs |   2.30 |    0.09 |
|             TenParams |  10000 | SerilogFileLogger |  37,220.35 μs |   722.874 μs | 1,375.342 μs |  36,700.71 μs |   1.25 |    0.06 |
|         LogfTenParams |  10000 | SerilogFileLogger |  68,734.45 μs | 1,226.468 μs | 1,872.946 μs |  68,061.75 μs |   2.31 |    0.06 |
| LogfTenParamsPercentA |  10000 | SerilogFileLogger |  67,706.01 μs |   626.784 μs |   555.628 μs |  67,734.02 μs |   2.25 |    0.12 |
|                       |        |                   |               |              |              |               |        |         |
|              **NoParams** | **100000** |        **NullLogger** |   **1,117.50 μs** |    **17.909 μs** |    **16.752 μs** |   **1,114.25 μs** |   **1.00** |    **0.00** |
|          LogfNoParams | 100000 |        NullLogger | 141,037.53 μs |   170.991 μs |   151.579 μs | 140,986.42 μs | 126.17 |    1.87 |
|              OneParam | 100000 |        NullLogger |   8,520.59 μs |    97.667 μs |    91.358 μs |   8,540.60 μs |   7.63 |    0.13 |
|          LogfOneParam | 100000 |        NullLogger | 207,968.82 μs |   453.589 μs |   402.095 μs | 207,879.44 μs | 186.05 |    2.83 |
|             TwoParams | 100000 |        NullLogger |   9,022.34 μs |    95.008 μs |    88.870 μs |   8,993.83 μs |   8.08 |    0.15 |
|         LogfTwoParams | 100000 |        NullLogger | 246,559.28 μs |   670.201 μs |   594.116 μs | 246,632.81 μs | 220.57 |    3.16 |
|             TenParams | 100000 |        NullLogger |   8,996.33 μs |    80.538 μs |    71.395 μs |   8,998.54 μs |   8.05 |    0.15 |
|         LogfTenParams | 100000 |        NullLogger | 248,528.74 μs |   376.392 μs |   333.661 μs | 248,605.92 μs | 222.33 |    3.53 |
| LogfTenParamsPercentA | 100000 |        NullLogger | 247,647.43 μs |   311.212 μs |   291.108 μs | 247,716.62 μs | 221.66 |    3.41 |
|                       |        |                   |               |              |              |               |        |         |
|              **NoParams** | **100000** | **SerilogFileLogger** | **287,239.03 μs** |   **846.633 μs** |   **706.977 μs** | **287,175.21 μs** |   **1.00** |    **0.00** |
|          LogfNoParams | 100000 | SerilogFileLogger | 460,612.08 μs | 1,621.929 μs | 1,354.384 μs | 460,968.88 μs |   1.60 |    0.01 |
|              OneParam | 100000 | SerilogFileLogger | 330,668.93 μs | 1,007.196 μs |   841.054 μs | 330,571.75 μs |   1.15 |    0.00 |
|          LogfOneParam | 100000 | SerilogFileLogger | 599,113.13 μs | 2,474.888 μs | 2,066.644 μs | 598,748.42 μs |   2.09 |    0.01 |
|             TwoParams | 100000 | SerilogFileLogger | 358,458.56 μs | 1,793.355 μs | 1,497.533 μs | 358,022.62 μs |   1.25 |    0.01 |
|         LogfTwoParams | 100000 | SerilogFileLogger | 670,566.34 μs | 3,925.497 μs | 3,277.968 μs | 670,653.79 μs |   2.33 |    0.01 |
|             TenParams | 100000 | SerilogFileLogger | 358,236.93 μs | 1,307.517 μs | 1,091.836 μs | 358,599.29 μs |   1.25 |    0.00 |
|         LogfTenParams | 100000 | SerilogFileLogger | 650,437.64 μs | 1,927.200 μs | 1,504.631 μs | 650,587.13 μs |   2.26 |    0.01 |
| LogfTenParamsPercentA | 100000 | SerilogFileLogger | 653,951.45 μs | 4,064.348 μs | 3,393.915 μs | 655,406.71 μs |   2.28 |    0.02 |
