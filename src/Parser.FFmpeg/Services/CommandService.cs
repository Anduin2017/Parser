using System.Diagnostics;

namespace Anduin.Parser.FFmpeg.Services
{
    public class CommandService
    {
        public async Task<string> RunCommandAsync(string bin, string arg, string path, bool getOutput = true)
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = bin,
                    Arguments = arg,
                    WorkingDirectory = path,
                    UseShellExecute = !getOutput,
                    RedirectStandardOutput = getOutput,
                    RedirectStandardError = getOutput,
                }
            };
            p.Start();
            await p.WaitForExitAsync();
            if (getOutput)
            {
                var output = await p.StandardOutput.ReadToEndAsync();
                var error = await p.StandardError.ReadToEndAsync();
                return !string.IsNullOrWhiteSpace(output) ? output : error;
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
