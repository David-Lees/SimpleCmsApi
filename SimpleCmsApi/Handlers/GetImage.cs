using Azure.Data.Tables;
using MediatR;
using Microsoft.Extensions.Configuration;
using SimpleCmsApi.Models;

namespace SimpleCmsApi.Handlers;

public record GetImageQuery(string ParentId, string Id) : IRequest<GalleryImage>;

public class GetImageHandler(IConfiguration config) : IRequestHandler<GetImageQuery, GalleryImage>
{
    public async Task<GalleryImage> Handle(GetImageQuery request, CancellationToken cancellationToken)
    {
        var client = new TableClient(config.GetValue<string>("AzureWebJobsStorage"), "Images");
        var response = await client.GetEntityAsync<GalleryImage>(request.ParentId, request.Id, cancellationToken: cancellationToken);
        return response.Value;
    }
}