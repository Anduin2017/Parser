using Anduin.Parser.Core.Abstracts;
using Anduin.Parser.Core.Framework;

namespace Anduin.Parser.FFmpeg;

public class FFmpegPlugin : IParserPlugin
{
    public CommandHandler[] Install() => new CommandHandler[] { new FFmpegHandler() };
}
