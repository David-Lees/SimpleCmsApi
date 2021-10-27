using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;

namespace SimpleCmsApi.Models
{
    public class GalleryImage: TableEntity
    {
        public GalleryImage(string parentFolderId, string id)
        {
            PartitionKey = parentFolderId;
            RowKey = id;
        }

        public GalleryImage()
        {
        }

        public Dictionary<string, GalleryImageDetails> Files { get; set; }

        public string DominantColour { get; set; }

        public string Description { get; set; }
    }
}
