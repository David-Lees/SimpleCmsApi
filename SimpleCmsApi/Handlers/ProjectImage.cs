using MediatR;
using OpenCvSharp;

namespace SimpleCmsApi.Handlers;

public record ProjectImageResult(byte[] Image, int Width, int Height);
public record ProjectImageCommand(byte[] Image, Point2f TopLeft, Point2f TopRight, Point2f BottomRight, Point2f BottomLeft) : IRequest<ProjectImageResult>;

public class ProjectImageHandler : IRequestHandler<ProjectImageCommand, ProjectImageResult>
{
    private static float Distance(Point2f a, Point2f b)
    {
        var c = b.X - a.X;
        var d = b.Y - a.Y;
        return MathF.Sqrt(c * c + d * d);
    }

    public async Task<ProjectImageResult> Handle(ProjectImageCommand request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        using var _source = Cv2.ImDecode(request.Image, ImreadModes.Unchanged);

        var h = MathF.Ceiling((Distance(request.TopLeft, request.BottomLeft) + Distance(request.TopRight, request.BottomRight)) / 2);
        var w = MathF.Ceiling((Distance(request.TopLeft, request.TopRight) + Distance(request.BottomLeft, request.BottomRight)) / 2);

        using var matrix = Cv2.GetPerspectiveTransform(
            new Point2f[] { request.TopLeft, request.TopRight, request.BottomRight, request.BottomLeft },
            new Point2f[] { new(0, 0), new(w, 0), new(w, h), new Point2f(0, h) }
        );

        var size = new Size(w, h);
        using var _destination = new Mat(size, _source.Type());
        
        Cv2.WarpPerspective(_source, _destination, matrix, size);

        return new ProjectImageResult(_destination.ImEncode(), (int)w, (int)h);
    }
}
