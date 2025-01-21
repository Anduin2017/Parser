using System.CommandLine;
using System.CommandLine.Invocation;
using Aiursoft.CommandFramework.Framework;
using Aiursoft.CommandFramework.Models;
using Aiursoft.CommandFramework.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Anduin.Parser.Photos;

public class CompressPhotosHandler : ExecutableCommandHandlerBuilder
{
    private readonly Option<double> _scale = new(
        getDefaultValue: () => 0.5,
        aliases: ["--scale", "-s"],
        description: "The range of the scale is 0-1, where 0 is empty, 1 is the original size. Default is 0.5.");

    private readonly Option<int> _onlyIfPhotoLargerThanMb = new(
        getDefaultValue: () => 10,
        aliases: ["--filter-only-if-larger-than", "-f"],
        description: "Only compress photos larger than this size in MB. Default is 10.");
    
    private readonly Option<bool> _deleteOriginal = new(
        getDefaultValue: () => false,
        aliases: ["--delete", "-del"],
        description: "Delete the original photo after compressing.");
    
    private readonly Option<string[]> _fileExtensions = new(
        getDefaultValue: () => new[] { ".jpg", ".jpeg", ".png", ".bmp" },
        aliases: ["--extensions", "-e"],
        description: "The file extensions to compress. Default is '.jpg', '.jpeg', '.png', '.bmp'.");
    
    protected override string Name => "compress-photos";

    protected override string Description => "The command to compress all photos to a smaller size.";

    protected override Task Execute(InvocationContext context)
    {
        var verbose = context.ParseResult.GetValueForOption(CommonOptionsProvider.VerboseOption);
        var dryRun = context.ParseResult.GetValueForOption(CommonOptionsProvider.DryRunOption);
        var path = context.ParseResult.GetValueForOption(CommonOptionsProvider.PathOptions)!;
        var scale = context.ParseResult.GetValueForOption(_scale);
        var onlyIfPhotoLargerThanMb = context.ParseResult.GetValueForOption(_onlyIfPhotoLargerThanMb);
        var deleteOriginal = context.ParseResult.GetValueForOption(_deleteOriginal);
        var extensions = context.ParseResult.GetValueForOption(_fileExtensions)!;
        
        var hostBuilder = ServiceBuilder.CreateCommandHostBuilder<StartUp>(verbose);
        var serviceProvider = hostBuilder.Build().Services;
        var logger = serviceProvider.GetRequiredService<ILogger<CompressPhotosHandler>>();
        var entry = serviceProvider.GetRequiredService<CompressEntry>();
        var fullPath = Path.GetFullPath(path);
        logger.LogTrace("Starting service: Compress Entry. Full path is: {FullPath}, Dry run is: {DryRun}, Scale is: {Scale}, OnlyIfPhotoLargerThanMb is: {OnlyIfPhotoLargerThanMb}, DeleteOriginal is: {DeleteOriginal}, Extensions are: {Extensions}", fullPath, dryRun, scale, onlyIfPhotoLargerThanMb, deleteOriginal, extensions);
        return entry.OnServiceStartedAsync(fullPath, shouldTakeAction: !dryRun, scale: scale, onlyIfPhotoLargerThanMb: onlyIfPhotoLargerThanMb, deleteOriginal: deleteOriginal, extensions: extensions);
    }

    protected override Option[] GetCommandOptions() =>
    [
        CommonOptionsProvider.PathOptions,
        CommonOptionsProvider.VerboseOption,
        CommonOptionsProvider.DryRunOption,
        _scale,
        _onlyIfPhotoLargerThanMb,
        _deleteOriginal,
        _fileExtensions
    ];
}