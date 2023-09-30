using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SimpleCmsApi;

public static class PageFunctions
{

    [Function("UpdateSite")]
    public static async Task<IActionResult> UpdateSite(
        [HttpTrigger(AuthorizationLevel.User, Route = null)] HttpRequest req,
        ILogger log)
    {
        await UpdateSiteAsync(req, log);
        return new OkResult();
    }

    private static Task UpdateSiteAsync(HttpRequest req, ILogger log)
    {
        if (req == null) throw new ArgumentNullException(nameof(req));
        if (log == null) throw new ArgumentNullException(nameof(log));
        return UpdateSiteInternalAsync(req, log);
    }

    private static async Task UpdateSiteInternalAsync(HttpRequest req, ILogger log)
    {
        log.LogInformation("C# HTTP update site trigger function processed a request.");

        var container = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "images");
        var blob = container.GetBlobClient("site.json");
        await blob.UploadAsync(req.Body, true);
    }
}
