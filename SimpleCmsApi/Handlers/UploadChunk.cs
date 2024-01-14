using MediatR;
using Microsoft.Azure.Functions.Worker.Http;
using SimpleCmsApi.Models;
using SimpleCmsApi.Services;
using System.Text.RegularExpressions;

namespace SimpleCmsApi.Handlers;

public record UploadChunkCommand(Guid Id, FileChunkDto Chunk, HttpRequestData RequestData) : IRequest<HttpResponseData>;

public partial class UploadChunkHandler : IRequestHandler<UploadChunkCommand, HttpResponseData>
{
    private readonly IBlobStorageService _blobStorage;

    public UploadChunkHandler(IBlobStorageService blobStorageService)
    {
        _blobStorage = blobStorageService;
    }

    public async Task<HttpResponseData> Handle(UploadChunkCommand request, CancellationToken cancellationToken)
    {
        // We need to have a unique address in order to upload in parallel as browsers
        // won't allow more than one upload to the same address at the same time.
        // So we include the block id in the address in addition to the dto.
        if (request.Id != request.Chunk.FileId) return request.RequestData.CreateResponse(System.Net.HttpStatusCode.BadRequest);

        // Sanitise filename
        request.Chunk.Name = FilenameRegex().Replace(request.Chunk.Name, "-");

        // Derive container name and BLOB name from DTO, then upload chunk.
        string containerName = "images";
        var blobName = $"files/{request.Id}/original-{request.Id}{Path.GetExtension(request.Chunk.Name)}";
        await _blobStorage.UploadChunkAsync(containerName, blobName, request.Chunk.BlockId, request.Chunk.Data);
        return request.RequestData.CreateResponse(System.Net.HttpStatusCode.OK);
    }

    [GeneratedRegex("[^0-9A-Za-z.,]")]
    private static partial Regex FilenameRegex();
}