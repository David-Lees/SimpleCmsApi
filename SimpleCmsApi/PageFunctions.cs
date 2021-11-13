using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using SimpleCmsApi.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Azure.Storage.Blobs;

namespace SimpleCmsApi
{
    public static class PageFunctions
    {

        [FunctionName("UpdateSite")]
        public static async Task<IActionResult> UpdateSite(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = null)] HttpRequest req,
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
}
