using SkiaSharp;

namespace SimpleCmsApi.Models;

// https://github.com/jellever/DominantColor/blob/master/DominantColor/DominantHueColorCalculator.cs
/// <summary>
/// ctor
/// </summary>
/// <param name="saturationThreshold">The saturation thresshold</param>
/// <param name="brightnessThreshold">The brightness thresshold</param>
/// <param name="hueSmoothFactor">hue smoothing factor</param>
public class DominantHueColorCalculator(float saturationThreshold, float brightnessThreshold, int hueSmoothFactor) : IDominantColorCalculator
{
    private readonly float _saturationThreshold = saturationThreshold;
    private readonly float _brightnessThreshold = brightnessThreshold;
    private readonly int _hueSmoothFactor = hueSmoothFactor;
    private Dictionary<int, uint> _hueHistogram = [];
    private Dictionary<int, uint> _smoothedHueHistogram = [];

    /// <summary>
    /// The Hue histogram used in the calculation for dominant color
    /// </summary>
    public Dictionary<int, uint> HueHistogram
    {
        get
        {
            return new Dictionary<int, uint>(_hueHistogram);
        }
    }

    /// <summary>
    /// The smoothed histogram used in the calculation for dominant color
    /// </summary>
    public Dictionary<int, uint> SmoothedHueHistorgram
    {
        get
        {
            return new Dictionary<int, uint>(_smoothedHueHistogram);
        }
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
    public SKColor CalculateDominantColor(SKBitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        _hueHistogram = ColourUtils.GetColorHueHistogram(bitmap, _saturationThreshold, _brightnessThreshold);
        _smoothedHueHistogram = ColourUtils.SmoothHistogram(_hueHistogram, _hueSmoothFactor);
        var dominantHue = GetDominantHue(_smoothedHueHistogram);
        return SKColor.FromHsv(dominantHue, 1, 1);
    }
}