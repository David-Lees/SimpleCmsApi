using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using SimpleCmsApi.Models;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Linq;

namespace SimpleCmsApi
{
    public static class HttpFunctions
    {
        private const string imagesJsonFilename = "images.json";

        [FunctionName("ProcessMedia")]
        public static async Task<IActionResult> ProcessMedia(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            log.LogInformation("C# HTTP process media trigger function processed a request.");
            string name = req.Query["filename"];
            if (string.IsNullOrWhiteSpace(name)) throw new NullReferenceException();

            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var blobClient = storageAccount.CreateCloudBlobClient();

            var srcContainer = blobClient.GetContainerReference("image-upload");
            var container = blobClient.GetContainerReference("images");

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            Gallery gallery = new Gallery();

            // load images.json
            var blob = container.GetBlockBlobReference(imagesJsonFilename);
            if (blob != null || (await blob.ExistsAsync()))
            {
                var body = await blob.DownloadTextAsync();
                if (!string.IsNullOrWhiteSpace(body))
                {
                    var imageList = JsonSerializer.Deserialize<List<GalleryImage>>(body, options);
                    gallery.Images.AddRange(imageList);
                }
            }

            // load file
            var f = srcContainer.GetBlockBlobReference(name);
            using MemoryStream stream = new MemoryStream();
            await f.DownloadToStreamAsync(stream).ConfigureAwait(true);
            stream.Position = 0;
            using var src = Image.Load(stream);

            var id = Guid.NewGuid();
            var hueCalc = new DominantHueColorCalculator(0.5f, 0.5f, 60);
            var galleryImage = new GalleryImage()
            {
                Id = id,
                DominantColour = "#" + hueCalc.CalculateDominantColor(src).ToHex(),
                Files = new Dictionary<string, GalleryImageDetails>()
            };

            var resolutions = GetResolutions();
            foreach (var key in resolutions.Keys)
            {
                galleryImage.Files[key] = await CreatePreviewImageAsync(
                    src.Clone(x => x.AutoOrient()),
                    key,
                    container,
                    id,
                    resolutions
                );
            }

            gallery.Images.Add(galleryImage);

            // save images.json
            var outputBody = JsonSerializer.Serialize(gallery.Images, options);
            await blob.UploadTextAsync(outputBody);
            log.LogInformation("Updated images.json");

            // delete original file
            await f.DeleteAsync().ConfigureAwait(true);

            return new OkObjectResult(gallery);
        }

        private static async Task<GalleryImageDetails> CreatePreviewImageAsync(
           Image image, string resolution, CloudBlobContainer container, Guid id, Dictionary<string, int?> resolutions)
        {
            var res = resolutions[resolution];
            var width = image.Width;
            var height = image.Height;
            string name = string.Empty;
            if (res != null)
            {                
                double ratio = (double)res / image.Height;
                width = (int)(image.Width * ratio);
                height = (int)(image.Height * ratio);
                if (ratio < 1.0)
                {
                    image.Mutate(x => x.Resize(width, height));
                    name = ("files/" + id.ToString() + "/" + resolution + "-" + id.ToString() + ".jpg").ToLowerInvariant();

                    var blob = container.GetBlockBlobReference(name);
                    blob.Properties.ContentType = "image/jpeg";
                    using var stream = new MemoryStream();
                    image.SaveAsJpeg(stream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 90 });
                    stream.Position = 0;
                    await blob.UploadFromStreamAsync(stream);
                }
                else
                {                   
                    name = ("files/" + id.ToString() + "/" + resolution + "-" + id.ToString() + ".png").ToLowerInvariant();
                    var blob = container.GetBlockBlobReference(name);
                    blob.Properties.ContentType = "image/jpeg";
                    using var stream = new MemoryStream();
                    image.SaveAsJpeg(stream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 90 });
                    stream.Position = 0;
                    await blob.UploadFromStreamAsync(stream).ConfigureAwait(true);
                }
            }
            return new GalleryImageDetails
            {
                Path = name,
                Width = width,
                Height = height
            };
        }

        private static Dictionary<string, int?> GetResolutions()
        {
            return new Dictionary<string, int?>
            {
                { "preview_small", 375},
                { "preview_sd",  768},
                { "preview_hd",  1080},
                { "raw",  null}
            };
        }

        [FunctionName("UpdateSite")]
        public static async Task<IActionResult> UpdateSite(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = null)] HttpRequest req,
            ILogger log)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            log.LogInformation("C# HTTP update site trigger function processed a request.");

            using var sr = new StreamReader(req.Body);
            string requestBody = await sr.ReadToEndAsync();

            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("images");
            var blob = container.GetBlockBlobReference("site.json");
            await blob.UploadTextAsync(requestBody).ConfigureAwait(true);
            return new OkResult();
        }

        [FunctionName("GetSasToken")]
        public static IActionResult GetSasToken(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            log.LogInformation("Call to get SAS Token");

            var permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write
                | SharedAccessBlobPermissions.Create | SharedAccessBlobPermissions.Add;

            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("image-upload");

            var adHocSas = new SharedAccessBlobPolicy()
            {
                Permissions = permissions,
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-30),
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(50)
            };
            var sasContainerToken = container.GetSharedAccessSignature(adHocSas);

            return new OkObjectResult(new { token = sasContainerToken });
        }

        [FunctionName("DeleteMedia")]
        public static async Task<IActionResult> DeleteMedia(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "DeleteMedia/{identifier}")] HttpRequest req,
            string identifier,
            ILogger log)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            log.LogInformation("C# HTTP delete media trigger function processed a request.");

            if (!Guid.TryParse(identifier, out Guid id)) throw new NullReferenceException();

            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("images");

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            Gallery gallery = new Gallery();


            // load images.json
            var blob = container.GetBlockBlobReference(imagesJsonFilename);
            if (blob != null || (await blob.ExistsAsync()))
            {
                var body = await blob.DownloadTextAsync();
                if (!string.IsNullOrWhiteSpace(body))
                {
                    var imageList = JsonSerializer.Deserialize<List<GalleryImage>>(body, options);
                    gallery.Images.AddRange(imageList);
                }
            }

            // find file
            var item = gallery.Images.SingleOrDefault(x => x.Id == id);
            if (item != null)
            {
                gallery.Images.Remove(item);
                var outputBody = JsonSerializer.Serialize(gallery.Images, options);
                await blob.UploadTextAsync(outputBody);
                log.LogInformation("Updated images.json");
                foreach (var key in item.Files.Keys)
                {
                    var path = item.Files[key].Path;
                    log.LogInformation("Deleted {deleted}", path);
                    var f = container.GetBlockBlobReference(path);
                    await f.DeleteIfExistsAsync();
                }
            }
            return new OkObjectResult(gallery);
        }

    }
}
