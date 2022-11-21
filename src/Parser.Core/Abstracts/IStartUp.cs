

using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Parser.Core;

public interface IStartUp
{
    public void ConfigureServices(IServiceCollection services);
}
