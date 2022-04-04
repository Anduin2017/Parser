using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Parser
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddLogging(logging => {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });

            services.AddSingleton<Entry>();
            services.AddTransient<CommandService>();

            var serviceProvider = services.BuildServiceProvider();
            var entry = serviceProvider.GetRequiredService<Entry>();

            await entry.StartEntry(args);
        }
    }
}