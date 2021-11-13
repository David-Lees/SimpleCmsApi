using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SimpleCmsApi.Models;
using SimpleCmsApi.Services;
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
            var item = await JsonSerializer.DeserializeAsync<GalleryFolder>(req.Body);
            log.LogInformation($"Create folder {item.Name}, id {item.RowKey} in parent {item.PartitionKey}");
            await FolderService.Instance.CreateFolder(item);
            return new OkResult();
        }

        [FunctionName("DeleteFolder")]
        public static async Task<IActionResult> DeleteFolder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "folder")] HttpRequest req,
            ILogger log)
        {
            var item = await JsonSerializer.DeserializeAsync<GalleryFolder>(req.Body);
            if (item != null)
            {
                log.LogInformation($"Delete folder {item.RowKey} in parent {item.PartitionKey}");
                await FolderService.Instance.DeleteFolder(item);
            }
            return new OkResult();
        }

        [FunctionName("MoveFolder")]
        public static async Task<IActionResult> MoveFolder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "folder/{newParent}")] HttpRequest req,
            ILogger log,
            string newParent)
        {
            var item = await JsonSerializer.DeserializeAsync<GalleryFolder>(req.Body);
            if (item != null)
            {
                log.LogInformation($"Move folder {item.Name} from {item.PartitionKey} to {newParent}");
                await FolderService.Instance.MoveFolder(newParent, item);
            }
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
