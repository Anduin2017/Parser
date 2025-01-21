using Aiursoft.CommandFramework.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace Anduin.Parser.Photos;

public class StartUp : IStartUp
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<CompressEntry>();
        services.AddTransient<ImageCompressor>();
    }
}