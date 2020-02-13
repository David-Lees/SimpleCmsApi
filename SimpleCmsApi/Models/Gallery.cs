using System.Collections.Generic;

namespace SimpleCmsApi.Models
{
    public class Gallery
    {
        public List<GalleryImage> Images { get; } = new List<GalleryImage>();
    }
}
