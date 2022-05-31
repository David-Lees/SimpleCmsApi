using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleCmsApi.Models
{
    // https://github.com/jellever/DominantColor/blob/master/DominantColor/DominantHueColorCalculator.cs
    public class DominantHueColorCalculator : IDominantColorCalculator
    {
        private readonly float saturationThreshold;
        private readonly float brightnessThreshold;
        private readonly int hueSmoothFactor;
        private Dictionary<int, uint> hueHistogram;
        private Dictionary<int, uint> smoothedHueHistogram;

        /// <summary>
        /// The Hue histogram used in the calculation for dominant color
        /// </summary>
        public Dictionary<int, uint> HueHistogram
        {
            get
            {
                return new Dictionary<int, uint>(hueHistogram);
            }
        }

        /// <summary>
        /// The smoothed histogram used in the calculation for dominant color
        /// </summary>
        public Dictionary<int, uint> SmoothedHueHistorgram
        {
            get
            {
                return new Dictionary<int, uint>(smoothedHueHistogram);
            }
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="saturationThreshold">The saturation thresshold</param>
        /// <param name="brightnessThreshold">The brightness thresshold</param>
        /// <param name="hueSmoothFactor">hue smoothing factor</param>
        public DominantHueColorCalculator(float saturationThreshold, float brightnessThreshold, int hueSmoothFactor)
        {
            this.saturationThreshold = saturationThreshold;
            this.brightnessThreshold = brightnessThreshold;
            this.hueSmoothFactor = hueSmoothFactor;
            hueHistogram = new Dictionary<int, uint>();
            smoothedHueHistogram = new Dictionary<int, uint>();
        }

        public DominantHueColorCalculator() :
            this(0.3f, 0.0f, 4)
        {
        }

        /// <summary>
        /// Get dominant hue in given hue histogram
        /// </summary>
        /// <param name="hueHistogram"></param>
        /// <returns></returns>
        private static int GetDominantHue(Dictionary<int, uint> hueHistogram)
        {
            int dominantHue = hueHistogram.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            return dominantHue;
        }

        /// <summary>
        /// Calculate dominant color for given bitmap
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public Color CalculateDominantColor(Image bitmap)
        {
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));
            hueHistogram = ColourUtils.GetColorHueHistogram(bitmap, saturationThreshold, brightnessThreshold);
            smoothedHueHistogram = ColourUtils.SmoothHistogram(hueHistogram, hueSmoothFactor);
            int dominantHue = GetDominantHue(smoothedHueHistogram);
            return ColourUtils.ColorFromHSV(dominantHue, 1, 1);
        }
    }

}
