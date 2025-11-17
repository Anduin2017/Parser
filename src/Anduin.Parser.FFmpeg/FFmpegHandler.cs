using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Aiursoft.CommandFramework.Framework;
using Aiursoft.CommandFramework.Models;
using Aiursoft.CommandFramework.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Anduin.Parser.FFmpeg;

public enum DuplicateAction
{
    Nothing,
    Delete,
    MoveToTrash
}

public class FFmpegHandler : ExecutableCommandHandlerBuilder
{
    private readonly Option<bool> _useGpu = new(
        name: "--gpu",
        aliases: ["-g"])
    {
        DefaultValueFactory = _ => false,
        Description = "Use NVIDIA GPU to speed up parsing. Only if you have an NVIDIA GPU attached."
    };

    private readonly Option<int> _crf = new(
        name: "--crf",
        aliases: ["-c"])
    {
        DefaultValueFactory = _ => 20,
        Description = "The range of the CRF scale is 0–51, where 0 is loss-less (for 8 bit only, for 10 bit use -qp 0), 20 is the default, and 51 is worst quality possible."
    };

    private readonly Option<DuplicateAction> _actionOption = new(
        name: "--action",
        aliases: ["-a"])
    {
        DefaultValueFactory = _ => DuplicateAction.MoveToTrash,
        Description = "Action to take when files are parsed. Available options: Nothing, Delete, MoveToTrash."
    };

    protected override string Name => "ffmpeg";

    protected override string Description => "The command to convert all video files to HEVC using FFmpeg.";

    protected override Task Execute(ParseResult context)
    {
        var verbose = context.GetValue(CommonOptionsProvider.VerboseOption);
        var dryRun = context.GetValue(CommonOptionsProvider.DryRunOption);
        var path = context.GetValue(CommonOptionsProvider.PathOptions)!;
        var useGpu = context.GetValue(_useGpu);
        var crf = context.GetValue(_crf);
        var action = context.GetValue(_actionOption);
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
        return entry.OnServiceStartedAsync(fullPath, shouldTakeAction: !dryRun, action: action);
    }

    protected override Option[] GetCommandOptions() =>
    [
        CommonOptionsProvider.PathOptions,
        CommonOptionsProvider.VerboseOption,
        CommonOptionsProvider.DryRunOption,
        _useGpu,
        _crf,
        _actionOption
    ];
}
