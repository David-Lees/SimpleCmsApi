using Azure.Data.Tables;
using Azure.Storage.Blobs;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using OpenCvSharp;
using OpenCvSharp.ML;
using Serilog;
using SimpleCmsApi.Models;

namespace SimpleCmsApi.Handlers;

public record ProcessMediaCommand(HttpRequest Request) : IRequest;

public class ProcessMediaHandler : IRequestHandler<ProcessMediaCommand>
{
    private readonly IConfiguration _config;
    private readonly IMediator _m;

    public ProcessMediaHandler(IConfiguration config, IMediator m)
    {
        _config = config;
        _m = m;
    }

    public Task Handle(ProcessMediaCommand request, CancellationToken cancellationToken)
    {
        if (request.Request == null) throw new ArgumentNullException(nameof(request));        
        var name = request.Request.Query["filename"].FirstOrDefault();
        var folder = request.Request.Query["folder"].FirstOrDefault();
        var description = request.Request.Query["description"].FirstOrDefault() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentNullException(nameof(request));
        return ProcessMediaInternalAsync(name, folder, description, cancellationToken);
    }

    private async Task ProcessMediaInternalAsync(string name, string folder, string description, CancellationToken cancellationToken)
    {
        Log.Information("C# HTTP process media trigger function processed a request.");
        var connectionString = _config.GetValue<string>("AzureWebJobsBlobStorage");

        var table = new TableClient(connectionString, "Images");
        await table.CreateIfNotExistsAsync(cancellationToken);

        var srcContainer = new BlobContainerClient(connectionString, "image-upload");
        var container = new BlobContainerClient(connectionString, "images");

        // load file
        var f = srcContainer.GetBlobClient(name);
        using var stream = new MemoryStream();
        await f.DownloadToAsync(stream, cancellationToken);
        stream.Position = 0;
        var src = stream.GetBuffer();

        var id = Guid.NewGuid();

        var hueCalc = await _m.Send(new CalculateDominantColourQuery(src), cancellationToken);

        var small = await CreatePreviewImageAsync(src, 375, "preview-small", container, id, ".jpg", cancellationToken);
        var medium = await CreatePreviewImageAsync(src, 768, "preview-medium", container, id, ".jpg", cancellationToken);
        var large = await CreatePreviewImageAsync(src, 1080, "preview-large", container, id, ".jpg", cancellationToken);
        var raw = await CreatePreviewImageAsync(src, null, "preview-raw", container, id, ".jpg", cancellationToken);
        var original = await CreatePreviewImageAsync(src, null, "original", container, id, ".png", cancellationToken);

        var galleryImage = new GalleryImage(folder, id.ToString())
        {
            DominantColour = "#" + Convert.ToHexString(new byte[] { hueCalc.Red, hueCalc.Green, hueCalc.Blue }),
            Description = description,
            PreviewSmallPath = small.Path,
            PreviewSmallWidth = small.Width,
            PreviewSmallHeight = small.Height,
            PreviewMediumPath = medium.Path,
            PreviewMediumWidth = medium.Width,
            PreviewMediumHeight = medium.Height,
            PreviewLargePath = large.Path,
            PreviewLargeWidth = large.Width,
            PreviewLargeHeight = large.Height,
            OriginalPath = original.Path,
            OriginalHeight = original.Height,
            OriginalWidth = original.Width,
            RawPath = raw.Path,
            RawWidth = raw.Width,
            RawHeight = raw.Height
        };

        await table.UpsertEntityAsync(galleryImage, cancellationToken: cancellationToken);

        // delete original file
        await f.DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(true);
    }

    private async Task<GalleryImageDetails> CreatePreviewImageAsync(
       byte[] image, int? res, string resolution, BlobContainerClient container, Guid id, string ext, CancellationToken cancellationToken)
    {
        var resizedImage = await _m.Send(new ResizeImageCommand(image, res), cancellationToken);

        var name = ("files/" + id.ToString() + "/" + resolution + "-" + id.ToString() + ext).ToLowerInvariant();

        var output = resizedImage.Image.ImEncode(ext);
        resizedImage.Image.Dispose();
        using var stream = new MemoryStream(output);
        stream.Position = 0;
   
        await container.UploadBlobAsync(name, stream, cancellationToken);

        return new GalleryImageDetails
        {
            Path = name,
            Width = resizedImage.Width,
            Height = resizedImage.Height
        };
    }
}

