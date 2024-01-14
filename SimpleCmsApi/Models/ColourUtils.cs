// https://github.com/jellever/DominantColor/blob/master/DominantColor/ColorUtils.cs

using SkiaSharp;

namespace SimpleCmsApi.Models;

public static class ColourUtils
{
    /// <summary>
    /// Get hue histogram for given bitmap.
    /// </summary>
    /// <param name="bmp">The bitmap to get the histogram for</param>
    /// <param name="saturationThreshold">The saturation threshold to take into account getting the histogram</param>
    /// <param name="brightnessThreshold">The brightness threshold to take into account getting the histogram</param>
    /// <returns>A dictionary representing the hue histogram. Key: Hue index (0-360). Value: Occurence of the hue.</returns>
    internal static Dictionary<int, uint> GetColorHueHistogram(SKBitmap image, float saturationThreshold, float brightnessThreshold)
    {
        var colorHueHistorgram = new Dictionary<int, uint>();
        for (var i = 0; i <= 360; i++)
        {
            colorHueHistorgram.Add(i, 0);
        }

        foreach (var pixel in image.Pixels)
        {
            RgbToHls(pixel.Red, pixel.Green, pixel.Blue, out var h, out var l, out var s);
            if (s > saturationThreshold && l > brightnessThreshold)
            {
                var hue = (int)Math.Round(h, 0);
                colorHueHistorgram[hue]++;
            }
        }

        return colorHueHistorgram;
    }

    /// <summary>
    /// Calculate average RGB color for given bitmap
    /// </summary>
    /// <param name="bmp">The bitmap to calculate the average color for.</param>
    /// <returns>Average color</returns>
    internal static SKColor GetAverageRGBColor(SKBitmap bmp)
    {
        var totalRed = 0;
        var totalGreen = 0;
        var totalBlue = 0;

        foreach (var pixel in bmp.Pixels)
        {
            totalRed += pixel.Red;
            totalGreen += pixel.Green;
            totalBlue += pixel.Blue;
        }

        var totalPixels = bmp.Width * bmp.Height;
        var avgRed = (byte)(totalRed / totalPixels);
        var avgGreen = (byte)(totalGreen / totalPixels);
        var avgBlue = (byte)(totalBlue / totalPixels);
        return new SKColor(avgRed, avgGreen, avgBlue);
    }

    /// <summary>
    /// Correct out of bound hue index
    /// </summary>
    /// <param name="hue">hue index</param>
    /// <returns>Corrected hue index (within 0-360 boundaries)</returns>
    private static int CorrectHueIndex(int hue)
    {
        var result = hue;
        if (result > 360)
            result -= 360;
        if (result < 0)
            result += 360;
        return result;
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
        var totalNrColumns = smoothFactor * 2 + 1;
        for (var i = 0; i <= 360; i++)
        {
            uint sum = 0;
            for (var x = i - smoothFactor; x <= i + smoothFactor; x++)
            {
                var hueIndex = CorrectHueIndex(x);
                sum += colorHueHistogram[hueIndex];
            }
            var average = (uint)(sum / totalNrColumns);
            newHistogram[i] = average;
        }
        return newHistogram;
    }

    // Convert an RGB value into an HLS value.
    // http://csharphelper.com/blog/2016/08/convert-between-rgb-and-hls-color-models-in-c/
    public static void RgbToHls(byte r, byte g, byte b,
        out double h, out double l, out double s)
    {
        // Convert RGB to a 0.0 to 1.0 range.
        var double_r = r / 255.0;
        var double_g = g / 255.0;
        var double_b = b / 255.0;

        // Get the maximum and minimum RGB components.
        var max = double_r;
        if (max < double_g) max = double_g;
        if (max < double_b) max = double_b;

        var min = double_r;
        if (min > double_g) min = double_g;
        if (min > double_b) min = double_b;

        var diff = max - min;
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

            var r_dist = (max - double_r) / diff;
            var g_dist = (max - double_g) / diff;
            var b_dist = (max - double_b) / diff;

            if (double_r == max) h = b_dist - g_dist;
            else if (double_g == max) h = 2 + r_dist - b_dist;
            else h = 4 + g_dist - r_dist;

            h *= 60;
            if (h < 0) h += 360;
        }
    }
}