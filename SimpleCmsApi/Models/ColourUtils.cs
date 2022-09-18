// https://github.com/jellever/DominantColor/blob/master/DominantColor/ColorUtils.cs

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;
using Image = SixLabors.ImageSharp.Image;

namespace SimpleCmsApi.Models
{
    public static class ColourUtils
    {
        /// <summary>
        /// Get hue histogram for given bitmap.
        /// </summary>
        /// <param name="bmp">The bitmap to get the histogram for</param>
        /// <param name="saturationThreshold">The saturation threshold to take into account getting the histogram</param>
        /// <param name="brightnessThreshold">The brightness threshold to take into account getting the histogram</param>
        /// <returns>A dictionary representing the hue histogram. Key: Hue index (0-360). Value: Occurence of the hue.</returns>
        internal static Dictionary<int, uint> GetColorHueHistogram(Image image, float saturationThreshold, float brightnessThreshold)
        {
            var colorHueHistorgram = new Dictionary<int, uint>();
            for (int i = 0; i <= 360; i++)
            {
                colorHueHistorgram.Add(i, 0);
            }
            if (image is Image<Rgba32> imageRgba32)
            {
                imageRgba32.ProcessPixelRows(pixelAccessor =>
                {
                    for (int i = 0; i < pixelAccessor.Height; ++i)
                    {
                        var row = pixelAccessor.GetRowSpan(i);
                        for (int j = 0; j < row.Length; ++j)
                        {
                            var pixel = row[j];
                            RgbToHls(pixel.R, pixel.G, pixel.B, out double h, out double l, out double s);
                            if (s > saturationThreshold && l > brightnessThreshold)
                            {
                                int hue = (int)Math.Round(h, 0);
                                colorHueHistorgram[hue]++;
                            }
                        }
                    }
                });
            }

            return colorHueHistorgram;
        }

        /// <summary>
        /// Calculate average RGB color for given bitmap
        /// </summary>
        /// <param name="bmp">The bitmap to calculate the average color for.</param>
        /// <returns>Average color</returns>
        internal static Color GetAverageRGBColor(Image bmp)
        {
            int totalRed = 0;
            int totalGreen = 0;
            int totalBlue = 0;

            if (bmp is Image<Rgba32> imageRgba32)
            {
                imageRgba32.ProcessPixelRows(pixelAccessor =>
                {
                    for (int i = 0; i < pixelAccessor.Height; ++i)
                    {
                        var row = pixelAccessor.GetRowSpan(i);
                        for (int j = 0; j < row.Length; ++j)
                        {
                            var clr = row[j];
                            totalRed += clr.R;
                            totalGreen += clr.G;
                            totalBlue += clr.B;
                        }
                    }
                });

                int totalPixels = bmp.Width * bmp.Height;
                byte avgRed = (byte)(totalRed / totalPixels);
                byte avgGreen = (byte)(totalGreen / totalPixels);
                byte avgBlue = (byte)(totalBlue / totalPixels);
                return Color.FromRgb(avgRed, avgGreen, avgBlue);
            }
            return Color.Transparent;
        }

        /// <summary>
        /// Correct out of bound hue index
        /// </summary>
        /// <param name="hue">hue index</param>
        /// <returns>Corrected hue index (within 0-360 boundaries)</returns>
        private static int CorrectHueIndex(int hue)
        {
            int result = hue;
            if (result > 360)
                result -= 360;
            if (result < 0)
                result += 360;
            return result;
        }

        /// <summary>
        /// Get color from HSV (Hue, Saturation, Brightness) combination.
        /// </summary>
        /// <param name="hue"></param>
        /// <param name="saturation"></param>
        /// <param name="value"></param>
        /// <returns>The color</returns>
        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value *= 255;
            byte v = Convert.ToByte(value);
            byte p = Convert.ToByte(value * (1 - saturation));
            byte q = Convert.ToByte(value * (1 - f * saturation));
            byte t = Convert.ToByte(value * (1 - (1 - f) * saturation));

            return hi switch
            {
                0 => Color.FromRgba(v, t, p, 255),
                1 => Color.FromRgba(q, v, p, 255),
                2 => Color.FromRgba(p, v, t, 255),
                3 => Color.FromRgba(p, q, v, 255),
                4 => Color.FromRgba(t, p, v, 255),
                _ => Color.FromRgba(v, p, q, 255),
            };
        }

        /// <summary>
        /// Smooth histogram with given smoothfactor.
        /// </summary>
        /// <param name="colorHueHistogram">The histogram to smooth</param>
        /// <param name="smoothFactor">How many hue neighbouring hue indexes will be averaged by the smoothing algoritme.</param>
        /// <returns>Smoothed hue color histogram</returns>
        internal static Dictionary<int, uint> SmoothHistogram(Dictionary<int, uint> colorHueHistogram, int smoothFactor)
        {
            if (smoothFactor < 0 || smoothFactor > 360)
                throw new ArgumentException("smoothFactor may not be negative or bigger then 360", nameof(smoothFactor));
            if (smoothFactor == 0)
                return new Dictionary<int, uint>(colorHueHistogram);

            var newHistogram = new Dictionary<int, uint>();
            int totalNrColumns = (smoothFactor * 2) + 1;
            for (int i = 0; i <= 360; i++)
            {
                uint sum = 0;
                for (int x = i - smoothFactor; x <= i + smoothFactor; x++)
                {
                    int hueIndex = CorrectHueIndex(x);
                    sum += colorHueHistogram[hueIndex];
                }
                uint average = (uint)(sum / totalNrColumns);
                newHistogram[i] = average;
            }
            return newHistogram;
        }

        // Convert an RGB value into an HLS value.
        // http://csharphelper.com/blog/2016/08/convert-between-rgb-and-hls-color-models-in-c/
        public static void RgbToHls(int r, int g, int b,
            out double h, out double l, out double s)
        {
            // Convert RGB to a 0.0 to 1.0 range.
            double double_r = r / 255.0;
            double double_g = g / 255.0;
            double double_b = b / 255.0;

            // Get the maximum and minimum RGB components.
            double max = double_r;
            if (max < double_g) max = double_g;
            if (max < double_b) max = double_b;

            double min = double_r;
            if (min > double_g) min = double_g;
            if (min > double_b) min = double_b;

            double diff = max - min;
            l = (max + min) / 2;
            if (Math.Abs(diff) < 0.00001)
            {
                s = 0;
                h = 0;  // H is really undefined.
            }
            else
            {
                if (l <= 0.5) s = diff / (max + min);
                else s = diff / (2 - max - min);

                double r_dist = (max - double_r) / diff;
                double g_dist = (max - double_g) / diff;
                double b_dist = (max - double_b) / diff;

                if (double_r == max) h = b_dist - g_dist;
                else if (double_g == max) h = 2 + r_dist - b_dist;
                else h = 4 + g_dist - r_dist;

                h *= 60;
                if (h < 0) h += 360;
            }
        }
    }
}