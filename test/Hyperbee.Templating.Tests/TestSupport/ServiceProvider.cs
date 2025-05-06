using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hyperbee.Templating.Tests.TestSupport;

public interface ITestService
{
    string DoSomething();
}

public class TestService : ITestService
{
    public TestService() { }
    public TestService( string extra ) => Extra = extra;
    public string Extra { get; set; }

    public string DoSomething() => "Hello, World!" + Extra;
}

public static class ServiceProvider
{
    public static IServiceProvider GetServiceProvider()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices( ( _, services ) =>
            {
                services.AddSingleton<ITestService, TestService>();
                services.AddKeyedSingleton<ITestService>( "TestKey", ( _, _ ) => new TestService( " And Universe!" ) );
            } )
            .ConfigureAppConfiguration( ( _, config ) =>
            {
                config.AddInMemoryCollection( new Dictionary<string, string>
                {
                    {"hello", "Hello, World!"},
                    {"number", "10"},
                    {"connections:sql:secure", "true"},
                } );
            } )
            .Build();

        return host.Services;
    }
}
