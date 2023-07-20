using Aiursoft.CommandFramework.Abstracts;
using Aiursoft.CommandFramework.Framework;

namespace Anduin.Parser.FFmpeg;

public class FFmpegPlugin : IPlugin
{
    public CommandHandler[] Install() => new CommandHandler[] { new FFmpegHandler() };
}
