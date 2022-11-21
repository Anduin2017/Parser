using Aiursoft.Parser.Core.Framework;

namespace Aiursoft.Parser.Core.Abstracts;

public interface IParserPlugin
{
    public CommandHandler[] Install();
}
