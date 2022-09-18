using Azure.Data.Tables;
using MediatR;
using Microsoft.Extensions.Configuration;
using SimpleCmsApi.Models;

namespace SimpleCmsApi.Handlers;

public record GetImageQuery(string ParentId, string Id) : IRequest<GalleryImage>;

public class GetImageHandler : IRequestHandler<GetImageQuery, GalleryImage>
{
    private readonly IConfiguration _config;

    public GetImageHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<GalleryImage> Handle(GetImageQuery request, CancellationToken cancellationToken)
    {
        var client = new TableClient(_config.GetValue<string>("AzureWebJobsBlobStorage"), "Images");
        var response = await client.GetEntityAsync<GalleryImage>(request.ParentId, request.Id, cancellationToken: cancellationToken);
        return response.Value;
    }
}
