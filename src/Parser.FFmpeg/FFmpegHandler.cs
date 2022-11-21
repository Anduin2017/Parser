using Aiursoft.Parser.Core.Framework;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace Aiursoft.Parser.FFmpeg;

public class FFmpegHandler : ServiceCommandHandler<FFmpegEntry, StartUp>
{
    private Option<bool> UseGPU = new Option<bool>(
        aliases: new[] { "--gpu", "-g" },
        description: "Show NVIDIA GPU to speed up parsing. Only if you have an NVIDIA GPU attached.");

    public override string Name => "parser";

    public override string Description => "The command to convert all video files to HEVC using FFmpeg.";

    public override Option[] GetOptions()
    {
        return new Option[]
        {
            UseGPU
        };
    }

    public override void OnCommandBuilt(Command command)
    {
        command.SetHandler(
            ExecuteOverride,
            OptionsProvider.PathOptions,
            OptionsProvider.DryRunOption,
            OptionsProvider.VerboseOption,
            UseGPU);
    }

    public Task ExecuteOverride(string path, bool dryRun, bool verbose, bool useGPU)
    {
        var services = BuildServices(verbose);
        services.AddSingleton(new FFmpegOptions { UseGPU = useGPU });
        return RunFromServices(services, path, dryRun);
    }
}
