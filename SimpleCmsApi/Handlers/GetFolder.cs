using Azure.Data.Tables;
using MediatR;
using Microsoft.Extensions.Configuration;
using SimpleCmsApi.Models;

namespace SimpleCmsApi.Handlers;

public record GetFolderQuery(string ParentId, string Id) : IRequest<GalleryFolder>;

public class GetFolderHandler(IConfiguration config) : IRequestHandler<GetFolderQuery, GalleryFolder>
{
    public async Task<GalleryFolder> Handle(GetFolderQuery request, CancellationToken cancellationToken)
    {
        var client = new TableClient(config.GetValue<string>("AzureWebJobsStorage"), "Folders");
        var response = await client.GetEntityAsync<GalleryFolder>(request.ParentId, request.Id, cancellationToken: cancellationToken);
        return response.Value;
    }
}