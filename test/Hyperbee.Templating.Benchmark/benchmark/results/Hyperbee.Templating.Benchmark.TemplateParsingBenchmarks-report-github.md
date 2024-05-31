```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3593/23H2/2023Update/SunValley3)
12th Gen Intel Core i9-12900HK, 1 CPU, 20 logical and 14 physical cores
.NET SDK 8.0.300
  [Host]   : .NET 8.0.6 (8.0.624.26715), X64 RyuJIT AVX2
  ShortRun : .NET 8.0.6 (8.0.624.26715), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean     | Error    | StdDev  | Gen0   | Gen1   | Allocated |
|-------------------------- |---------:|---------:|--------:|-------:|-------:|----------:|
| ParseTokenWithBufferWraps | 505.5 ns | 151.6 ns | 8.31 ns | 0.2975 | 0.0029 |   3.65 KB |
