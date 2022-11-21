using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Parser.Abstracts;

public interface IStartUp
{
    public void ConfigureServices(IServiceCollection services);
}
