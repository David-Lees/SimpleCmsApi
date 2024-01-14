using Azure.Data.Tables;
using MediatR;
using Microsoft.Extensions.Configuration;
using SimpleCmsApi.Models;

namespace SimpleCmsApi.Handlers;

public record DeleteFolderCommand(GalleryFolder Folder) : IRequest;

public class DeleteFolderHandler(IConfiguration config) : IRequestHandler<DeleteFolderCommand>
{
    public async Task Handle(DeleteFolderCommand request, CancellationToken cancellationToken)
    {
        var client = new TableClient(config.GetValue<string>("AzureWebJobsStorage"), "Folders");
        await client.CreateIfNotExistsAsync(cancellationToken);
        await client.DeleteEntityAsync(request.Folder.PartitionKey.ToUpperInvariant(), request.Folder.RowKey.ToUpperInvariant(), cancellationToken: cancellationToken);
        await client.DeleteEntityAsync(request.Folder.PartitionKey.ToUpperInvariant(), request.Folder.RowKey.ToLowerInvariant(), cancellationToken: cancellationToken);
        await client.DeleteEntityAsync(request.Folder.PartitionKey.ToLowerInvariant(), request.Folder.RowKey.ToUpperInvariant(), cancellationToken: cancellationToken);
        await client.DeleteEntityAsync(request.Folder.PartitionKey.ToLowerInvariant(), request.Folder.RowKey.ToLowerInvariant(), cancellationToken: cancellationToken);
    }
}