using Aiursoft.CommandFramework;
using Anduin.Parser.FFmpeg;
using Aiursoft.CommandFramework.Models;

return await new SingleCommandApp<FFmpegHandler>()
    .WithDefaultOption(CommonOptionsProvider.PathOptions)
    .RunAsync(args);