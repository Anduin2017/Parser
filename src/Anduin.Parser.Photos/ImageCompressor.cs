using SkiaSharp;

namespace Anduin.Parser.Photos;

public class ImageCompressor
{
    public string CompressImage(string path, double scaleFactor = 0.5)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Image path cannot be empty!", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Image file does not exist!", path);
        }

        if (scaleFactor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scaleFactor),
                "Scaling factor must be greater than 0.");
        }

        var directory = Path.GetDirectoryName(path);
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);

        var compressedFileName = $"{fileNameWithoutExt}_comp{extension}";
        var destinationPath = Path.Combine(directory!, compressedFileName);

        using var bitmap = SKBitmap.Decode(path)
            ?? throw new InvalidOperationException($"Failed to decode image: {path}");

        var newWidth = (int)(bitmap.Width * scaleFactor);
        var newHeight = (int)(bitmap.Height * scaleFactor);

        using var resized = new SKBitmap(newWidth, newHeight);
        using var canvas = new SKCanvas(resized);
        using var paint = new SKPaint();
        paint.IsAntialias = true;
canvas.DrawImage(SKImage.FromBitmap(bitmap),
                    new SKRect(0, 0, bitmap.Width, bitmap.Height),
                    new SKRect(0, 0, newWidth, newHeight),
                    SKSamplingOptions.Default,
                    paint);
        canvas.Flush();

        var format = Path.GetExtension(destinationPath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
            ".png" => SKEncodedImageFormat.Png,
            ".webp" => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Png
        };

        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(format, format == SKEncodedImageFormat.Jpeg ? 90 : 100);
        using var stream = File.OpenWrite(destinationPath);
        data.SaveTo(stream);

        return destinationPath;
    }
}