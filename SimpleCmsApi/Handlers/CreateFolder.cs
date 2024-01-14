using Azure.Data.Tables;
using MediatR;
using Microsoft.Extensions.Configuration;
using SimpleCmsApi.Models;

namespace SimpleCmsApi.Handlers;

public record CreateFolderCommand(GalleryFolder Folder) : IRequest;

public class CreateFolderHandler(IConfiguration config) : IRequestHandler<CreateFolderCommand>
{
    public async Task Handle(CreateFolderCommand request, CancellationToken cancellationToken)
    {
        request.Folder.PartitionKey = request.Folder.PartitionKey.ToLowerInvariant();
        request.Folder.RowKey = request.Folder.RowKey.ToLowerInvariant();
        var client = new TableClient(config.GetValue<string>("AzureWebJobsStorage"), "Folders");
        await client.CreateIfNotExistsAsync(cancellationToken);
        await client.UpsertEntityAsync(request.Folder, TableUpdateMode.Merge, cancellationToken);
    }
}