using Azure.Data.Tables;
using MediatR;
using Microsoft.Extensions.Configuration;
using SimpleCmsApi.Models;

namespace SimpleCmsApi.Handlers;

public record GetImagesQuery(string ParentId) : IRequest<List<GalleryImage>>;

public class GetImagesHandler(IConfiguration config) : IRequestHandler<GetImagesQuery, List<GalleryImage>>
{
    public async Task<List<GalleryImage>> Handle(GetImagesQuery request, CancellationToken cancellationToken)
    {
        var client = new TableClient(config.GetValue<string>("AzureWebJobsStorage"), "Images");
        var pages = client.QueryAsync<GalleryImage>(x => x.PartitionKey == request.ParentId, cancellationToken: cancellationToken).AsPages();
        var results = new List<GalleryImage>();
        await foreach (var page in pages)
        {
            results.AddRange(page.Values);
        }
        return results;
    }
}