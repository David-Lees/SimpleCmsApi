using MediatR;
using SimpleCmsApi.Models;

namespace SimpleCmsApi.Handlers;

public record MoveFolderCommand(string NewParent, GalleryFolder Item) : IRequest;

public class MoveFolderHandler : IRequestHandler<MoveFolderCommand>
{
    private readonly IMediator _m;

    public MoveFolderHandler(IMediator m)
    {
        _m = m;
    }

    public async Task Handle(MoveFolderCommand request, CancellationToken cancellationToken)
    {
        if (request.Item.RowKey != Guid.Empty.ToString())
        {
            await _m.Send(new DeleteFolderCommand(request.Item), cancellationToken);
            request.Item.PartitionKey = request.NewParent;
            await _m.Send(new CreateFolderCommand(request.Item), cancellationToken);
        }
    }
}