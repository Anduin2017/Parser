using Aiursoft.Parser.Core.Framework;

namespace Aiursoft.Parser.Abstracts;

public interface IParserPlugin
{
    public CommandHandler[] Install();
}
