using System.CommandLine;
using Aiursoft.CommandFramework.Framework;
using Aiursoft.CommandFramework.Models;
using Aiursoft.CommandFramework.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Anduin.Parser.Photos;

public class CompressPhotosHandler : ExecutableCommandHandlerBuilder
{
    private readonly Option<double> _scale = new(
        name: "--scale",
        aliases: ["-s"])
    {
        DefaultValueFactory = _ => 0.5,
        Description = "The range of the scale is 0-1, where 0 is empty, 1 is the original size. Default is 0.5."
    };

    private readonly Option<int> _onlyIfPhotoLargerThanMb = new(
        name: "--filter-only-if-larger-than",
        aliases: ["-f"])
    {
        DefaultValueFactory = _ => 10,
        Description = "Only compress photos larger than this size in MB. Default is 10."
    };

    private readonly Option<bool> _deleteOriginal = new(
        name: "--delete",
        aliases: ["-del"])
    {
        DefaultValueFactory = _ => false,
        Description = "Delete the original photo after compressing."
    };

    private readonly Option<bool> _keepOriginalName = new(
        name: "--keep-name",
        aliases: ["-kn"])
    {
        DefaultValueFactory = _ => false,
        Description = "After deleting the original photo, rename the compressed photo to the original name."
    };

    private readonly Option<string[]> _fileExtensions = new(
        name: "--extensions",
        aliases: ["-e"])
    {
        DefaultValueFactory = _ => new[] { ".jpg", ".jpeg", ".png", ".bmp" },
        Description = "The file extensions to compress. Default is '.jpg', '.jpeg', '.png', '.bmp'."
    };

    protected override string Name => "compress-photos";

    protected override string Description => "The command to compress all photos to a smaller size.";

    protected override Task Execute(ParseResult context)
    {
        var verbose = context.GetValue(CommonOptionsProvider.VerboseOption);
        var dryRun = context.GetValue(CommonOptionsProvider.DryRunOption);
        var path = context.GetValue(CommonOptionsProvider.PathOptions)!;
        var scale = context.GetValue(_scale);
        var onlyIfPhotoLargerThanMb = context.GetValue(_onlyIfPhotoLargerThanMb);
        var deleteOriginal = context.GetValue(_deleteOriginal);
        var keepOriginalName = context.GetValue(_keepOriginalName);
        var extensions = context.GetValue(_fileExtensions)!;

        var hostBuilder = ServiceBuilder.CreateCommandHostBuilder<StartUp>(verbose);
        var serviceProvider = hostBuilder.Build().Services;
        var logger = serviceProvider.GetRequiredService<ILogger<CompressPhotosHandler>>();
        var entry = serviceProvider.GetRequiredService<CompressEntry>();
        var fullPath = Path.GetFullPath(path);
        logger.LogTrace("Starting service: Compress Entry. Full path is: {FullPath}, Dry run is: {DryRun}, Scale is: {Scale}, OnlyIfPhotoLargerThanMb is: {OnlyIfPhotoLargerThanMb}, DeleteOriginal is: {DeleteOriginal}, Extensions are: {Extensions}", fullPath, dryRun, scale, onlyIfPhotoLargerThanMb, deleteOriginal, extensions);
        return entry.OnServiceStartedAsync(
            fullPath,
            shouldTakeAction: !dryRun,
            scale: scale,
            onlyIfPhotoLargerThanMb: onlyIfPhotoLargerThanMb,
            deleteOriginal: deleteOriginal,
            useOriginalName: keepOriginalName,
            extensions: extensions);
    }

    protected override Option[] GetCommandOptions() =>
    [
        CommonOptionsProvider.PathOptions,
        CommonOptionsProvider.VerboseOption,
        CommonOptionsProvider.DryRunOption,
        _scale,
        _onlyIfPhotoLargerThanMb,
        _deleteOriginal,
        _keepOriginalName,
        _fileExtensions
    ];
}
