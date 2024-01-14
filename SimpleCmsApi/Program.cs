using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Serilog;
using SimpleCmsApi;
using SimpleCmsApi.Models;
using SimpleCmsApi.Services;

var config = StartupHelper.GetConfigurationBuilder().Build();
Log.Logger = StartupHelper.GetSerilogConfiguration(config).CreateBootstrapLogger();

Log.Information("Starting Simple CMS API");

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureHostConfiguration(config =>
    {
        config.AddEnvironmentVariables();
        config.AddJsonFile("appsettings.json", true, true);
        config.AddJsonFile("local.settings.json", true, true);
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<FolderFunctions>());
        services.AddSingleton(x => hostContext.Configuration);
        services.AddTransient<IBlobStorageService, BlobStorageService>();

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = Microsoft.Identity.Web.Constants.Bearer;
            options.DefaultChallengeScheme = Microsoft.Identity.Web.Constants.Bearer;
        }).AddMicrosoftIdentityWebApi(hostContext.Configuration);

        var logger = StartupHelper.GetSerilogConfiguration(hostContext.Configuration);
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