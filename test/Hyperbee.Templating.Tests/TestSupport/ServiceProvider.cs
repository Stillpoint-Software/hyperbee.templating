using System;
using System.Collections.Generic;
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

    public string DoSomething() => "World" + Extra;
}

public static class ServiceProvider
{
    public static IServiceProvider GetServiceProvider()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices( ( _, services ) =>
            {
                services.AddSingleton<ITestService, TestService>();
                services.AddKeyedSingleton<ITestService>( "TestKey", ( _, _ ) => new TestService( " and Universe" ) );
            } )
            .ConfigureAppConfiguration( ( _, config ) =>
            {
                config.AddInMemoryCollection( new Dictionary<string, string>
                {
                    {"hello", "aliens"}
                } );
            } )
            .Build();

        return host.Services;
    }
}
