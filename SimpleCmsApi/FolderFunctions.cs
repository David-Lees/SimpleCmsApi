using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SimpleCmsApi.Models;
using SimpleCmsApi.Services;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimpleCmsApi
{
    public static class FolderFunctions
    {
        [FunctionName("CreateFolder")]
        public static async Task<IActionResult> CreateFolder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "folder")] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var item = JsonSerializer.Deserialize<GalleryFolderRequest>(requestBody);
            if (item == null) return new NotFoundResult();
            log.LogInformation($"Create folder {item.Name}, id {item.RowKey} in parent {item.PartitionKey} ({requestBody})");
            await FolderService.Instance.CreateFolder(new GalleryFolder(item));
            return new OkResult();
        }

        [FunctionName("DeleteFolder")]
        public static async Task<IActionResult> DeleteFolder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "folder")] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var item = JsonSerializer.Deserialize<GalleryFolderRequest>(requestBody);
            if (item == null) return new NotFoundResult();
            log.LogInformation($"Delete folder {item.RowKey} in parent {item.PartitionKey}");
            await FolderService.Instance.DeleteFolder(new(item));
            return new OkResult();
        }

        [FunctionName("MoveFolder")]
        public static async Task<IActionResult> MoveFolder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "folder/{newParent}")] HttpRequest req,
            ILogger log,
            string newParent)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var item = JsonSerializer.Deserialize<GalleryFolderRequest>(requestBody);
            if (item == null) return new NotFoundResult();
            log.LogInformation($"Move folder {item.Name} from {item.PartitionKey} to {newParent}");
            await FolderService.Instance.MoveFolder(newParent, new(item));
            return new OkResult();
        }

        [FunctionName("GetFolders")]
        public static IActionResult GetFolders(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "folder")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"Get All Folders - {req.Path}");
            return new OkObjectResult(FolderService.Instance.GetFolders());
        }
    }
}
