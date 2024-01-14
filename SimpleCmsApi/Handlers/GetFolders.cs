using Azure.Data.Tables;
using MediatR;
using Microsoft.Extensions.Configuration;
using SimpleCmsApi.Models;

namespace SimpleCmsApi.Handlers;

public class GetFoldersQuery : IRequest<List<GalleryFolder>>
{ }

public class GetFoldersHandler(IConfiguration config) : IRequestHandler<GetFoldersQuery, List<GalleryFolder>>
{
    public async Task<List<GalleryFolder>> Handle(GetFoldersQuery request, CancellationToken cancellationToken)
    {
        var client = new TableClient(config.GetValue<string>("AzureWebJobsStorage"), "Folders");
        var pages = client.QueryAsync<GalleryFolder>(cancellationToken: cancellationToken).AsPages();
        var results = new List<GalleryFolder>();
        await foreach (var page in pages)
        {
            results.AddRange(page.Values);
        }
        return results;
    }
}