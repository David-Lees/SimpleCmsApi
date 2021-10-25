using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleCmsApi.Models
{
    public class GalleryFolder
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public List<GalleryImage> Images { get; set; }
        public List<GalleryFolder> Folders { get; set; }

        public GalleryFolder()
        {
            Images = new List<GalleryImage>();
            Folders = new List<GalleryFolder>();
        }
    }
}
