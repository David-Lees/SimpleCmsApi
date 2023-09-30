using Azure.Data.Tables;
using MediatR;
using Microsoft.Extensions.Configuration;
using SimpleCmsApi.Models;

namespace SimpleCmsApi.Handlers;

public class GetFoldersQuery : IRequest<List<GalleryFolder>> { }

public class GetFoldersHandler : IRequestHandler<GetFoldersQuery, List<GalleryFolder>>
{
    private readonly IConfiguration _config;

    public GetFoldersHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<List<GalleryFolder>> Handle(GetFoldersQuery request, CancellationToken cancellationToken)
    {
        var client = new TableClient(_config.GetValue<string>("AzureWebJobsBlobStorage"), "Folders");
        var pages = client.QueryAsync<GalleryFolder>(cancellationToken: cancellationToken).AsPages();
        var results = new List<GalleryFolder>();
        await foreach (var page in pages)
        {
            results.AddRange(page.Values);
        }
        return results;
    }
}
