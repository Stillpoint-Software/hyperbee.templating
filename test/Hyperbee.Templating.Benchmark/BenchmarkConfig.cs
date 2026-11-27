using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Validators;

namespace Hyperbee.Templating.Benchmark;

public class BenchmarkConfig
{
    public class Config : ManualConfig
    {
        public Config()
        {
            // net8.0 and net9.0 are no longer built, and BenchmarkDotNet 0.15.8 has no
            // .NET 11 moniker, so benchmarks run on .NET 10 alone.
            AddJob( Job.ShortRun
                .WithRuntime( CoreRuntime.Core10_0 )
                .WithId( ".NET 10" ) );

            AddExporter( MarkdownExporter.GitHub );
            AddValidator( JitOptimizationsValidator.DontFailOnError );
            AddLogger( ConsoleLogger.Default );
            AddColumnProvider(
                DefaultColumnProviders.Job,
                DefaultColumnProviders.Params,
                DefaultColumnProviders.Descriptor,
                DefaultColumnProviders.Metrics,
                DefaultColumnProviders.Statistics
            );

            AddDiagnoser( MemoryDiagnoser.Default );

            Orderer = new DefaultOrderer( SummaryOrderPolicy.FastestToSlowest );
            ArtifactsPath = "benchmark";
        }
    }
}
