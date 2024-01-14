using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Configuration;
using SimpleCmsApi.Models;

namespace SimpleCmsApi.Services;

public interface IBlobStorageService
{
    Task UploadChunkAsync(string containerName, string blobName, string blockId, Stream data);

    Task<(BlobContainerClient, BlockBlobClient)> FinaliseChunkedUploadAsync(string containerName, string blobName, List<string> blockIds, CancellationToken cancellationToken);

    Task<Stream> GetFileAsync(string containerName, string blobName);
}

public class BlobStorageService(IConfiguration config) : IBlobStorageService
{
    private readonly string _blobStorageConnectionString = config.GetValue<string>("AzureWebJobsStorage") ?? throw new InvalidOperationException();

    public async Task UploadChunkAsync(string containerName, string blobName, string blockId, Stream data)
    {
        var container = new BlobContainerClient(_blobStorageConnectionString, containerName.ToLowerInvariant());
        await container.CreateIfNotExistsAsync();
        var blob = container.GetBlockBlobClient(blobName);
        await blob.StageBlockAsync(blockId, data);
    }

    public async Task<(BlobContainerClient, BlockBlobClient)> FinaliseChunkedUploadAsync(string containerName, string blobName, List<string> blockIds, CancellationToken cancellationToken)
    {
        var container = new BlobContainerClient(_blobStorageConnectionString, containerName.ToLowerInvariant());
        var blob = container.GetBlockBlobClient(blobName);
        await blob.CommitBlockListAsync(blockIds, cancellationToken: cancellationToken);
        return (container, blob);
    }

    public async Task<Stream> GetFileAsync(string containerName, string blobName)
    {
        var container = new BlobContainerClient(_blobStorageConnectionString, containerName.ToLowerInvariant());
        if (!await container.ExistsAsync())
        {
            throw new NotFoundException();
        }

        var blob = container.GetBlockBlobClient(blobName);
        if (!await blob.ExistsAsync())
        {
            throw new NotFoundException();
        }

        return await blob.OpenReadAsync();
    }
}