using Microsoft.Extensions.DependencyInjection;

namespace Anduin.Parser.Core.Abstracts;

public interface IStartUp
{
    public void ConfigureServices(IServiceCollection services);
}
