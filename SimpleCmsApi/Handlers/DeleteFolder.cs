using Azure.Data.Tables;
using MediatR;
using Microsoft.Extensions.Configuration;
using SimpleCmsApi.Models;

namespace SimpleCmsApi.Handlers;

public record DeleteFolderCommand(GalleryFolder Folder) : IRequest;

public class DeleteFolderHandler : IRequestHandler<DeleteFolderCommand>
{
    private readonly IConfiguration _config;

    public DeleteFolderHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task Handle(DeleteFolderCommand request, CancellationToken cancellationToken)
    {
        var client = new TableClient(_config.GetValue<string>("AzureWebJobsBlobStorage"), "Folders");
        await client.CreateIfNotExistsAsync(cancellationToken);
        await client.DeleteEntityAsync(request.Folder.PartitionKey, request.Folder.RowKey, cancellationToken: cancellationToken);
    }
}
