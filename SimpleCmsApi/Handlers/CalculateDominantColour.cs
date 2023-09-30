using MediatR;
using OpenCvSharp;

namespace SimpleCmsApi.Handlers;

public record Colour(byte Red, byte Green, byte Blue);
public record CalculateDominantColourQuery(byte[] Image) : IRequest<Colour>;

public class CalculateDominantColourHandler : IRequestHandler<CalculateDominantColourQuery, Colour>
{
    public Task<Colour> Handle(CalculateDominantColourQuery request, CancellationToken cancellationToken)
    {
        using var image = Cv2.ImDecode(request.Image, ImreadModes.Unchanged);
        image.CvtColor(ColorConversionCodes.BGR2HSV);
        var channels = image.Split();

        // Calculate histogram
        Mat hist = new();
        int[] hdims = { 256 }; // Histogram size for each dimension
        Rangef[] ranges = { new Rangef(0, 256), }; // min/max 
        Cv2.CalcHist(
            new Mat[] { channels[0] },
            new int[] { 0 },
            null,
            hist,
            1,
            hdims,
            ranges);

        // Get the max value of histogram
        Cv2.MinMaxLoc(hist, out double minVal, out double maxVal);

        int hi = Convert.ToInt32(Math.Floor(maxVal / 60)) % 6;
        double f = maxVal / 60 - Math.Floor(maxVal / 60);

        var value = 255;
        var saturation = 1;
        byte v = Convert.ToByte(value);
        byte p = Convert.ToByte(value * (1 - saturation));
        byte q = Convert.ToByte(value * (1 - f * saturation));
        byte t = Convert.ToByte(value * (1 - (1 - f) * saturation));

        foreach (var channel in channels) channel.Dispose();

        return Task.FromResult(hi switch
        {
            0 => new Colour(v, t, p),
            1 => new Colour(q, v, p),
            2 => new Colour(p, v, t),
            3 => new Colour(p, q, v),
            4 => new Colour(t, p, v),
            _ => new Colour(v, p, q),
        });
    }
}
