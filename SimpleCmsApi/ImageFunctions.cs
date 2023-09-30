using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Serilog;
using SimpleCmsApi.Handlers;

namespace SimpleCmsApi;

public class ImageFunctions
{
    private readonly IMediator _m;

    public ImageFunctions(IMediator m)
    {
        _m = m;
    }

    [Function("ProcessUpload")]
    public async Task<IActionResult> ProcessUpload(
       [HttpTrigger(AuthorizationLevel.User, "get", "post", Route = null)] HttpRequest req)
    {
        await _m.Send(new ProcessMediaCommand(req));
        return new OkResult();
    }

    [Function("DeleteImage")]
    public async Task<IActionResult> DeleteImage(
        [HttpTrigger(AuthorizationLevel.User, "delete", Route = "folder/{parent}/{id}")] HttpRequest req,
        string parent, string id)
    {
        Log.Information($"{req.Method} - Remove image {id} in parent {parent}");
        try
        {
            await _m.Send(new DeleteImageCommand(parent, id));
            return new OkResult();
        }
        catch (NullReferenceException)
        {
            return new NotFoundResult();
        }
    }

    [Function("MoveImage")]
    public async Task<IActionResult> MoveImage(
        [HttpTrigger(AuthorizationLevel.User, "get", "post", Route = null)] HttpRequest req)
    {
        var oldParent = req.Query["oldParent"].FirstOrDefault();
        var newParent = req.Query["newParent"].FirstOrDefault();
        var id = req.Query["id"].FirstOrDefault(); 
        if (string.IsNullOrEmpty(oldParent) || string.IsNullOrEmpty(newParent) || string.IsNullOrEmpty(id))
        {
            return new NotFoundResult();
        }
        Log.Information($"Move folder {id} from {oldParent} to {newParent}");
        var image = await _m.Send(new GetImageQuery(oldParent, id));
        await _m.Send(new MoveImageCommand(newParent, image));
        return new OkResult();
    }

    [Function("GetImages")]
    public async Task<IActionResult> GetImages(
        [HttpTrigger(AuthorizationLevel.User, "get", Route = "folder/{parent}")] HttpRequest req,
        string parent)
    {
        Log.Information($"{req.Method} images in parent {parent} ");        
        return new OkObjectResult((await _m.Send(new GetImagesQuery(parent))).OrderBy(x => x.Description));
    }
}
