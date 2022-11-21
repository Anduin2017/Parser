using Aiursoft.Parser.Abstracts;
using Aiursoft.Parser.Core.Framework;

namespace Aiursoft.Parser.FFmpeg;

public class FFmpegPlugin : IParserPlugin
{
    public CommandHandler[] Install() => new CommandHandler[] { new FFmpegHandler() };
}
