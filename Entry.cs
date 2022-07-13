using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Parser
{
    public class Entry
    {
        private readonly CommandService _commandService;
        private readonly ILogger<Entry> logger;

        public Entry(
            CommandService commandService,
            ILogger<Entry> logger)
        {
            _commandService = commandService;
            this.logger = logger;
        }

        public async Task StartEntry(string[] args)
        {
            Console.WriteLine("Starting parser...");

            if (args.Length < 1)
            {
                Console.WriteLine("Usage: WorkingPath");
                Console.WriteLine("Current parameters:");
                foreach (var arg in args)
                {
                    Console.WriteLine(arg);
                }
                return;
            }

            var workingPath = args[0];
            var videos = Directory.EnumerateFiles(workingPath, "*.*", SearchOption.AllDirectories)
                .Where(v =>
                    v.EndsWith(".webm") ||
                    v.EndsWith(".mp4") ||
                    v.EndsWith(".avi") ||
                    v.EndsWith(".wmv") ||
                    v.EndsWith(".mkv"));

            foreach (var file in videos)
            {
                await Parse(file);
            }
        }

        private async Task Parse(string filePath)
        {
            var folder = Path.GetDirectoryName(filePath) ?? throw new Exception($"{filePath} is invalid!");
            var baseFileInfo = await _commandService.RunCommand("ffmpeg.exe", $@"-i ""{filePath}""", folder);
            var shouldParse = 
                !baseFileInfo.Contains("Video: hevc") ||  // Not HEVC
                baseFileInfo.Contains("creation_time") || // Or contains privacy info
                !filePath.EndsWith(".mp4"); // Or not MP4
            var fileInfo = new FileInfo(filePath);
            var bareName = Path.GetFileNameWithoutExtension(filePath);
            var newFileName = $"{fileInfo.Directory}{Path.DirectorySeparatorChar}{bareName}_265.mp4";
            if (shouldParse)
            {
                Console.WriteLine($"{filePath} WILL be parsed!");

                File.Delete(newFileName);
                await _commandService.RunCommand("ffmpeg", $@"-i ""{filePath}"" -codec:a copy -codec:v hevc_nvenc -b:v 16M ""{newFileName}""", folder, getOutput: false);

                // Delete old file.
                File.Delete(filePath);
            }
            else
            {
                Console.WriteLine($"{filePath} don't have to be parsed...");
            }
        }
    }
}
