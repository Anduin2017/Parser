using Anduin.Parser.Core.Abstracts;
using Anduin.Parser.FFmpeg.Services;
using Microsoft.Extensions.Logging;

namespace Anduin.Parser.FFmpeg
{
    public class FFmpegEntry : IEntryService
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
            _logger.LogTrace("Enumerating files under path: " + path);
            var videos = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(v =>
                    v.EndsWith(".webm") ||
                    v.EndsWith(".mp4") ||
                    v.EndsWith(".avi") ||
                    v.EndsWith(".wmv") ||
                    v.EndsWith(".mkv"));

            foreach (var file in videos)
            {
                _logger.LogTrace("Parsing video file: " + file);
                await this.ProcessVideoAsync(file, shouldTakeAction);
            }
        }

        private async Task ProcessVideoAsync(string filePath, bool shouldTakeAction)
        {
            var folder = Path.GetDirectoryName(filePath) ?? throw new Exception($"{filePath} is invalid!");
            var baseFileInfo = await _commandService.RunCommandAsync("ffmpeg", $@"-i ""{filePath}""", folder);
            var fileInfo = new FileInfo(filePath);

            if (ShouldParseVideo(baseFileInfo, fileInfo))
            {
                var newFileName = GetNewFileName(fileInfo);

                _logger.LogInformation($"{filePath} should be parsed...");
                if (shouldTakeAction)
                {
                    await ParseVideoAsync(filePath, newFileName, folder, gpu: _options.UseGpu, crf: _options.Crf);
                }
                else
                {
                    _logger.LogInformation($"{filePath} Running in dry run mode. Skip parsing...");
                }
            }
        }

        private bool ShouldParseVideo(string baseFileInfo, FileInfo fileInfo)
        {
            var largeEnough = fileInfo.Length > 20 * MbToBytes;
            var isNotHevc = !baseFileInfo.Contains("Video: hevc");
            var isNotmp4 = !fileInfo.Name.EndsWith(".mp4");
            var containsPrivacyInfo = baseFileInfo.Contains("creation_time");

            if (!largeEnough)
                _logger.LogInformation($"Don't have to parse {fileInfo.FullName} because it's too small: {fileInfo.Length / MbToBytes}MB. Minimum size is 20MB.");
            else if (isNotHevc)
                _logger.LogInformation($"Parse {fileInfo.FullName} because it is not HEVC!");
            else if (isNotmp4)
                _logger.LogInformation($"Parse {fileInfo.FullName} because it is not mp4!");
            else if (containsPrivacyInfo)
                _logger.LogInformation($"Parse {fileInfo.FullName} because it contains privacy info!");
            else
                _logger.LogInformation($"{fileInfo.FullName} don't have to be parsed...");

            return largeEnough && (isNotHevc || isNotmp4 || containsPrivacyInfo);
        }

        private string GetNewFileName(FileInfo fileInfo)
        {
            var bareName = Path.GetFileNameWithoutExtension(fileInfo.FullName);
            return $"{fileInfo.Directory}{Path.DirectorySeparatorChar}{bareName}_265.mp4";
        }

        private async Task ParseVideoAsync(string sourceFilePath, string targetFilePath, string folder, bool gpu, int crf)
        {
            _logger.LogWarning($"{sourceFilePath} WILL be parsed! crf is {crf}");

            if (File.Exists(targetFilePath))
            {
                File.Delete(targetFilePath);
            }

            if (gpu)
            {
                await _commandService.RunCommandAsync("ffmpeg", $@"-i ""{sourceFilePath}"" -preset slow -codec:a copy -codec:v hevc_nvenc -rc:v vbr -cq:v {crf} -rc-lookahead 10 -profile:v main10 ""{targetFilePath}""", folder, getOutput: false);
            }
            else
            {
                await _commandService.RunCommandAsync("ffmpeg", $@"-i ""{sourceFilePath}"" -preset slow -codec:a copy -codec:v libx265    -crf {crf} ""{targetFilePath}""", folder, getOutput: false);
            }

            var targetFileInfo = new FileInfo(targetFilePath);
            if (targetFileInfo.Exists && targetFileInfo.Length > 8 * MbToBytes)
            {
                File.Delete(sourceFilePath);
            }
            else
            {
                throw new Exception("After parsing, still couldn't locate the converted file: " + targetFilePath);
            }
        }
    }
}
