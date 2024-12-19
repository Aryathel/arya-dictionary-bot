using System.Reflection;
using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DictionaryBot.Core.Services;

public class InteractionHandler
{
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _configuration;
    private readonly InteractionService _interactionHandler;
    private readonly IServiceProvider _services;
    private readonly LogService _logger;
    private readonly LocalStorageService _localStorage;
    private readonly EmbedService _embeds;
    
    private readonly PaginationService? _pagination;
    
    public InteractionHandler(DiscordSocketClient client, InteractionService interactions, IServiceProvider services, IConfiguration config, LogService logger, LocalStorageService localStorage, EmbedService embedService)
    {
        _client = client;
        _interactionHandler = interactions;
        _services = services;
        _configuration = config;
        _logger = logger;
        _localStorage = localStorage;
        _embeds = embedService;
        
        _pagination = services.GetRequiredService<PaginationService>() ?? null;
    }

    public async Task Initialize()
    {
        _client.Ready += Ready;
        _interactionHandler.Log += _logger.LogAsync;
        
        await _interactionHandler.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        _client.InteractionCreated += HandleInteraction;

        _interactionHandler.InteractionExecuted += HandleInteractionExecute;
    }

    private async Task Ready()
    {
        if (_localStorage.Data is { DiscordGuildId: > 0 })
        {
            await _interactionHandler.RegisterCommandsToGuildAsync(_localStorage.Data.DiscordGuildId);
            await _logger.LogAsync("InteractionHandler",
                $"Updated guild commands for guild ID: {_localStorage.Data.DiscordGuildId}");
        }
        else
        {
            var ex = new Exception("No guild ID is provided, new commands/command updates will not be registered.");
            await _logger.LogAsync(LogSeverity.Warning, "InteractionHandler", "No guild ID is provided, new commands/command updates will not be registered", ex);
        }
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        if (interaction.Type != InteractionType.ApplicationCommand)
            return;
        
        try
        {
            var context = new SocketInteractionContext(_client, interaction);

            var result = await _interactionHandler.ExecuteCommandAsync(context, _services);

            if (!result.IsSuccess)
            {
                string? message = null;
                switch (result.Error)
                {
                    case InteractionCommandError.UnknownCommand:
                        await _logger.LogAsync(LogSeverity.Warning, "InteractionHandler", "Unknown command.");
                        break;
                    case InteractionCommandError.ConvertFailed:
                        message =
                            "I'm sorry, I seem to have failed to convert your parameter into something I can use. Please try again.";
                        await _logger.LogAsync(LogSeverity.Warning, "InteractionHandler", "Failed to convert parameter.",
                            new Exception(result.ErrorReason));
                        break;
                    case InteractionCommandError.BadArgs:
                        message = "I'm sorry, your arguments appear to be invalid. Please try again.";
                        await _logger.LogAsync(LogSeverity.Warning, "InteractionHandler", "Bad arguments were received.",
                            new Exception(result.ErrorReason));
                        break;
                    case InteractionCommandError.Exception:
                        message = "An error occurred processing your request.";
                        await _logger.LogAsync(new LogMessage(LogSeverity.Error, "InteractionHandler",
                            "An exception occurred during command processing.", new Exception(result.ErrorReason)));
                        break;
                    case InteractionCommandError.Unsuccessful:
                        message = "The command was unsuccessful. Please try again.";
                        await _logger.LogAsync(LogSeverity.Error, "InteractionHandler", "The command was unsuccessful.",
                            new Exception(result.ErrorReason));
                        break;
                    case InteractionCommandError.UnmetPrecondition:
                        message = "Looks like you don't meet the conditions to use this command.";
                        break;
                    case InteractionCommandError.ParseFailed:
                        await _logger.LogAsync(LogSeverity.Error, "InteractionHandler", "Failed to parse command context.",
                            new Exception(result.ErrorReason));
                        break;
                    default:
                        message = "Something went wrong.";
                        await _logger.LogAsync(new LogMessage(LogSeverity.Error, "InteractionHandler",
                            "An unhandled error occurred during command processing.",
                            new Exception(result.ErrorReason)));
                        break;
                }

                if (message != null)
                {
                    var emb = new EmbedBuilder().WithColor(new Color(255, 0, 0)).WithDescription(message);
                    await interaction.RespondAsync(embed: emb.Build(), ephemeral: true);
                }
                else
                {
                    await interaction.DeferAsync();
                }
            }
        }
        catch
        {
            if (interaction.Type == InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
        }
    }

    private async Task HandleInteractionExecute(ICommandInfo commandInfo, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
        {
            string? message = null;
            switch (result.Error)
            {
                case InteractionCommandError.UnknownCommand:
                    await _logger.LogAsync(LogSeverity.Warning, "InteractionHandler", "Unknown command.");
                    break;
                case InteractionCommandError.ConvertFailed:
                    message =
                        "I'm sorry, I seem to have failed to convert your parameter into something I can use. Please try again.";
                    await _logger.LogAsync(LogSeverity.Warning, "InteractionHandler", "Failed to convert parameter.",
                        new Exception(result.ErrorReason));
                    break;
                case InteractionCommandError.BadArgs:
                    message = "I'm sorry, your arguments appear to be invalid. Please try again.";
                    await _logger.LogAsync(LogSeverity.Warning, "InteractionHandler", "Bad arguments were received.",
                        new Exception(result.ErrorReason));
                    break;
                case InteractionCommandError.Exception:
                    message = "An error occurred processing your request.";
                    await _logger.LogAsync(new LogMessage(LogSeverity.Error, "InteractionHandler",
                        "An exception occurred during command processing.", new Exception(result.ErrorReason)));
                    break;
                case InteractionCommandError.Unsuccessful:
                    message = "The command was unsuccessful. Please try again.";
                    await _logger.LogAsync(LogSeverity.Error, "InteractionHandler", "The command was unsuccessful.",
                        new Exception(result.ErrorReason));
                    break;
                case InteractionCommandError.UnmetPrecondition:
                    message = "Looks like you don't meet the conditions to use this command.";
                    break;
                case InteractionCommandError.ParseFailed:
                    await _logger.LogAsync(LogSeverity.Error, "InteractionHandler", "Failed to parse command context.",
                        new Exception(result.ErrorReason));
                    break;
                default:
                    message = "Something went wrong.";
                    await _logger.LogAsync(new LogMessage(LogSeverity.Error, "InteractionHandler",
                        "An unhandled error occurred during command processing.",
                        new Exception(result.ErrorReason)));
                    break;
            }

            if (message != null)
            {
                var emb = new EmbedBuilder().WithColor(new Color(255, 0, 0)).WithDescription(message);
                await context.Interaction.RespondAsync(embed: emb.Build(), ephemeral: true);
            }
            else
            {
                await context.Interaction.DeferAsync();
            }
        }
    }
}