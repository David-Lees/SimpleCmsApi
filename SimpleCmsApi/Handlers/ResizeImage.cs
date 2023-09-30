using MediatR;
using OpenCvSharp;
using System;

namespace SimpleCmsApi.Handlers;

public record ResizeImageResult(Mat Image, int Width, int Height);
public record ResizeImageCommand(byte[] Image, int? Resolution) : IRequest<ResizeImageResult>;

public class ResizeImageHandler : IRequestHandler<ResizeImageCommand, ResizeImageResult>
{
    public async Task<ResizeImageResult> Handle(ResizeImageCommand request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        var image = Cv2.ImDecode(request.Image, ImreadModes.Unchanged);

        var width = image.Width;
        var height = image.Height;

        if (request.Resolution != null)
        {
            double ratio = (double)request.Resolution / image.Height;
            width = (int)(image.Width * ratio);
            height = (int)(image.Height * ratio);
            if (ratio < 1.0)
            {
                image.Resize(new Size(width, height));
            }
        }

        return new ResizeImageResult(image, width, height);
    }
}
