using Aiursoft.Parser.Core.Framework;
using System.CommandLine;

var description = "A cli tool project helps to re-encode and save all videos under a path.";

var program = new RootCommand(description)
    .AddGlobalOptions()
    .AddPlugins();

return await program.InvokeAsync(args);
