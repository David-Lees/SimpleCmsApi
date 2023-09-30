using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace SimpleCmsApi.Models;

public static class StartupHelper
{
    public static IConfigurationBuilder GetConfigurationBuilder(IConfigurationBuilder? config = null)
    {
        config ??= new ConfigurationBuilder();
        config
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true)
            .AddJsonFile("local.settings.json", true)
            .AddEnvironmentVariables();

        return config;
    }

    public static LoggerConfiguration GetSerilogConfiguration(IConfigurationRoot config, LoggerConfiguration? logger = null)
    {
        logger ??= new LoggerConfiguration();
        logger
           .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
           .Enrich.FromLogContext()
        .WriteTo.Console();
        
        var connectionString = config.GetValue<string>("AzureWebJobsBlobStorage");
        if (!string.IsNullOrEmpty(connectionString))
        {            
            var cloudAccount = new BlobServiceClient(connectionString);
            logger.WriteTo.AzureBlobStorage(cloudAccount, storageFileName: "{yyyy}-{MM}/Log-{dd}.txt");
        }

        return logger;
    }
}
