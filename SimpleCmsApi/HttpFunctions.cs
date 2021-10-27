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

namespace SimpleCmsApi
{
    public static class HttpFunctions
    {
        private const string imagesJsonFilename = "images.json";

        [FunctionName("GetImages")]
        public static async Task<IActionResult> GetImages(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            return new OkObjectResult(await GetImagesAsync());
        }

        private static async Task<List<GalleryImage>> GetImagesAsync()
        {
            var storageAccount = Microsoft.Azure.Cosmos.Table.CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var client = storageAccount.CreateCloudTableClient();
            var table = client.GetTableReference("Images");
            await table.CreateIfNotExistsAsync();

            return table.ExecuteQuery(new TableQuery<GalleryImage>()).ToList();
        }

        [FunctionName("ProcessMedia")]
        public static async Task<IActionResult> ProcessMedia(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            var folder = await ProcessMediaAsync(req, log);
            return new OkObjectResult(folder);
        }

        private static Task<GalleryFolder> ProcessMediaAsync(HttpRequest req, ILogger log)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (log == null) throw new ArgumentNullException(nameof(log));
            string name = req.Query["filename"];
            string folder = req.Query["folder"];
            string description = req.Query["description"];
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(req));
            return ProcessMediaInternalAsync(log, name, folder, description);
        }

        private static async Task<GalleryFolder> ProcessMediaInternalAsync(ILogger log, string name, string folder, string description)
        {
            log.LogInformation("C# HTTP process media trigger function processed a request.");

            var tableAccount = Microsoft.Azure.Cosmos.Table.CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var client = tableAccount.CreateCloudTableClient();
            var table = client.GetTableReference("Images");
            await table.CreateIfNotExistsAsync();

            var storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var blobClient = storageAccount.CreateCloudBlobClient();

            var srcContainer = blobClient.GetContainerReference("image-upload");
            var container = blobClient.GetContainerReference("images");

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            GalleryFolder root = new GalleryFolder() { Id = Guid.Empty.ToString() };

            // load images.json
            //var blob = container.GetBlockBlobReference(imagesJsonFilename);
            //if (blob != null)
            //{
            //if (await blob.ExistsAsync())
            //{
            //    var body = await blob.DownloadTextAsync();
            //    if (!string.IsNullOrWhiteSpace(body))
            //    {
            //        root = JsonSerializer.Deserialize<GalleryFolder>(body, options);
            //    }
            //}

            // load file
            var f = srcContainer.GetBlockBlobReference(name);
            using MemoryStream stream = new MemoryStream();
            await f.DownloadToStreamAsync(stream).ConfigureAwait(true);
            stream.Position = 0;
            using var src = Image.Load(stream);

            var id = Guid.NewGuid();
            var hueCalc = new DominantHueColorCalculator(0.5f, 0.5f, 60);
            var galleryImage = new GalleryImage(folder, id.ToString())
            {
                DominantColour = "#" + hueCalc.CalculateDominantColor(src).ToHex(),
                Files = new Dictionary<string, GalleryImageDetails>(),
                Description = description
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

            //if (!string.IsNullOrWhiteSpace(folder))
            //{
            //    (GetFolderRecursive(root, folder) ?? root)
            //        .Images.Add(galleryImage);
            //}
            //else
            //{
            //    root.Images.Add(galleryImage);
            //}

            TableOperation insertOperation = TableOperation.Insert(galleryImage);
            table.Execute(insertOperation);

            // save images.json
            //var outputBody = JsonSerializer.Serialize(root, options);
            //await blob.UploadTextAsync(outputBody);
            //log.LogInformation("Updated images.json");

            // delete original file
            await f.DeleteAsync().ConfigureAwait(true);
            //}
            return root;
        }

        private static GalleryFolder GetFolderRecursive(GalleryFolder root, string id)
        {
            if (root.Id == id) return root;
            foreach (var f in root.Folders)
            {
                var r = GetFolderRecursive(f, id);
                if (r != null) return r;
            }
            return null;
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

            using var sr = new StreamReader(req.Body);
            string requestBody = await sr.ReadToEndAsync();

            var storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("images");
            var blob = container.GetBlockBlobReference("site.json");
            await blob.UploadTextAsync(requestBody).ConfigureAwait(true);
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

            var storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
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
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "DeleteMedia/{folder}/{identifier}")] HttpRequest req,
            string folder,
            string identifier,
            ILogger log)
        {
            await DeleteMediaAsync(req, folder, identifier, log);
            return new OkObjectResult(await GetImagesAsync());
        }

        private static Task DeleteMediaAsync(HttpRequest req,
            string folder,
            string identifier,
            ILogger log)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            if (!Guid.TryParse(identifier, out _)) throw new ArgumentNullException(nameof(identifier));
            if (!Guid.TryParse(folder, out _)) throw new ArgumentNullException(nameof(folder));
            return DeleteMediaInternalAsync(folder, identifier, log);
        }

        private static async Task DeleteMediaInternalAsync(string folder, string id, ILogger log)
        {
            log.LogInformation("C# HTTP delete media trigger function processed a request.");
            var storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("images");

            //JsonSerializerOptions options = new JsonSerializerOptions
            //{
            //    PropertyNameCaseInsensitive = true,
            //    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            //};

            //GalleryFolder root = new GalleryFolder();
            // load images.json
            //var blob = container.GetBlockBlobReference(imagesJsonFilename);
            //if (blob != null)
            //{
            //    if (await blob.ExistsAsync())
            //    {
            //        var body = await blob.DownloadTextAsync();
            //        if (!string.IsNullOrWhiteSpace(body))
            //        {
            //            root = JsonSerializer.Deserialize<GalleryFolder>(body, options);
            //        }
            //    }

            // find folder
            //var folder = FindImageFolder(root, id);
            if (folder != null)
            {

                var tableAccount = Microsoft.Azure.Cosmos.Table.CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                var client = tableAccount.CreateCloudTableClient();
                var table = client.GetTableReference("Images");
                await table.CreateIfNotExistsAsync();

                var condition1 = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, folder);
                var condition2 = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, id);
                var condition = TableQuery.CombineFilters(condition1, TableOperators.And, condition2);
                var query = new TableQuery<GalleryImage>().Where(condition);
                var item = query.FirstOrDefault();
                //var item = folder.Images.SingleOrDefault(x => x.Id == id);
                if (item != null)
                {
                    //folder.Images.Remove(item);
                    //var outputBody = JsonSerializer.Serialize(root, options);
                    //await blob.UploadTextAsync(outputBody);
                    //log.LogInformation("Updated images.json");

                    table.Execute(TableOperation.Delete(item));

                    foreach (var key in item.Files.Keys)
                    {
                        var path = item.Files[key].Path;
                        log.LogInformation("Deleted {deleted}", path);
                        var f = container.GetBlockBlobReference(path);
                        await f.DeleteIfExistsAsync();
                    }
                }
            }
        }

        //private static GalleryFolder FindImageFolder(GalleryFolder root, Guid id)
        //{
        //    if (root.Images.Any(x => x.Id == id)) return root;
        //    foreach (var f in root.Folders)
        //    {
        //        var r = FindImageFolder(f, id);
        //        if (r != null) return r;
        //    }
        //    return null;
        //}

        [FunctionName("UpdateMedia")]
        public static async Task<IActionResult> UpdateMedia(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "UpdateMedia")] HttpRequest req,
            ILogger log)
        {
            await UpdateMediaAsync(req, log);
            return new OkResult();
        }

        private static Task UpdateMediaAsync(HttpRequest req, ILogger log)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (log == null) throw new ArgumentNullException(nameof(log));
            return UpdateMediaInternalAsync(req, log);
        }

        private static async Task UpdateMediaInternalAsync(HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP update media trigger function processed a request.");

            using var sr = new StreamReader(req.Body);
            string requestBody = await sr.ReadToEndAsync();

            var storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("images");
            var blob = container.GetBlockBlobReference("images.json");
            await blob.UploadTextAsync(requestBody).ConfigureAwait(true);
        }
    }
}