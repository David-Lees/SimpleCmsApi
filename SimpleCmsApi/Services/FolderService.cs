using Microsoft.Azure.Cosmos.Table;
using SimpleCmsApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleCmsApi.Services
{
    public class FolderService
    {
        private readonly CloudTable _folders;

        public readonly static FolderService Instance = new();

        public FolderService()
        {
            var tableAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var client = tableAccount.CreateCloudTableClient();
            _folders = client.GetTableReference("Folders");
        }

        public async Task CreateFolder(GalleryFolder item)
        {
            var insertOperation = TableOperation.InsertOrReplace(item);
            await ExecuteTableOperation(insertOperation);
        }

        public async Task DeleteFolder(GalleryFolder item)
        {
            var empty = Guid.Empty.ToString();
            if (item.PartitionKey != empty && item.RowKey != empty)
            {
                var operation = TableOperation.Delete(item);
                await ExecuteTableOperation(operation);
            }
        }

        public async Task<GalleryFolder> GetFolder(string parentId, string id)
        {
            var operation = TableOperation.Retrieve<GalleryFolder>(parentId, id);
            return await ExecuteTableOperation(operation) as GalleryFolder;
        }

        public List<GalleryFolder> GetFolders()
        {
            return _folders.ExecuteQuery(new TableQuery<GalleryFolder>()).ToList();         
        }

        public async Task MoveFolder(string newParent, GalleryFolder item)
        {
            if (item.RowKey != Guid.Empty.ToString())
            {
                await DeleteFolder(item);
                item.PartitionKey = newParent;
                await CreateFolder(item);
            }
        }

        private async Task<object> ExecuteTableOperation(TableOperation tableOperation)
        {
            await _folders.CreateIfNotExistsAsync();
            var tableResult = await _folders.ExecuteAsync(tableOperation);
            return tableResult.Result;
        }
    }
}
