using System.Reflection;
using DictionaryBot.Core.Modules;
using DictionaryBot.Core.Services;
using Discord;
using Discord.WebSocket;

namespace DictionaryBot.WindowsService;

public class TimerHost : BackgroundService
{
    private List<TaskModuleBase> tasks;
    private readonly IServiceProvider _services;
    private readonly LogService _logger;
    private readonly DiscordSocketClient _client;

    private const int SleepTimeSeconds = 15;

    private bool _initialized = false;
    
    public TimerHost(IServiceProvider services, LogService logger, DiscordSocketClient client)
    { 
        _services = services;
        _logger = logger;
        _client = client;
        tasks = new List<TaskModuleBase>();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.Ready += InitializeAsync;
        
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_initialized || tasks.Count == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(SleepTimeSeconds), stoppingToken);
                continue;
            }
            
            await _logger.LogAsync(LogSeverity.Debug, "TimerHost", "Checking hosted tasks modules.");

            foreach (var task in tasks)
            {
                try
                {
                    (bool ready, Dictionary<string, object>? args) = await task.IsTaskReady();

                    if (!ready) continue;

                    await task.PreExecuteAsync(args);
                    await task.ExecuteAsync(args);
                    await task.PostExecuteAsync(args);
                }
                catch (Exception ex)
                {
                    await _logger.LogAsync(LogSeverity.Error, "TimerHost", $"An error occurred when executing task module {task.Name}.", ex);
                }
            }
            
            await Task.Delay(TimeSpan.FromSeconds(SleepTimeSeconds), stoppingToken);
        }
    }
    
    private async Task InitializeAsync()
    {
        var assembly = Assembly.GetEntryAssembly()!;

        foreach (var type in assembly.DefinedTypes)
        {
            if (IsTaskModule(type)
                && Activator.CreateInstance(type) is TaskModuleBase instance)
            {
                await instance.InitializeAsync(_services);
                tasks.Add(instance);
            }
        }
        await _logger.LogAsync("TimerHost", $"Loaded {tasks.Count} task modules.");
        _initialized = true;
    }
    
    private static bool IsTaskModule(TypeInfo type)
    {
        return (type.IsPublic || type.IsNestedPublic)
               && typeof(TaskModuleBase).IsAssignableFrom(type)
               && type is { IsAbstract: false, ContainsGenericParameters: false };
    }
}