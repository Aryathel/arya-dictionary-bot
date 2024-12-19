using DictionaryBot.Core;
using DictionaryBot.Core.Services;
using Discord;
using Discord.WebSocket;

namespace DictionaryBot.WindowsService;

public class DiscordHost : BackgroundService
{
    private readonly LogService _logger;
    private readonly DiscordSocketClient _client;
    private readonly InteractionHandler _interactionHandler;
    private readonly LocalStorageService _localStorage;

    public DiscordHost(LogService logger, DiscordSocketClient socketClient, InteractionHandler interactionHandler, LocalStorageService localStorageService)
    {
        _logger = logger;
        _client = socketClient;
        _interactionHandler = interactionHandler;
        _localStorage = localStorageService;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _client.Log += _logger.LogAsync;
            _client.Ready += Ready;

            await Initialize();
            await _interactionHandler.Initialize();
            
            await _client.LoginAsync(TokenType.Bot, _localStorage.Data?.DiscordToken);
            await _client.StartAsync();
            
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }

    private async Task Initialize()
    {
        _localStorage.Load();
        // Re-save immediately to update stored object as needed.
        _localStorage.Save();
        
        if (string.IsNullOrEmpty(_localStorage.Data.DiscordToken))
        {
            var ex = new ApplicationException("Discord token is missing from config.");
            await _logger.LogAsync(new LogMessage(LogSeverity.Critical, "DiscordHost", "Failed to load discord token from local configuration file. Please create one before running the bot.", ex));
            throw ex;
        }
    }

    private async Task Ready()
    {
        await _logger.LogAsync(new LogMessage(LogSeverity.Info, "DiscordHost", $"Connected to discord: {_client.CurrentUser.Username}#{_client.CurrentUser.Discriminator} (ID: {_client.CurrentUser.Id})"));
    }
}