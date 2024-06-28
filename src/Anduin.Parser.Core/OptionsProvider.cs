using System.CommandLine;
using Aiursoft.CommandFramework.Models;

namespace Anduin.Parser.Core;

public static class OptionsProvider
{
    private static Option[] GetGlobalOptions()
    {
        return
        [
            CommonOptionsProvider.PathOptions,
            CommonOptionsProvider.DryRunOption,
            CommonOptionsProvider.VerboseOption
        ];
    }

    public static Command AddGlobalOptions(this Command command)
    {
        var options = GetGlobalOptions();
        foreach (var option in options)
        {
            command.AddGlobalOption(option);
        }
        return command;
    }
}
