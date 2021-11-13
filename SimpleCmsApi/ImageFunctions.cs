using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SimpleCmsApi.Services;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleCmsApi
{
    public static class ImageFunctions
    {
        [FunctionName("ProcessUpload")]
        public static async Task<IActionResult> ProcessUpload(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            await MediaProcessingService.ProcessMediaAsync(req, log);
            return new OkResult();
        }

        [FunctionName("DeleteImage")]
        public static async Task<IActionResult> DeleteImage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "folder/{parent}/{id}")] HttpRequest req,
            ILogger log, string parent, string id)
        {
            log.LogInformation($"{req.Method} - Remove image {id} in parent {parent}");
            await ImageService.Instance.DeleteImage(parent, id);
            return new OkResult();
        }

        [FunctionName("MoveImage")]
        public static async Task<IActionResult> MoveImage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var oldParent = req.Query["oldParent"];
            var newParent = req.Query["newParent"];
            var id = req.Query["id"];
            log.LogInformation($"Move folder {id} from {oldParent} to {newParent}");
            await ImageService.Instance.MoveImage(oldParent, newParent, id);
            return new OkResult();
        }

        [FunctionName("GetImages")]
        public static IActionResult GetImages(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "folder/{parent}")] HttpRequest req,
        ILogger log, string parent)
        {
            log.LogInformation($"{req.Method} images in parent {parent} ");
            return new OkObjectResult(ImageService.Instance.GetImages(parent).OrderBy(x => x.Description));
        }
    }
}
