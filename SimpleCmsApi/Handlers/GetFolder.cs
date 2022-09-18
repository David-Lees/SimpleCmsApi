using Azure.Data.Tables;
using MediatR;
using Microsoft.Extensions.Configuration;
using SimpleCmsApi.Models;

namespace SimpleCmsApi.Handlers;

public record GetFolderQuery(string ParentId, string Id) : IRequest<GalleryFolder>;

public class GetFolderHandler : IRequestHandler<GetFolderQuery, GalleryFolder>
{
    private readonly IConfiguration _config;

    public GetFolderHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<GalleryFolder> Handle(GetFolderQuery request, CancellationToken cancellationToken)
    {
        var client = new TableClient(_config.GetValue<string>("AzureWebJobsBlobStorage"), "Folders");
        var response = await client.GetEntityAsync<GalleryFolder>(request.ParentId, request.Id, cancellationToken: cancellationToken);
        return response.Value;
    }
}
