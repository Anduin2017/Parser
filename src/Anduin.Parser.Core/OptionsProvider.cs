﻿using System.CommandLine;
using Aiursoft.CommandFramework.Models;

namespace Anduin.Parser.Core.Framework;

public static class OptionsProvider
{
    private static Option[] GetGlobalOptions()
    {
        return new Option[]
        {
            CommonOptionsProvider.PathOptions,
            CommonOptionsProvider.DryRunOption,
            CommonOptionsProvider.VerboseOption,
        };
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
