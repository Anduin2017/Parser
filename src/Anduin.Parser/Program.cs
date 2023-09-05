using Anduin.Parser.Core.Framework;
using Anduin.Parser.FFmpeg;
using System.CommandLine;
using System.Reflection;
using Aiursoft.CommandFramework.Extensions;

var descriptionAttribute = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;

var program = new RootCommand(descriptionAttribute ?? "Unknown usage.")
    .AddGlobalOptions()
    .AddPlugins(
        new FFmpegPlugin()
    );

return await program.InvokeAsync(args);
