using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using SimpleCmsApi.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SimpleCmsApi.Services
{
    public static class MediaProcessingService
    {
        public static Task ProcessMediaAsync(HttpRequest req, ILogger log)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (log == null) throw new ArgumentNullException(nameof(log));
            string name = req.Query["filename"];
            string folder = req.Query["folder"];
            string description = req.Query["description"];
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(req));
            return ProcessMediaInternalAsync(log, name, folder, description);
        }

        private static async Task ProcessMediaInternalAsync(ILogger log, string name, string folder, string description)
        {
            log.LogInformation("C# HTTP process media trigger function processed a request.");
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            var tableAccount = Microsoft.Azure.Cosmos.Table.CloudStorageAccount.Parse(connectionString);
            var client = tableAccount.CreateCloudTableClient();
            var table = client.GetTableReference("Images");
            await table.CreateIfNotExistsAsync();

            var srcContainer = new BlobContainerClient(connectionString, "image-upload");
            var container = new BlobContainerClient(connectionString, "images");

            // load file
            var f = srcContainer.GetBlobClient(name);
            using var stream = new MemoryStream();
            await f.DownloadToAsync(stream);
            stream.Position = 0;
            using var src = Image.Load(stream);

            var id = Guid.NewGuid();
            var hueCalc = new DominantHueColorCalculator(0.5f, 0.5f, 60);

            var small = await CreatePreviewImageAsync(src.Clone(x => x.AutoOrient()), 375, "preview-small", container, id);
            var medium = await CreatePreviewImageAsync(src.Clone(x => x.AutoOrient()), 768, "preview-medium", container, id);
            var large = await CreatePreviewImageAsync(src.Clone(x => x.AutoOrient()), 1080, "preview-large", container, id);
            var raw = await CreatePreviewImageAsync(src.Clone(x => x.AutoOrient()), null, "preview-raw", container, id);

            var galleryImage = new GalleryImage(folder, id.ToString())
            {
                DominantColour = "#" + hueCalc.CalculateDominantColor(src).ToHex(),
                Description = description,
                PreviewSmallPath = small.Path,
                PreviewSmallWidth = small.Width,
                PreviewSmallHeight = small.Height,
                PreviewMediumPath = medium.Path,
                PreviewMediumWidth = medium.Width,
                PreviewMediumHeight = medium.Height,
                PreviewLargePath = large.Path,
                PreviewLargeWidth = large.Width,
                PreviewLargeHeight = large.Height,
                RawPath = raw.Path,
                RawWidth = raw.Width,
                RawHeight = raw.Height
            };

            TableOperation insertOperation = TableOperation.Insert(galleryImage);
            table.Execute(insertOperation);

            // delete original file
            await f.DeleteAsync().ConfigureAwait(true);
        }


        private static async Task<GalleryImageDetails> CreatePreviewImageAsync(
           Image image, int? res, string resolution, BlobContainerClient container, Guid id)
        {
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
                }
            }
            name = ("files/" + id.ToString() + "/" + resolution + "-" + id.ToString() + ".jpg").ToLowerInvariant();

            using var stream = new MemoryStream();
            image.SaveAsJpeg(stream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 90 });
            stream.Position = 0;
            await container.UploadBlobAsync(name, stream);

            return new GalleryImageDetails
            {
                Path = name,
                Width = width,
                Height = height
            };
        }
    }
}

