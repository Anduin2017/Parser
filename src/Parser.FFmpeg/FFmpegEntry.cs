using Aiursoft.Parser.Core.Abstracts;
using Aiursoft.Parser.FFmpeg.Services;
using Microsoft.Extensions.Logging;

namespace Aiursoft.Parser.FFmpeg
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
                await this.ProcessVideoAsync(file);
            }
        }

        private async Task ProcessVideoAsync(string filePath)
        {
            var folder = Path.GetDirectoryName(filePath) ?? throw new Exception($"{filePath} is invalid!");
            var baseFileInfo = await _commandService.RunCommandAsync("ffmpeg", $@"-i ""{filePath}""", folder);
            var fileInfo = new FileInfo(filePath);

            if (ShouldParseVideo(baseFileInfo, fileInfo))
            {
                var newFileName = GetNewFileName(fileInfo);
                await ParseVideoAsync(filePath, newFileName, folder, coder: _options.UseGpu ? "hevc_nvenc" : "libx265", crf: _options.Crf);
            }
            else
            {
                _logger.LogInformation($"{filePath} don't have to be parsed...");
            }
        }

        private bool ShouldParseVideo(string baseFileInfo, FileInfo fileInfo)
        {
            return fileInfo.Length > 20 * MbToBytes &&
                   (!baseFileInfo.Contains("Video: hevc") || !fileInfo.Name.EndsWith(".mp4") || baseFileInfo.Contains("creation_time"));
        }

        private string GetNewFileName(FileInfo fileInfo)
        {
            var bareName = Path.GetFileNameWithoutExtension(fileInfo.FullName);
            return $"{fileInfo.Directory}{Path.DirectorySeparatorChar}{bareName}_265.mp4";
        }

        private async Task ParseVideoAsync(string sourceFilePath, string targetFilePath, string folder, string coder, int crf)
        {
            _logger.LogWarning($"{sourceFilePath} WILL be parsed, with codec: {coder}, crf is {crf}");

            if (File.Exists(targetFilePath))
            {
                File.Delete(targetFilePath);
            }

            await _commandService.RunCommandAsync("ffmpeg", $@"-i ""{sourceFilePath}"" -preset slower -codec:a copy -codec:v {coder} -crf {crf} ""{targetFilePath}""", folder, getOutput: false);

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
