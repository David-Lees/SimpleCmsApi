using Microsoft.Azure.Cosmos.Table;

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

        public string PreviewSmallPath { get; set; }
        public int PreviewSmallWidth { get; set; }
        public int PreviewSmallHeight { get; set; }

        public string PreviewMediumPath { get; set; }
        public int PreviewMediumWidth { get; set; }
        public int PreviewMediumHeight { get; set; }

        public string PreviewLargePath { get; set; }
        public int PreviewLargeWidth { get; set; }
        public int PreviewLargeHeight { get; set; }

        public string RawPath { get; set; }
        public int RawWidth { get; set; }
        public int RawHeight { get; set; }

        public string DominantColour { get; set; }
        public string Description { get; set; }
    }
}
