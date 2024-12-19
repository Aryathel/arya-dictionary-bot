using DictionaryBot.Core;
using DictionaryBot.Core.Models;
using DictionaryBot.Core.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Serilog;

namespace DictionaryBot.WindowsService;

public class Program
{
    static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AryaCore", "DictionaryBot", "Logs", "log_.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileTimeLimit: TimeSpan.FromDays(30))
            .CreateLogger();
        var socketConf = new DiscordSocketConfig()
        {
            LogLevel = LogSeverity.Info,
            GatewayIntents = GatewayIntents.AllUnprivileged,
        };

        var interactionConf = new InteractionServiceConfig()
        {
            LogLevel = LogSeverity.Debug,
        };

        var embedConf = new EmbedServiceConfig()
        {
            DefaultColor = new Color(255, 255, 255),
        };
        
        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddWindowsService(options =>
                {
                    options.ServiceName = "DictionaryBot";
                });
                
                services.AddSingleton<RssReaderService>()
                    .AddSingleton<LocalStorageService>()
                    .AddSingleton<LogService>()
                    .AddSingleton(embedConf)
                    .AddSingleton<EmbedService>()
                    .AddSingleton<PaginationService>();
                
                services.AddSingleton(socketConf)
                    .AddSingleton<DiscordSocketClient>()
                    .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>(), interactionConf))
                    .AddSingleton<InteractionHandler>();

                services.AddHostedService<DiscordHost>()
                    .AddHostedService<TimerHost>();
            })
            .Build();
        
        await host.RunAsync();
    }
}