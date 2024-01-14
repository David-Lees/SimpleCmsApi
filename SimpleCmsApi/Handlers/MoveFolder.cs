using MediatR;
using SimpleCmsApi.Models;

namespace SimpleCmsApi.Handlers;

public record MoveFolderCommand(string NewParent, GalleryFolder Item) : IRequest;

public class MoveFolderHandler(IMediator m) : IRequestHandler<MoveFolderCommand>
{
    public async Task Handle(MoveFolderCommand request, CancellationToken cancellationToken)
    {
        if (request.Item.RowKey != Guid.Empty.ToString())
        {
            await m.Send(new DeleteFolderCommand(request.Item), cancellationToken);
            request.Item.PartitionKey = request.NewParent;
            await m.Send(new CreateFolderCommand(request.Item), cancellationToken);
        }
    }
}