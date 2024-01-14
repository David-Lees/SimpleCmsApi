using Azure.Data.Tables;
using Azure.Storage.Blobs;
using MediatR;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using SimpleCmsApi.Models;
using SkiaSharp;

namespace SimpleCmsApi.Handlers;

public record SaveEditCommand(HttpRequestData Request) : IRequest;

public class SaveEditHandler(IConfiguration config) : IRequestHandler<SaveEditCommand>
{
    public Task Handle(SaveEditCommand request, CancellationToken cancellationToken)
    {
        if (request.Request == null) throw new ArgumentNullException(nameof(request));
        var queryString = System.Web.HttpUtility.ParseQueryString(request.Request.Url.Query);
        var rowKey = queryString["rowKey"] ?? string.Empty;
        var partitionKey = queryString["partitionKey"] ?? string.Empty;
        return SaveEditInternalAsync(rowKey, partitionKey, request.Request, cancellationToken);
    }

    private async Task SaveEditInternalAsync(string rowKey, string partitionKey, HttpRequestData request, CancellationToken cancellationToken)
    {
        var connectionString = config.GetValue<string>("AzureWebJobsStorage");
        var table = new TableClient(connectionString, "Images");
        await table.CreateIfNotExistsAsync(cancellationToken);

        var container = new BlobContainerClient(connectionString, "images");
        var image = await table.GetEntityAsync<GalleryImage>(partitionKey, rowKey, cancellationToken: cancellationToken);
        if (image == null || image.Value == null) throw new InvalidOperationException();
        var galleryImage = image.Value;

        if (string.IsNullOrEmpty(galleryImage.OriginalPath))
        {
            var f = container.GetBlobClient($"files/{rowKey}/preview-raw-{rowKey}.jpg".ToLowerInvariant());
            using var stream = new MemoryStream();
            await f.DownloadToAsync(stream, cancellationToken);
            stream.Position = 0;
            using var rawSrc = SKBitmap.Decode(stream);
            var original = $"files/{rowKey}/original.png".ToLowerInvariant();
            galleryImage.OriginalPath = original;
            using var stream2 = new MemoryStream();
            rawSrc.Encode(stream2, SKEncodedImageFormat.Jpeg, 90);
            stream2.Position = 0;
            await container.UploadBlobAsync(original, stream2, cancellationToken);
        }

        using var src = SKBitmap.Decode(request.Body);
        var hueCalc = new DominantHueColorCalculator(0.5f, 0.5f, 60);

        var small = await ProcessMediaHandler.CreatePreviewImageAsync(src, 375, "preview-small", container, rowKey, cancellationToken);
        var medium = await ProcessMediaHandler.CreatePreviewImageAsync(src, 768, "preview-medium", container, rowKey, cancellationToken);
        var large = await ProcessMediaHandler.CreatePreviewImageAsync(src, 1080, "preview-large", container, rowKey, cancellationToken);
        var raw = await ProcessMediaHandler.CreatePreviewImageAsync(src, null, "preview-raw", container, rowKey, cancellationToken);

        galleryImage.DominantColour = hueCalc.CalculateDominantColor(src).ToString();
        galleryImage.PreviewSmallPath = small.Path;
        galleryImage.PreviewSmallWidth = small.Width;
        galleryImage.PreviewSmallHeight = small.Height;
        galleryImage.PreviewMediumPath = medium.Path;
        galleryImage.PreviewMediumWidth = medium.Width;
        galleryImage.PreviewMediumHeight = medium.Height;
        galleryImage.PreviewLargePath = large.Path;
        galleryImage.PreviewLargeWidth = large.Width;
        galleryImage.PreviewLargeHeight = large.Height;
        galleryImage.RawPath = raw.Path;
        galleryImage.RawWidth = raw.Width;
        galleryImage.RawHeight = raw.Height;

        await table.UpsertEntityAsync(galleryImage, cancellationToken: cancellationToken);
    }
}