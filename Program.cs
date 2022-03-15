using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Parser
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            if(args.Length < 1)
            {
                Console.WriteLine("Usage: WorkingPath");
                Console.WriteLine("Current parameters:");
                foreach(var arg in args)
                {
                    Console.WriteLine(arg);
                }
                return;
            }

            var workingPath = args[0];
            var videos = Directory.EnumerateFiles(workingPath, "*.*", SearchOption.AllDirectories)
                .Where(v =>
                    v.EndsWith(".mp4") ||
                    v.EndsWith(".avi") ||
                    v.EndsWith(".wmv") ||
                    v.EndsWith(".mkv"));

            foreach (var file in videos)
            {
                await Parse(file);
            }
        }

        private static async Task Parse(string filePath)
        {
            var folder = Path.GetDirectoryName(filePath) ?? throw new Exception($"{filePath} is invalid!");
            var baseFileInfo = await RunCommand("ffmpeg.exe", $@"-i ""{filePath}""", folder);
            var shouldParse = baseFileInfo.Contains("creation_time");
            if (shouldParse)
            {
                Console.WriteLine($"{filePath} will be parsed...\n\n");
                var fileInfo = new FileInfo(filePath);
                var bareName = Path.GetFileNameWithoutExtension(filePath);
                var newFileName = $"{fileInfo.Directory}{Path.DirectorySeparatorChar}{bareName}_parsed.mp4";

                File.Delete(newFileName);
                await RunCommand("ffmpeg", $@"-i ""{filePath}"" ""{newFileName}""", folder, redirectError: false);

                // Delete old file.
                File.Delete(filePath);
            }
            else
            {
                Console.WriteLine($"{filePath} don't have to be parsed...\n\n");
            }
        }

        private static async Task<string> RunCommand(string bin, string arg, string path, bool redirectError = true)
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = bin,
                    Arguments = arg,
                    WorkingDirectory = path,
                    UseShellExecute = !redirectError,
                    RedirectStandardOutput = redirectError,
                    RedirectStandardError = redirectError,
                }
            };
            p.Start();
            p.WaitForExit();
            var output = await p.StandardOutput.ReadToEndAsync();
            if (redirectError)
            {
                var error = await p.StandardError.ReadToEndAsync();
                return !string.IsNullOrWhiteSpace(output) ? output : error;
            }
            else
            {
                return output;
            }
        }
    }
}