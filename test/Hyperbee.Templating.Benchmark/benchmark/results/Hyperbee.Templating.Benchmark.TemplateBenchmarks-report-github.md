```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3593/23H2/2023Update/SunValley3)
12th Gen Intel Core i9-12900HK, 1 CPU, 20 logical and 14 physical cores
.NET SDK 8.0.300
  [Host]   : .NET 8.0.6 (8.0.624.26715), X64 RyuJIT AVX2
  ShortRun : .NET 8.0.6 (8.0.624.26715), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | ParseMethod | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |------------ |------------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ParserSignal              | Buffered    |   172.05 ns | 154.55 ns |  8.471 ns |  1.00 |    0.00 | 0.2365 | 0.0010 |    2968 B |        1.00 |
| ParserMulti               | Buffered    |   185.07 ns | 180.90 ns |  9.916 ns |  1.08 |    0.06 | 0.2511 | 0.0026 |    3152 B |        1.06 |
| ParseTokenWithBufferWraps | Buffered    |   507.66 ns | 116.47 ns |  6.384 ns |  2.95 |    0.13 | 0.2975 | 0.0029 |    3736 B |        1.26 |
| NestedTokens              | Buffered    | 1,037.43 ns | 721.47 ns | 39.546 ns |  6.03 |    0.07 | 0.3300 | 0.0019 |    4144 B |        1.40 |
| InlineBlockExpression     | Buffered    | 1,941.39 ns | 467.36 ns | 25.617 ns | 11.31 |    0.69 | 0.3376 | 0.0038 |    4240 B |        1.43 |
|                           |             |             |           |           |       |         |        |        |           |             |
| ParserSignal              | InMemory    |    52.42 ns |  37.82 ns |  2.073 ns |  1.00 |    0.00 | 0.0337 |      - |     424 B |        1.00 |
| ParserMulti               | InMemory    |    58.13 ns |  21.16 ns |  1.160 ns |  1.11 |    0.02 | 0.0414 |      - |     520 B |        1.23 |
| ParseTokenWithBufferWraps | InMemory    |   505.17 ns | 222.02 ns | 12.170 ns |  9.64 |    0.27 | 0.2975 | 0.0029 |    3736 B |        8.81 |
| NestedTokens              | InMemory    |   940.07 ns | 116.89 ns |  6.407 ns | 17.95 |    0.60 | 0.1612 |      - |    2032 B |        4.79 |
| InlineBlockExpression     | InMemory    | 1,758.07 ns | 216.43 ns | 11.864 ns | 33.57 |    1.28 | 0.1678 |      - |    2128 B |        5.02 |
