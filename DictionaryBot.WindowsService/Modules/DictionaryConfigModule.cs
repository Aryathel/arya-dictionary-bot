using DictionaryBot.Core.Services;
using Discord;
using Discord.Interactions;
using DictionaryBot.Core;
using Discord.WebSocket;

namespace DictionaryBot.WindowsService.Modules;

public class DictionaryConfigModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly LocalStorageService _localStorage;
    private readonly DiscordSocketClient _client;
    private readonly EmbedService _embeds;
    private readonly LogService _logger;
    
    public DictionaryConfigModule(LocalStorageService localStorage, DiscordSocketClient client, EmbedService embedService, LogService logger)
    {
        _localStorage = localStorage;
        _client = client;
        _embeds = embedService;
        _logger = logger;
    }

    [SlashCommand("set-wotd-channel", "Set the word of the day channel.")]
    public async Task SetWotdChannel([ChannelTypes(ChannelType.Text, ChannelType.News)][Summary(description: "The channel to post the word of the day in.")] IChannel channel)
    {
        var perms = (await (channel as IGuildChannel)?.GetUserAsync(_client.CurrentUser.Id))?.GetPermissions(channel as IGuildChannel);
        
        if (perms.HasValue && perms is { ViewChannel: true, SendMessages: true })
        {
            _localStorage.Data.TaskModuleConfigs["WordOfTheDayTaskModule"].Extras["DiscordChannelId"] = channel.Id;
            _localStorage.Save();
            var emb = _embeds.WithDefault()
                .WithDescription($"The Word of the Day will post daily in {(channel as ITextChannel)?.Mention}.");
            await RespondAsync(embed: emb.Build(), ephemeral: true);
        }
        else
        {
            var emb = _embeds.WithDefault()
                .WithDescription($"I am not able to post any messages in {(channel as ITextChannel)?.Mention}, please give me permission try again.");
            await RespondAsync(embed: emb.Build(), ephemeral: true);
        }
    }

    [SlashCommand("check-wotd-config", description: "Checks the current word of the day configuration.")]
    public async Task CheckWotdConfig()
    {
        ulong? channelId = Convert.ToUInt64(_localStorage.Data.TaskModuleConfigs["WordOfTheDayTaskModule"].Extras["DiscordChannelId"]?.ToString());
        ITextChannel? channel = null;
        if (channelId.HasValue && channelId.Value > 0)
            channel = (await _client.GetChannelAsync(channelId.Value) as ITextChannel);
        
        var emb = _embeds.WithDefault();
        if (channel == null)
            emb = emb.WithDescription("No channel configured for posting.");
        else
            emb = emb.WithDescription(
                $"Posting Word of the Day in {channel.Mention} daily.");
        
        await RespondAsync(embed: emb.Build(), ephemeral: true);
    }
}