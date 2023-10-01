using Azure.Data.Tables;
using MediatR;
using Microsoft.Extensions.Configuration;
using SimpleCmsApi.Models;

namespace SimpleCmsApi.Handlers;

public record MoveImageCommand(string NewParent, GalleryImage Item) : IRequest;

public class MoveImageHandler : IRequestHandler<MoveImageCommand>
{
    private readonly IMediator _m;
    private readonly IConfiguration _config;

    public MoveImageHandler(IConfiguration config, IMediator m)
    {
        _config = config;
        _m = m;
    }

    public async Task Handle(MoveImageCommand request, CancellationToken cancellationToken)
    {
        await _m.Send(new DeleteImageCommand(request.Item.PartitionKey, request.Item.RowKey), cancellationToken);
        request.Item.PartitionKey = request.NewParent;

        var client = new TableClient(_config.GetValue<string>("AzureWebJobsBlobStorage"), "Images");
        await client.UpsertEntityAsync(request.Item, TableUpdateMode.Merge, cancellationToken);
    }
}