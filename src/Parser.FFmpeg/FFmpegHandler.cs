using Aiursoft.Parser.Core.Framework;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace Aiursoft.Parser.FFmpeg;

public class FFmpegHandler : ServiceCommandHandler<FFmpegEntry, StartUp>
{
    private Option<bool> _useGpu = new Option<bool>(
        getDefaultValue: () => false,
        aliases: new[] { "--gpu", "-g" },
        description: "Use NVIDIA GPU to speed up parsing. Only if you have an NVIDIA GPU attached.");

    private Option<int> _crf = new Option<int>(
        getDefaultValue: () => 20,
        aliases: new[] { "--crf", "-c" },
        description: "The range of the CRF scale is 0–51, where 0 is lossless (for 8 bit only, for 10 bit use -qp 0), 20 is the default, and 51 is worst quality possible.");

    public override string Name => "ffmpeg";

    public override string Description => "The command to convert all video files to HEVC using FFmpeg.";

    public override Option[] GetOptions()
    {
        return new Option[]
        {
            _useGpu,
            _crf
        };
    }

    public override void OnCommandBuilt(Command command)
    {
        command.SetHandler(
            ExecuteOverride,
            OptionsProvider.PathOptions,
            OptionsProvider.DryRunOption,
            OptionsProvider.VerboseOption,
            _useGpu,
            _crf);
    }

    public Task ExecuteOverride(string path, bool dryRun, bool verbose, bool useGpu, int crf)
    {
        var services = BuildServices(verbose);
        services.AddSingleton(new FFmpegOptions { UseGpu = useGpu, Crf = crf });
        return RunFromServices(services, path, dryRun);
    }
}
