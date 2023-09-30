using Azure.Data.Tables;
using MediatR;
using Microsoft.Extensions.Configuration;
using SimpleCmsApi.Models;

namespace SimpleCmsApi.Handlers;

public record CreateFolderCommand(GalleryFolder Folder) : IRequest;

public class CreateFolderHandler : IRequestHandler<CreateFolderCommand>
{
    private readonly IConfiguration _config;

    public CreateFolderHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task Handle(CreateFolderCommand request, CancellationToken cancellationToken)
    {
        var client = new TableClient(_config.GetValue<string>("AzureWebJobsBlobStorage"), "Folders");
        await client.CreateIfNotExistsAsync(cancellationToken);
        await client.UpsertEntityAsync(request.Folder, TableUpdateMode.Merge, cancellationToken);
    }
}
