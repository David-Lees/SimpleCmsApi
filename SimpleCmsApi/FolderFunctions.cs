using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Serilog;
using SimpleCmsApi.Handlers;
using SimpleCmsApi.Models;
using System.Text.Json;

namespace SimpleCmsApi;

public class FolderFunctions
{
    private readonly IMediator _m;

    public FolderFunctions(IMediator m)
    {
        _m = m;
    }

    [FunctionName("CreateFolder")]
    public async Task<IActionResult> CreateFolder(
        [HttpTrigger(AuthorizationLevel.User, "post", Route = "folder")] HttpRequest req)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var item = JsonSerializer.Deserialize<GalleryFolderRequest>(requestBody);
        if (item == null) return new NotFoundResult();
        Log.Information($"Create folder {item.Name}, id {item.RowKey} in parent {item.PartitionKey} ({requestBody})");
        await _m.Send(new CreateFolderCommand(new GalleryFolder(item)));
        return new OkResult();
    }

    [FunctionName("DeleteFolder")]
    public async Task<IActionResult> DeleteFolder(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "folder")] HttpRequest req)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var item = JsonSerializer.Deserialize<GalleryFolderRequest>(requestBody);
        if (item == null) return new NotFoundResult();
        Log.Information($"Delete folder {item.RowKey} in parent {item.PartitionKey}");
        await _m.Send(new DeleteFolderCommand(new(item)));
        return new OkResult();
    }

    [FunctionName("MoveFolder")]
    public async Task<IActionResult> MoveFolder(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "folder/{newParent}")] HttpRequest req,        
        string newParent)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var item = JsonSerializer.Deserialize<GalleryFolderRequest>(requestBody);
        if (item == null) return new NotFoundResult();
        Log.Information($"Move folder {item.Name} from {item.PartitionKey} to {newParent}");
        await _m.Send(new MoveFolderCommand(newParent, new(item)));
        return new OkResult();
    }

    [FunctionName("GetFolders")]
    public async Task<IActionResult> GetFolders(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "folder")] HttpRequest req)
    {
        Log.Information($"Get All Folders - {req.Path}");
        return new OkObjectResult(await _m.Send(new GetFoldersQuery()));
    }
}
