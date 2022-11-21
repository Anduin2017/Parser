

using Aiursoft.Parser.Core.Models.Framework;

namespace Aiursoft.Parser.Core;

public interface INinjaPlugin
{
    public CommandHandler[] Install();
}
