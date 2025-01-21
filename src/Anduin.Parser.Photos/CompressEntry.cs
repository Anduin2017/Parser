using Microsoft.Extensions.Logging;

namespace Anduin.Parser.Photos;

public class CompressEntry(ILogger<CompressEntry> logger, ImageCompressor imageCompressor)
{
    public async Task OnServiceStartedAsync(
        string fullPath, 
        bool shouldTakeAction, 
        double scale, 
        int onlyIfPhotoLargerThanMb, 
        bool deleteOriginal,
        string[] extensions)
    {
        if (!shouldTakeAction) return;
        
        var files = Directory.GetFiles(fullPath, "*.*", SearchOption.AllDirectories)
            .Where(file => extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .ToArray();
        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            if (fileInfo.Length > onlyIfPhotoLargerThanMb * 1024 * 1024)
            {
                var compressedPath = await imageCompressor.CompressImage(file, scale);
                logger.LogInformation("Compressed photo {File} to {CompressedPath}", file, compressedPath);
                
                var originalSize = new FileInfo(file).Length;
                var compressedSize = new FileInfo(compressedPath).Length;
                var savedSize = originalSize - compressedSize;
                logger.LogInformation("Original size: {OriginalSize} bytes ({OriginalSizeInKb} KB), Compressed size: {CompressedSize} bytes ({CompressedSizeInKb} KB)", originalSize, originalSize / 1024, compressedSize, compressedSize / 1024);
                logger.LogInformation("Saved {SavedSize} bytes ({SavedSizeInKb} KB) by compressing photo {File}", savedSize, savedSize / 1024, file);
                
                if (deleteOriginal)
                {
                    File.Delete(file);
                    logger.LogWarning("Deleted original photo {File}", file);
                }
            }
            else
            {
                logger.LogTrace("Skipped photo {File} because it is smaller than {OnlyIfPhotoLargerThanMb} MB", file, onlyIfPhotoLargerThanMb);
            }
        }
        
        logger.LogInformation("Finished compressing photos.");
    }
}