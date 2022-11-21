using Aiursoft.Parser.Core;
using Aiursoft.Parser.FFmpeg.Services;
using Microsoft.Extensions.Logging;

namespace Aiursoft.Parser.FFmpeg
{
    public class FFmpegEntry : IEntryService
    {
        private readonly ILogger<FFmpegEntry> _logger;
        private readonly FFmpegOptions _options;
        private readonly CommandService _commandService;

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
                await Parse(file, coder: _options.UseGpu ? "hevc_nvenc" : "libx265");
            }
        }

        private async Task Parse(string filePath, string coder)
        {
            var folder = Path.GetDirectoryName(filePath) ?? throw new Exception($"{filePath} is invalid!");
            var baseFileInfo = await _commandService.RunCommand("ffmpeg", $@"-i ""{filePath}""", folder);
            var fileInfo = new FileInfo(filePath);
            var shouldParse =
                fileInfo.Length > 20 * 1024 * 1024 && // 20MB
                !baseFileInfo.Contains("Video: hevc") ||  // Not HEVC
                baseFileInfo.Contains("creation_time") || // Or contains privacy info
                !filePath.EndsWith(".mp4"); // Or not MP4
            var bareName = Path.GetFileNameWithoutExtension(filePath);
            var newFileName = $"{fileInfo.Directory}{Path.DirectorySeparatorChar}{bareName}_265.mp4";
            if (shouldParse)
            {
                _logger.LogWarning($"{filePath} WILL be parsed, with codec: {coder}, crf is {_options.Crf}");

                File.Delete(newFileName);
                await _commandService.RunCommand("ffmpeg", $@"-i ""{filePath}"" -codec:a copy -codec:v {coder} -crf {_options.Crf} ""{newFileName}""", folder, getOutput: false);

                if (File.Exists(newFileName))
                {
                    // Delete old file.
                    File.Delete(filePath);
                }
                else
                {
                    throw new Exception("After parsing, still couldn't locate the converted file: " + newFileName);
                }
            }
            else
            {
                _logger.LogInformation($"{filePath} don't have to be parsed...");
            }
        }
    }
}
