using Aiursoft.CommandFramework;
using Anduin.Parser.FFmpeg;
using Aiursoft.CommandFramework.Models;

return await new SingleCommandApp(new FFmpegHandler())
    .WithDefaultOption(CommonOptionsProvider.PathOptions)
    .RunAsync(args);