using SixLabors.ImageSharp;

namespace SimpleCmsApi.Models
{
    public interface IDominantColorCalculator
    {
        Color CalculateDominantColor(Image bitmap);
    }
}
