using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Parser.Core.Abstracts;

public interface IStartUp
{
    public void ConfigureServices(IServiceCollection services);
}
