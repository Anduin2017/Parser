using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Anduin.Parser.Photos;

/// <summary>
/// Service class for compressing images.
/// </summary>
public class ImageCompressor
{
    /// <summary>
    /// Compresses the image at the specified path to the specified scale. Default is 0.5 (i.e., scales width and height by 50%).
    /// The compressed image is located in the same folder as the original image, with the suffix "_comp" added to the file name.
    /// </summary>
    /// <param name="path">Path to the original image.</param>
    /// <param name="scaleFactor">Scaling factor. Default is 0.5.</param>
    /// <returns>Path to the compressed image.</returns>
    /// <exception cref="ArgumentException">Thrown when the image file does not exist or the path is empty.</exception>
    public async Task<string> CompressImage(string path, double scaleFactor = 0.5)
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

        // Get the directory, file name without extension, and extension of the original image
        var directory = Path.GetDirectoryName(path);
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);

        // Destination path
        var compressedFileName = $"{fileNameWithoutExt}_comp{extension}";
        var destinationPath = Path.Combine(directory!, compressedFileName);

        // Use ImageSharp to read and compress the image
        using var image = await Image.LoadAsync(path);
        // Calculate the target size
        var newWidth = (int)(image.Width * scaleFactor);
        var newHeight = (int)(image.Height * scaleFactor);

        // Perform the scaling
        image.Mutate(x => x.Resize(newWidth, newHeight));

        // Save the scaled image
        // Here, it is saved in the original format, but it can also be saved in a specific format (JPEG/PNG, etc.) if needed
        await image.SaveAsync(destinationPath);

        return destinationPath;
    }
}