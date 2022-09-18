using Azure;
using Azure.Data.Tables;

namespace SimpleCmsApi.Models
{
    public class GalleryImage: ITableEntity
    {
        public GalleryImage(string parentFolderId, string id)
        {
            PartitionKey = parentFolderId;
            RowKey = id;
        }

        public GalleryImage()
        {
        }

        public string PreviewSmallPath { get; set; } = string.Empty;
        public int PreviewSmallWidth { get; set; }
        public int PreviewSmallHeight { get; set; }

        public string PreviewMediumPath { get; set; } = string.Empty;
        public int PreviewMediumWidth { get; set; }
        public int PreviewMediumHeight { get; set; }

        public string PreviewLargePath { get; set; } = string.Empty;
        public int PreviewLargeWidth { get; set; }
        public int PreviewLargeHeight { get; set; }

        public string RawPath { get; set; } = string.Empty;
        public int RawWidth { get; set; }
        public int RawHeight { get; set; }

        public string DominantColour { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
