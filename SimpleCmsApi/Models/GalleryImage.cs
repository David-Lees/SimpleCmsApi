using System;
using System.Collections.Generic;

namespace SimpleCmsApi.Models
{
    public class GalleryImage
    {
        public Dictionary<string, GalleryImageDetails> Files { get; set; }

        public Guid Id { get; set; }

        public string DominantColour { get; set; }

        public string Description { get; set; }
    }
}
