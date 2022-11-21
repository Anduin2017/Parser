using Aiursoft.Parser.Core;
using Aiursoft.Parser.FFmpeg.Services;

namespace Aiursoft.Parser.FFmpeg
{
    public class FFmpegEntry : IEntryService
    {
        private readonly FFmpegOptions _options;
        private readonly CommandService _commandService;

        public FFmpegEntry(
            FFmpegOptions options,
            CommandService commandService)
        {
            _options = options;
            _commandService = commandService;
        }

        public async Task OnServiceStartedAsync(string path, bool shouldTakeAction)
        {
            var videos = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(v =>
                    v.EndsWith(".webm") ||
                    v.EndsWith(".mp4") ||
                    v.EndsWith(".avi") ||
                    v.EndsWith(".wmv") ||
                    v.EndsWith(".mkv"));

            foreach (var file in videos)
            {
                await Parse(file, coder: _options.UseGPU ? "hevc_nvenc" : "libx265");
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
                Console.WriteLine($"{filePath} WILL be parsed!");

                File.Delete(newFileName);
                await _commandService.RunCommand("ffmpeg", $@"-i ""{filePath}"" -codec:a copy -codec:v {coder} -crf 20 ""{newFileName}""", folder, getOutput: false);

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
                Console.WriteLine($"{filePath} don't have to be parsed...");
            }
        }
    }
}
