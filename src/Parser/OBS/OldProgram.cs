using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Parser;

namespace Aiursoft.Parser.OBS
{
    public static class OldProgram
    {
        public static async Task MainObs(string[] args)
        {
            var services = new ServiceCollection();
            services.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });

            services.AddSingleton<OldEntry>();
            services.AddTransient<CommandService>();

            var serviceProvider = services.BuildServiceProvider();
            var entry = serviceProvider.GetRequiredService<OldEntry>();

            await entry.StartEntry(args);
        }
    }
}