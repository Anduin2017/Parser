using Aiursoft.Parser.Core.Framework;

namespace Aiursoft.Parser.Core;

public interface IParserPlugin
{
    public CommandHandler[] Install();
}
