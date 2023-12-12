using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Invocation;
using Aiursoft.CommandFramework.Framework;
using Aiursoft.CommandFramework.Models;
using Aiursoft.CommandFramework.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Anduin.Parser.FFmpeg;

public class FFmpegHandler : ExecutableCommandHandlerBuilder
{
    private readonly Option<bool> _useGpu = new(
        getDefaultValue: () => false,
        aliases: new[] { "--gpu", "-g" },
        description: "Use NVIDIA GPU to speed up parsing. Only if you have an NVIDIA GPU attached.");

    private readonly Option<int> _crf = new(
        getDefaultValue: () => 20,
        aliases: new[] { "--crf", "-c" },
        description: "The range of the CRF scale is 0–51, where 0 is loss-less (for 8 bit only, for 10 bit use -qp 0), 20 is the default, and 51 is worst quality possible.");

    protected override string Name => "ffmpeg";

    protected override string Description => "The command to convert all video files to HEVC using FFmpeg.";

    protected override Task Execute(InvocationContext context)
    {
        var verbose = context.ParseResult.GetValueForOption(CommonOptionsProvider.VerboseOption);
        var dryRun = context.ParseResult.GetValueForOption(CommonOptionsProvider.DryRunOption);
        var path = context.ParseResult.GetValueForOption(CommonOptionsProvider.PathOptions)!;
        var useGpu = context.ParseResult.GetValueForOption(_useGpu);
        var crf = context.ParseResult.GetValueForOption(_crf);
        var hostBuilder = ServiceBuilder.CreateCommandHostBuilder<StartUp>(verbose);
        hostBuilder.ConfigureServices(services => 
        {
            services.AddSingleton(new FFmpegOptions { UseGpu = useGpu, Crf = crf });
        });
        var serviceProvider = hostBuilder.Build().Services;
        var logger = serviceProvider.GetRequiredService<ILogger<FFmpegHandler>>();
        var entry = serviceProvider.GetRequiredService<FFmpegEntry>();
        var fullPath = Path.GetFullPath(path);
        logger.LogTrace("Starting service: FFmpeg Entry. Full path is: {FullPath}, Dry run is: {DryRun}", fullPath, dryRun);
        return entry.OnServiceStartedAsync(fullPath, shouldTakeAction: !dryRun);
    }

    protected override Option[] GetCommandOptions() => new Option[]
    {
        CommonOptionsProvider.PathOptions,
        CommonOptionsProvider.VerboseOption,
        CommonOptionsProvider.DryRunOption,
        _useGpu,
        _crf
    };
}
