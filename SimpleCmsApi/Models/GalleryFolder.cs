using Azure;
using Azure.Data.Tables;
using System.Text.Json.Serialization;

namespace SimpleCmsApi.Models
{
    public class GalleryFolder : ITableEntity
    {
        public GalleryFolder(string parentFolderId, string id, string name)
        {
            PartitionKey = parentFolderId;
            RowKey = id;
            Name = name;
        }

        public GalleryFolder(GalleryFolderRequest req)
        {
            PartitionKey = req.PartitionKey;
            RowKey= req.RowKey;
            Name = req.Name;
        }

        public GalleryFolder()
        {
        }

        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get ; set; }
        public ETag ETag { get; set; }
    }

    public class GalleryFolderRequest
    {
        [JsonPropertyName("partitionKey")]
        public string PartitionKey { get; set; } = string.Empty;

        [JsonPropertyName("rowKey")]
        public string RowKey { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { set; get; } = string.Empty;
    }
}
