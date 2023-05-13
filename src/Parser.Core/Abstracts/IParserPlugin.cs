using Anduin.Parser.Core.Framework;

namespace Anduin.Parser.Core.Abstracts;

public interface IParserPlugin
{
    public CommandHandler[] Install();
}
