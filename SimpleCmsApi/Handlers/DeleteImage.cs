using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using MediatR;
using Microsoft.Extensions.Configuration;
using Serilog;
using SimpleCmsApi.Models;
using System.Text.Json;

namespace SimpleCmsApi.Handlers;

public record DeleteImageCommand(string ParentId, string Id) : IRequest;

public class DeleteImageHandler : IRequestHandler<DeleteImageCommand>
{
    private readonly IConfiguration _config;

    public DeleteImageHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task Handle(DeleteImageCommand request, CancellationToken cancellationToken)
    {
        var connectionString = _config.GetValue<string>("AzureWebJobsBlobStorage");
        var client = new TableClient(connectionString, "Images");
        var image = await client.GetEntityAsync<GalleryImage>(request.ParentId, request.Id, cancellationToken: cancellationToken);
        await client.DeleteEntityAsync(request.ParentId, request.Id, cancellationToken: cancellationToken);

        var service = new BlobServiceClient(connectionString);
        var container = service.GetBlobContainerClient("images");

        var small = container.GetBlobClient(image.Value.PreviewSmallPath);
        var meduium = container.GetBlobClient(image.Value.PreviewMediumPath);
        var large = container.GetBlobClient(image.Value.PreviewLargePath);
        var raw = container.GetBlobClient(image.Value.RawPath);

        BlobBatchClient batch = service.GetBlobBatchClient();
        var uris = new Uri[] { small.Uri, meduium.Uri, large.Uri, raw.Uri };
        Log.Information(JsonSerializer.Serialize(uris));
        await batch.DeleteBlobsAsync(uris, cancellationToken: cancellationToken);
    }
}