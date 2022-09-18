using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace SimpleCmsApi;

public static class HttpFunctions
{
    [FunctionName("GetSasToken")]
    public static IActionResult GetSasToken(
        [HttpTrigger(AuthorizationLevel.User, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        if (req == null) throw new ArgumentNullException(nameof(req));
        log.LogInformation("Call to get SAS Token");

        var container = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "image-upload");
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = container.Name,
            Resource = "c",
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
        };
        sasBuilder.SetPermissions(BlobContainerSasPermissions.Read | BlobContainerSasPermissions.Write | BlobContainerSasPermissions.Create | BlobContainerSasPermissions.Add);

        Uri sasUri = container.GenerateSasUri(sasBuilder);
        
        return new OkObjectResult(new { token = sasUri });
    }
}