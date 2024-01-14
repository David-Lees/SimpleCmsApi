using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace SimpleCmsApi;

public class HttpFunctions(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<HttpFunctions>();

    [Function("GetSasToken")]
    public async Task<HttpResponseData> GetSasToken([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req)
    {
        ArgumentNullException.ThrowIfNull(req);
        _logger.LogInformation("Call to get SAS Token");

        var container = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "image-upload");
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = container.Name,
            Resource = "c",
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
        };
        sasBuilder.SetPermissions(BlobContainerSasPermissions.Read | BlobContainerSasPermissions.Write | BlobContainerSasPermissions.Create | BlobContainerSasPermissions.Add);

        Uri sasUri = container.GenerateSasUri(sasBuilder);

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { token = sasUri });
        return response;
    }
}