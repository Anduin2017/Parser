using Aiursoft.CommandFramework;
using Anduin.Parser.FFmpeg;
using Aiursoft.CommandFramework.Models;
using Anduin.Parser.Photos;

return await new NestedCommandApp()
    .WithGlobalOptions(CommonOptionsProvider.PathOptions)
    .WithGlobalOptions(CommonOptionsProvider.DryRunOption)
    .WithGlobalOptions(CommonOptionsProvider.VerboseOption)
    .WithFeature(new FFmpegHandler())
    .WithFeature(new CompressPhotosHandler())
    .RunAsync(args);
