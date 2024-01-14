using HttpMultipartParser;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Serilog;
using SimpleCmsApi.Handlers;
using SimpleCmsApi.Models;
using System.Text.Json;

namespace SimpleCmsApi;

public class ImageFunctions(IMediator m)
{
    [Function("Chunk")]
    public async Task<HttpResponseData> UploadChunk(
       [HttpTrigger(AuthorizationLevel.Function, "post", Route = "chunk/{id}")] HttpRequestData req, Guid id)
    {
        var parser = await MultipartFormDataParser.ParseAsync(req.Body).ConfigureAwait(false);
        var file = parser.Files[0];

        var chunk = new FileChunkDto
        {
            BlockId = parser.GetParameterValue("blockId"),
            FileId = Guid.Parse(parser.GetParameterValue("fileId")),
            Name = parser.GetParameterValue("name"),
            ParentId = Guid.Parse(parser.GetParameterValue("parentId")),
            Data = file.Data
        };

        return await m.Send(new UploadChunkCommand(id, chunk, req));
    }

    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Function("ProcessUpload")]
    public async Task<HttpResponseData> ProcessUpload(
       [HttpTrigger(AuthorizationLevel.Function, "post", Route = "finalise/{id}")] HttpRequestData req,
        Guid id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(req);
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync(cancellationToken);

        var chunks = JsonSerializer.Deserialize<FileChunkListDto>(requestBody, options);
        if (chunks is null) return req.CreateResponse(System.Net.HttpStatusCode.NotFound);
        if (id != chunks.FileId) return req.CreateResponse(System.Net.HttpStatusCode.BadRequest);

        await m.Send(new ProcessMediaCommand(chunks), cancellationToken);
        return req.CreateResponse(System.Net.HttpStatusCode.OK);
    }

    [Function("SaveEdit")]
    public async Task<HttpResponseData> SaveEdit(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req)
    {
        try
        {
            await m.Send(new SaveEditCommand(req));
            return req.CreateResponse(System.Net.HttpStatusCode.OK);
        }
        catch (NullReferenceException)
        {
            return req.CreateResponse(System.Net.HttpStatusCode.NotFound);
        }
        catch (InvalidOperationException ex)
        {
            var response = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(ex);
            return response;
        }
    }

    [Function("DeleteImage")]
    public async Task<HttpResponseData> DeleteImage(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "folder/{parent}/{id}")] HttpRequestData req,
        string parent, string id)
    {
        Log.Information($"{req.Method} - Remove image {id} in parent {parent}");
        try
        {
            await m.Send(new DeleteImageCommand(parent, id));
            return req.CreateResponse(System.Net.HttpStatusCode.OK);
        }
        catch (NullReferenceException)
        {
            return req.CreateResponse(System.Net.HttpStatusCode.NotFound);
        }
    }

    [Function("MoveImage")]
    public async Task<HttpResponseData> MoveImage(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req)
    {
        var queryString = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var oldParent = queryString["oldParent"] ?? string.Empty;
        var newParent = queryString["newParent"] ?? string.Empty;
        var id = queryString["id"] ?? string.Empty;
        Log.Information($"Move folder {id} from {oldParent} to {newParent}");
        var image = await m.Send(new GetImageQuery(oldParent, id));
        await m.Send(new MoveImageCommand(newParent, image));
        return req.CreateResponse(System.Net.HttpStatusCode.OK);
    }

    [Function("GetImages")]
    public async Task<HttpResponseData> GetImages(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "folder/{parent}")] HttpRequestData req,
        string parent)
    {
        Log.Information($"{req.Method} images in parent {parent} ");
        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync((await m.Send(new GetImagesQuery(parent))).OrderBy(x => x.Description));
        return response;
    }
}