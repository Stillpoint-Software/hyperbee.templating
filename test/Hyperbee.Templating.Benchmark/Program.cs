using BenchmarkDotNet.Running;
using static Hyperbee.Templating.Benchmark.BenchmarkConfig;

namespace Hyperbee.Templating.Benchmark;

public class Program
{
    public static void Main( string[] args )
    {
        BenchmarkSwitcher.FromAssembly( typeof( Program ).Assembly ).Run( args, new Config() );
    }
}


