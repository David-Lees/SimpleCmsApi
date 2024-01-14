using Azure.Data.Tables;
using MediatR;
using Microsoft.Extensions.Configuration;
using SimpleCmsApi.Models;

namespace SimpleCmsApi.Handlers;

public record MoveImageCommand(string NewParent, GalleryImage Item) : IRequest;

public class MoveImageHandler(IConfiguration config, IMediator m) : IRequestHandler<MoveImageCommand>
{
    public async Task Handle(MoveImageCommand request, CancellationToken cancellationToken)
    {
        await m.Send(new DeleteImageCommand(request.Item.PartitionKey, request.Item.RowKey), cancellationToken);
        request.Item.PartitionKey = request.NewParent;

        var client = new TableClient(config.GetValue<string>("AzureWebJobsStorage"), "Images");
        await client.UpsertEntityAsync(request.Item, TableUpdateMode.Merge, cancellationToken);
    }
}