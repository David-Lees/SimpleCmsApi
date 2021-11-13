using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCmsApi.Services
{
    public static class PageService
    {
        public static Task UpdateSiteAsync(HttpRequest req, ILogger log)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (log == null) throw new ArgumentNullException(nameof(log));
            return UpdateSiteInternalAsync(req, log);
        }

        private static async Task UpdateSiteInternalAsync(HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP update site trigger function processed a request.");

            using var sr = new StreamReader(req.Body);
            string requestBody = await sr.ReadToEndAsync();

            var storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("images");
            var blob = container.GetBlockBlobReference("site.json");
            await blob.UploadTextAsync(requestBody).ConfigureAwait(true);
        }
    }
}
