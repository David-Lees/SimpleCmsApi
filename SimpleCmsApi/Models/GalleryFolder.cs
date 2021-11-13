using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;

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
}
