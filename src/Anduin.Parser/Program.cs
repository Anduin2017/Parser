using Anduin.Parser.Core.Framework;
using Anduin.Parser.FFmpeg;
using Aiursoft.CommandFramework;
using Aiursoft.CommandFramework.Extensions;

return await new AiursoftCommand()
    .Configure(command =>
    {
        command
            .AddGlobalOptions()
            .AddPlugins(new FFmpegPlugin());
    })
    .RunAsync(args);