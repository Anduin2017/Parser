using Anduin.Parser.FFmpeg;
using Aiursoft.CommandFramework.Models;

return await new FFmpegHandler().RunAsync(args, defaultOption: CommonOptionsProvider.PathOptions);
