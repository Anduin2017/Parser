using Aiursoft.CommandFramework.Abstracts;
using Aiursoft.CSTools.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Anduin.Parser.FFmpeg;

public class StartUp : IStartUp
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<FFmpegEntry>();
        services.AddTransient<CommandService>();
    }
}
