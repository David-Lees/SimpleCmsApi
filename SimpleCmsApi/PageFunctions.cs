using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace SimpleCmsApi;

public class PageFunctions(ILogger<PageFunctions> log)
{
    [Function("UpdateSite")]
    public async Task<HttpResponseData> UpdateSite(
        [HttpTrigger(AuthorizationLevel.Function, Route = null)] HttpRequestData req)
    {
        await UpdateSiteAsync(req);
        return req.CreateResponse(System.Net.HttpStatusCode.OK);
    }

    private Task UpdateSiteAsync(HttpRequestData req)
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(log);
        return UpdateSiteInternalAsync(req);
    }

    private async Task UpdateSiteInternalAsync(HttpRequestData req)
    {
        log.LogInformation("C# HTTP update site trigger function processed a request.");

        var container = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "images");
        var blob = container.GetBlobClient("site.json");
        await blob.UploadAsync(req.Body, true);
    }
}