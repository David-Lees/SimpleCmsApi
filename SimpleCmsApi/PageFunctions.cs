using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace SimpleCmsApi
{
    public static class PageFunctions
    {
        [Function("UpdateSite")]
        public static async Task<HttpResponseData> UpdateSite(
            [HttpTrigger(AuthorizationLevel.Function, Route = null)] HttpRequestData req,
            ILogger log)
        {
            await UpdateSiteAsync(req, log);
            return req.CreateResponse(System.Net.HttpStatusCode.OK);
        }

        private static Task UpdateSiteAsync(HttpRequestData req, ILogger log)
        {
            ArgumentNullException.ThrowIfNull(req);
            ArgumentNullException.ThrowIfNull(log);
            return UpdateSiteInternalAsync(req, log);
        }

        private static async Task UpdateSiteInternalAsync(HttpRequestData req, ILogger log)
        {
            log.LogInformation("C# HTTP update site trigger function processed a request.");

            var container = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "images");
            var blob = container.GetBlobClient("site.json");
            await blob.UploadAsync(req.Body, true);
        }
    }
}