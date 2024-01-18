using Azure.Data.Tables;
using Azure.Storage.Blobs;
using MediatR;
using Microsoft.Extensions.Configuration;
using Serilog;
using SimpleCmsApi.Models;
using SimpleCmsApi.Services;
using SkiaSharp;
using System.Text.RegularExpressions;

namespace SimpleCmsApi.Handlers;

public record ProcessMediaCommand(FileChunkListDto Chunks) : IRequest;

public partial class ProcessMediaHandler : IRequestHandler<ProcessMediaCommand>
{
    private readonly IConfiguration _config;
    private readonly IBlobStorageService _blobStorage;

    public ProcessMediaHandler(IConfiguration config, IBlobStorageService blobStorage)
    {
        _config = config;
        _blobStorage = blobStorage;
    }

    [GeneratedRegex("[^0-9A-Za-z.,]")]
    private static partial Regex FilenameRegex();

    public async Task Handle(ProcessMediaCommand request, CancellationToken cancellationToken)
    {
        Log.Information("C# HTTP process media trigger function processed a request.");
        if (request.Chunks == null) throw new ArgumentNullException(nameof(request));

        // Sanitise filename
        request.Chunks.Name = FilenameRegex().Replace(request.Chunks.Name, "-");
        string containerName = "images";
        var blobName = $"files/{request.Chunks.FileId}/original-{request.Chunks.FileId}{Path.GetExtension(request.Chunks.Name)}";
        var (container, blob) = await _blobStorage.FinaliseChunkedUploadAsync(containerName, blobName, request.Chunks.BlockIds, cancellationToken);

        var connectionString = _config.GetValue<string>("AzureWebJobsStorage");

        var table = new TableClient(connectionString, "Images");
        await table.CreateIfNotExistsAsync(cancellationToken);

        // load file
        var stream = blob.OpenRead(cancellationToken: cancellationToken);
        using var src = SKBitmap.Decode(stream);

        var id = Guid.NewGuid().ToString();
        var hueCalc = new DominantHueColorCalculator(0.5f, 0.5f, 60);

        var small = await CreatePreviewImageAsync(src, 375, "preview-small", container, id, cancellationToken);
        var medium = await CreatePreviewImageAsync(src, 768, "preview-medium", container, id, cancellationToken);
        var large = await CreatePreviewImageAsync(src, 1080, "preview-large", container, id, cancellationToken);
        var raw = await CreatePreviewImageAsync(src, null, "preview-raw", container, id, cancellationToken);

        var galleryImage = new GalleryImage(request.Chunks.ParentId.ToString(), id.ToString())
        {
            DominantColour = hueCalc.CalculateDominantColor(src).ToString(),
            Description = request.Chunks.Description,
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
            RawHeight = raw.Height,
            OriginalPath = blobName
        };

        await table.UpsertEntityAsync(galleryImage, cancellationToken: cancellationToken);
    }

    public static async Task<GalleryImageDetails> CreatePreviewImageAsync(
       SKBitmap image, int? res, string resolution, BlobContainerClient container, string path, CancellationToken cancellationToken)
    {
        var width = image.Width;
        var height = image.Height;
        SKBitmap copy;
        if (res != null)
        {
            double ratio = (double)res / image.Height;
            width = (int)(image.Width * ratio);
            height = (int)(image.Height * ratio);
            if (ratio < 1.0)
            {
                copy = new SKBitmap(width, height);
                image.ScalePixels(copy, SKFilterQuality.High);
            }
            else
            {
                copy = image.Copy();
            }
        }
        else
        {
            copy = image.Copy();
        }

        var name = $"files/{path}/{resolution}.jpg".ToLowerInvariant();

        using var stream = new MemoryStream();
        copy.Encode(stream, SKEncodedImageFormat.Jpeg, 90);
        stream.Position = 0;

        var blob = container.GetBlobClient(name);
        if (await blob.ExistsAsync(cancellationToken))
        {
            await blob.UploadAsync(stream, true, cancellationToken);
        }
        else
        {
            await container.UploadBlobAsync(name, stream, cancellationToken);
        }

        return new GalleryImageDetails
        {
            Path = name,
            Width = width,
            Height = height
        };
    }
}