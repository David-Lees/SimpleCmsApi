using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SimpleCmsApi.Services
{
    public static class PageService
    {
        public static Task UpdateSiteAsync(HttpRequest req, ILogger log)
        {
            ArgumentNullException.ThrowIfNull(req);
            ArgumentNullException.ThrowIfNull(log);
            return UpdateSiteInternalAsync(req, log);
        }

        private static async Task UpdateSiteInternalAsync(HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP update site trigger function processed a request.");

            using var sr = new StreamReader(req.Body);
            string requestBody = await sr.ReadToEndAsync();

            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var blobServiceClient = new BlobServiceClient(connectionString);

            var container = blobServiceClient.GetBlobContainerClient("images");
            var blob = container.GetBlobClient("site.json");
            await blob.UploadAsync(requestBody).ConfigureAwait(true);
        }
    }
}