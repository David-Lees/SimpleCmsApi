using SkiaSharp;

namespace SimpleCmsApi.Models;

public interface IDominantColorCalculator
{
    SKColor CalculateDominantColor(SKBitmap bitmap);
}