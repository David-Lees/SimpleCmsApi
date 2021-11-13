using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SimpleCmsApi.Models
{
    public class GalleryFolder : TableEntity
    {
        public GalleryFolder(string parentFolderId, string id, string name)
        {
            PartitionKey = parentFolderId;
            RowKey = id;
            Name = name;
        }

        public GalleryFolder()
        {
        }

        public string Name { get; set; }
    }

    public class GalleryFolderRequest
    {
        [JsonPropertyName("partitionKey")]
        public string PartitionKey { get; set; }

        [JsonPropertyName("rowKey")]
        public string RowKey { get; set; }

        [JsonPropertyName("name")]
        public string Name { set; get; }
    }
}
