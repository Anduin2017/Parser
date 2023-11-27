using Aiursoft.CSTools.Services;
using Microsoft.Extensions.Logging;

namespace Anduin.Parser.FFmpeg
{
    public class FFmpegEntry
    {
        private readonly ILogger<FFmpegEntry> _logger;
        private readonly FFmpegOptions _options;
        private readonly CommandService _commandService;
        private const long MbToBytes = 1024 * 1024;

        public FFmpegEntry(
            ILogger<FFmpegEntry> logger,
            FFmpegOptions options,
            CommandService commandService)
        {
            _logger = logger;
            _options = options;
            _commandService = commandService;
        }

        public async Task OnServiceStartedAsync(string path, bool shouldTakeAction)
        {
            _logger.LogTrace("Enumerating files under path: {Path}", path);
            var videos = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(v =>
                    v.EndsWith(".webm") ||
                    v.EndsWith(".mp4") ||
                    v.EndsWith(".avi") ||
                    v.EndsWith(".wmv") ||
                    v.EndsWith(".mkv"));

            foreach (var file in videos)
            {
                _logger.LogTrace("Parsing video file: {File}", file);
                await ProcessVideoAsync(file, shouldTakeAction);
            }
        }

        private async Task ProcessVideoAsync(string filePath, bool shouldTakeAction)
        {
            var folder = Path.GetDirectoryName(filePath) ?? throw new Exception($"{filePath} is invalid!");
            var baseFileInfo = await _commandService.RunCommandAsync("ffmpeg", $@"-i ""{filePath}""", folder);
            var fileInfo = new FileInfo(filePath);

            if (ShouldParseVideo(baseFileInfo.error, fileInfo))
            {
                var newFileName = GetNewFileName(fileInfo);
                if (shouldTakeAction)
                {
                    await ParseVideoAsync(filePath, newFileName, folder, gpu: _options.UseGpu, crf: _options.Crf);
                }
                else
                {
                    _logger.LogInformation("{FilePath} Running in dry run mode. Skip parsing...", filePath);
                }
            }
        }

        private bool ShouldParseVideo(string baseFileInfo, FileInfo fileInfo)
        {
            var largeEnough = fileInfo.Length > 20 * MbToBytes;
            var isNotHevc = !baseFileInfo.Contains("Video: hevc");
            var isNotMp4 = !fileInfo.Name.EndsWith(".mp4");
            var containsPrivacyInfo = baseFileInfo.Contains("creation_time");

            if (!largeEnough)
                _logger.LogInformation(
                    "{FileInfoFullName} should not be parsed, because it\'s too small: {FileInfoLength}MB. Minimum size is 20MB",
                    fileInfo.FullName, fileInfo.Length / MbToBytes);
            else if (isNotHevc)
                _logger.LogInformation("{FileInfoFullName} should be parsed, because it is not HEVC!",
                    fileInfo.FullName);
            else if (isNotMp4)
                _logger.LogInformation("{FileInfoFullName} should be parsed, because it is not mp4!",
                    fileInfo.FullName);
            else if (containsPrivacyInfo)
                _logger.LogInformation("{FileInfoFullName} should be parsed, because it contains privacy info!",
                    fileInfo.FullName);
            else
                _logger.LogInformation("{FileInfoFullName} should not be parsed, because it is compliant!",
                    fileInfo.FullName);

            return largeEnough && (isNotHevc || isNotMp4 || containsPrivacyInfo);
        }

        private string GetNewFileName(FileInfo fileInfo)
        {
            var bareName = Path.GetFileNameWithoutExtension(fileInfo.FullName);
            return $"{fileInfo.Directory}{Path.DirectorySeparatorChar}{bareName}_265.mp4";
        }

        private async Task ParseVideoAsync(string sourceFilePath, string targetFilePath, string folder, bool gpu,
            int crf)
        {
            _logger.LogWarning("{SourceFilePath} parsing initialized! crf is {Crf}", sourceFilePath, crf);

            if (File.Exists(targetFilePath))
            {
                File.Delete(targetFilePath);
            }

            int result; 
            string error;
            if (gpu)
            {
                (result, _, error) = await _commandService.RunCommandAsync("ffmpeg",
                    $@"-i ""{sourceFilePath}"" -preset slow -codec:a copy -codec:v hevc_nvenc -rc:v vbr -cq:v {crf} -rc-lookahead 10 -profile:v main10 ""{targetFilePath}""",
                    folder, timeout: TimeSpan.FromMinutes(200));
            }
            else
            {
                (result, _, error) = await _commandService.RunCommandAsync("ffmpeg",
                    $@"-i ""{sourceFilePath}"" -preset slow -codec:a copy -codec:v libx265    -crf {crf} ""{targetFilePath}""",
                    folder, timeout: TimeSpan.FromMinutes(200));
            }

            var sourceFileInfo = new FileInfo(sourceFilePath);
            var targetFileInfo = new FileInfo(targetFilePath);
            _logger.LogInformation(
                "New file size is {ConvertFileSizeToMb}, source file size is {SourceFileSizeToMb}. Saved {SavedFileSizeToMb}",
                ConvertFileSizeToMb(targetFileInfo.Length),
                ConvertFileSizeToMb(sourceFileInfo.Length),
                ConvertFileSizeToMb(sourceFileInfo.Length - targetFileInfo.Length));

            // ReSharper disable once MergeIntoPattern
            if (targetFileInfo.Exists && targetFileInfo.Length > 1 * MbToBytes && result == 0 && error.Contains("encoded"))
            {
                _logger.LogWarning("{TargetFilePath} parsed file exists. Deleting source file...", targetFilePath);
                File.Delete(sourceFilePath);
            }
            else
            {
                throw new Exception("After parsing, still couldn't locate the converted file: " + targetFilePath);
            }
        }

        private static string ConvertFileSizeToMb(long fileSize)
        {
            var sizeInMb = (double)fileSize / (1024 * 1024);
            return sizeInMb.ToString("0.##") + " MB";
        }
    }
}