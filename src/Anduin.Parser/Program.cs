using Anduin.Parser.FFmpeg;
using Aiursoft.CommandFramework;
using Aiursoft.CommandFramework.Extensions;
using Aiursoft.CommandFramework.Models;

var command = new FFmpegHandler().BuildAsCommand();

return await new AiursoftCommandApp(command)
    .RunAsync(args.WithDefaultTo(CommonOptionsProvider.PathOptions));