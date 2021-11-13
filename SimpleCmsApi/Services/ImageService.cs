using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using SimpleCmsApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace SimpleCmsApi.Services
{
    public class ImageService
    {
        private readonly CloudTable _images;

        public readonly static ImageService Instance = new();

        private readonly string _connectionString;

        public ImageService()
        {
            _connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var tableAccount = CloudStorageAccount.Parse(_connectionString);
            var client = tableAccount.CreateCloudTableClient();
            _images = client.GetTableReference("Images");
        }
        public async Task<GalleryImage> GetImage(string parentId, string id)
        {
            var operation = TableOperation.Retrieve<GalleryImage>(parentId, id);
            return await ExecuteTableOperation(operation) as GalleryImage;
        }

        public List<GalleryImage> GetImages(string parentId)
        {
            return _images.CreateQuery<GalleryImage>()
                .Where(x => x.PartitionKey == parentId)
                .ToList();
        }

        public async Task DeleteImage(string parentId, string id, ILogger log)
        {
            var image = await GetImage(parentId, id);
            if (image == null) throw new NullReferenceException();

            log.LogInformation("Deleting image " + JsonSerializer.Serialize(image));
            await ExecuteTableOperation(TableOperation.Delete(image));

            var service = new BlobServiceClient(_connectionString);
            var container = service.GetBlobContainerClient("images");

            var small = container.GetBlobClient(image.PreviewSmallPath);
            var meduium = container.GetBlobClient(image.PreviewMediumPath);
            var large = container.GetBlobClient(image.PreviewLargePath);
            var raw = container.GetBlobClient(image.RawPath);

            BlobBatchClient batch = service.GetBlobBatchClient();
            var uris = new Uri[] { small.Uri, meduium.Uri, large.Uri, raw.Uri };
            log.LogInformation(JsonSerializer.Serialize(uris));
            await batch.DeleteBlobsAsync(uris);
        }

        public async Task MoveImage(string oldParent, string newParent, string id)
        {
            var image = await GetImage(oldParent, id);
            if (image != null)
            {
                await ExecuteTableOperation(TableOperation.Delete(image));
                image.PartitionKey = newParent;
                await ExecuteTableOperation(TableOperation.InsertOrReplace(image));
            }
        }

        private async Task<object> ExecuteTableOperation(TableOperation tableOperation)
        {
            await _images.CreateIfNotExistsAsync();
            var tableResult = await _images.ExecuteAsync(tableOperation);
            return tableResult.Result;
        }
    }
}
