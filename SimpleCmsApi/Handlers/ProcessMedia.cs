using Azure.Data.Tables;
using Azure.Storage.Blobs;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Serilog;
using SimpleCmsApi.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace SimpleCmsApi.Handlers;

public record ProcessMediaCommand(HttpRequest Request) : IRequest;

public class ProcessMediaHandler : IRequestHandler<ProcessMediaCommand>
{
    private readonly IConfiguration _config;

    public ProcessMediaHandler(IConfiguration config)
    {
        _config = config;
    }

    public Task Handle(ProcessMediaCommand request, CancellationToken cancellationToken)
    {
        if (request.Request == null) throw new ArgumentNullException(nameof(request));
        string name = request.Request.Query["filename"][0] ?? string.Empty;
        string folder = request.Request.Query["folder"][0] ?? string.Empty;
        string description = request.Request.Query["description"][0] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(request));
        return ProcessMediaInternalAsync(name, folder, description, cancellationToken);
    }

    private async Task<Unit> ProcessMediaInternalAsync(string name, string folder, string description, CancellationToken cancellationToken)
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
        using var src = Image.Load(stream);

        var id = Guid.NewGuid();
        var hueCalc = new DominantHueColorCalculator(0.5f, 0.5f, 60);

        var small = await CreatePreviewImageAsync(src.Clone(x => x.AutoOrient()), 375, "preview-small", container, id);
        var medium = await CreatePreviewImageAsync(src.Clone(x => x.AutoOrient()), 768, "preview-medium", container, id);
        var large = await CreatePreviewImageAsync(src.Clone(x => x.AutoOrient()), 1080, "preview-large", container, id);
        var raw = await CreatePreviewImageAsync(src.Clone(x => x.AutoOrient()), null, "preview-raw", container, id);

        var galleryImage = new GalleryImage(folder, id.ToString())
        {
            DominantColour = "#" + hueCalc.CalculateDominantColor(src).ToHex(),
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
            RawPath = raw.Path,
            RawWidth = raw.Width,
            RawHeight = raw.Height
        };

        await table.UpsertEntityAsync(galleryImage, cancellationToken: cancellationToken);

        // delete original file
        await f.DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(true);

        return Unit.Value;
    }

    private static async Task<GalleryImageDetails> CreatePreviewImageAsync(
       Image image, int? res, string resolution, BlobContainerClient container, Guid id)
    {
        var width = image.Width;
        var height = image.Height;
        string name = string.Empty;
        if (res != null)
        {
            double ratio = (double)res / image.Height;
            width = (int)(image.Width * ratio);
            height = (int)(image.Height * ratio);
            if (ratio < 1.0)
            {
                image.Mutate(x => x.Resize(width, height));
            }
        }
        name = ("files/" + id.ToString() + "/" + resolution + "-" + id.ToString() + ".jpg").ToLowerInvariant();

        using var stream = new MemoryStream();
        image.SaveAsJpeg(stream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 90 });
        stream.Position = 0;
        await container.UploadBlobAsync(name, stream);

        return new GalleryImageDetails
        {
            Path = name,
            Width = width,
            Height = height
        };
    }
}