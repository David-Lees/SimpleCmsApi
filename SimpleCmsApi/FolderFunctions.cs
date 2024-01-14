using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Serilog;
using SimpleCmsApi.Handlers;
using SimpleCmsApi.Models;
using System.Text.Json;

namespace SimpleCmsApi;

public class FolderFunctions(IMediator m)
{
    [Function("CreateFolder")]
    public async Task<HttpResponseData> CreateFolder(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "folder")] HttpRequestData req)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var item = JsonSerializer.Deserialize<GalleryFolderRequest>(requestBody);
        if (item == null) return req.CreateResponse(System.Net.HttpStatusCode.NotFound);
        Log.Information($"Create folder {item.Name}, id {item.RowKey} in parent {item.PartitionKey} ({requestBody})");
        await m.Send(new CreateFolderCommand(new GalleryFolder(item)));
        return req.CreateResponse(System.Net.HttpStatusCode.OK);
    }

    [Function("DeleteFolder")]
    public async Task<HttpResponseData> DeleteFolder(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "folder")] HttpRequestData req)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var item = JsonSerializer.Deserialize<GalleryFolderRequest>(requestBody);
        if (item == null) return req.CreateResponse(System.Net.HttpStatusCode.NotFound);
        Log.Information($"Delete folder {item.RowKey} in parent {item.PartitionKey}");
        await m.Send(new DeleteFolderCommand(new(item)));
        return req.CreateResponse(System.Net.HttpStatusCode.OK);
    }

    [Function("MoveFolder")]
    public async Task<HttpResponseData> MoveFolder(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "folder/{newParent}")] HttpRequestData req,
        string newParent)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var item = JsonSerializer.Deserialize<GalleryFolderRequest>(requestBody);
        if (item == null) return req.CreateResponse(System.Net.HttpStatusCode.NotFound);
        Log.Information($"Move folder {item.Name} from {item.PartitionKey} to {newParent}");
        await m.Send(new MoveFolderCommand(newParent, new(item)));
        return req.CreateResponse(System.Net.HttpStatusCode.OK);
    }

    [Function("GetFolders")]
    public async Task<HttpResponseData> GetFolders(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "folder")] HttpRequestData req)
    {
        try
        {
            Log.Information($"Get All Folders {req.Url.Query ?? string.Empty}");
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(await m.Send(new GetFoldersQuery()));
            return response;
        }
        catch (Exception ex)
        {
            var response = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(ex);
            return response;
        }
    }
}