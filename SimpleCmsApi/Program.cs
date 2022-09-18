using Microsoft.Extensions.Hosting;
using Serilog;
using SimpleCmsApi.Models;
using MediatR;
using SimpleCmsApi;
using Microsoft.Extensions.DependencyInjection;

var config = StartupHelper.GetConfigurationBuilder().Build();
Log.Logger = StartupHelper.GetSerilogConfiguration(config).CreateBootstrapLogger();

Log.Information("Starting Simple CMS API");

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(config => StartupHelper.GetConfigurationBuilder(config))
    .ConfigureServices(services =>
    {
        services.AddMediatR(typeof(HttpFunctions).Assembly);

        var logger = StartupHelper.GetSerilogConfiguration(config);
        Log.Logger = logger.CreateLogger();
        services.AddLogging(lb => lb.AddSerilog(Log.Logger, true));
    })
    .UseSerilog()
    .Build();

try
{
    Log.Information("Running Simple CMS API Functions");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Error(ex, "Error running Simple CMS API Functions: {message}", ex.Message);
}
finally
{
    Log.Information("Shutting down Simple CMS API Functions");
    Log.CloseAndFlush();
}