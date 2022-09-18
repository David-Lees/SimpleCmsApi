using Azure.Data.Tables;
using MediatR;
using Microsoft.Extensions.Configuration;
using SimpleCmsApi.Models;

namespace SimpleCmsApi.Handlers;

public record GetImagesQuery(string ParentId) : IRequest<List<GalleryImage>>;

public class GetImagesHandler : IRequestHandler<GetImagesQuery, List<GalleryImage>>
{
    private readonly IConfiguration _config;

    public GetImagesHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<List<GalleryImage>> Handle(GetImagesQuery request, CancellationToken cancellationToken)
    {
        var client = new TableClient(_config.GetValue<string>("AzureWebJobsBlobStorage"), "Images");
        var pages = client.QueryAsync<GalleryImage>(x => x.PartitionKey == request.ParentId, cancellationToken: cancellationToken).AsPages();
        var results = new List<GalleryImage>();
        await foreach (var page in pages)
        {
            results.AddRange(page.Values);
        }
        return results;
    }
}
