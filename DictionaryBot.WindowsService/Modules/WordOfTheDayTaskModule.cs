using System.ServiceModel.Syndication;
using DictionaryBot.Core.Models;
using DictionaryBot.Core.Modules;
using DictionaryBot.Core.Services;
using Discord;
using Discord.WebSocket;

namespace DictionaryBot.WindowsService.Modules;

public class WordOfTheDayTaskModule() : TaskModuleBase(TaskRunType.Other, ["DiscordChannelId", "LastPostId"])
{
    private RssReaderService _rssService;
    private EmbedService _embeds;
    private DiscordSocketClient _client;
    private const string RssFeedUrl = "https://www.merriam-webster.com/wotd/feed/rss2";

    public override async Task InitializeAsync(IServiceProvider services)
    {
        _rssService = services.GetRequiredService<RssReaderService>();
        _embeds = services.GetRequiredService<EmbedService>();
        _client = services.GetRequiredService<DiscordSocketClient>();
        
        await base.InitializeAsync(services);
    }
    
    public override async Task ExecuteAsync(Dictionary<string, object>? args = null)
    {
        if (args != null && args.TryGetValue("Post", out object? post))
        {
            var rssPost = (SyndicationItem?)post;
            ulong? channelId = Convert.ToUInt64(Config.Extras["DiscordChannelId"]?.ToString());
            if (channelId.HasValue && channelId.Value > 0 && rssPost != null)
            {
                var channel = (await _client.GetChannelAsync(channelId.Value)) as ITextChannel;
                if (channel != null)
                {
                    await channel.SendMessageAsync(embed: WotdToEmbed(rssPost));
                    await LogAsync($"Posted word of the day message to {channel.Name}");
                    _localStorage.Data.TaskModuleConfigs[Name].Extras["LastPostId"] = ((SyndicationItem?)post)?.Id;
                }
                else
                {
                    await _logger.LogAsync(LogSeverity.Warning, Name, "Could not find channel");
                }
            }
        }
    }
    
    public override async Task<(bool, Dictionary<string, object>?)> IsTaskReady()
    {
        if (Config.LastRunUTC != null && Config.LastRunUTC > DateTime.UtcNow - TimeSpan.FromHours(1))
            return (false, null);
        
        var mostRecentRss = _rssService.ReadRssMostRecent(RssFeedUrl);

        return (!mostRecentRss.Id.Equals(Config.Extras["LastPostId"]?.ToString()),
            new Dictionary<string, object>() { ["Post"] = mostRecentRss });
    }

    private Embed WotdToEmbed(SyndicationItem post)
    {
        var converter = new ReverseMarkdown.Converter(new ReverseMarkdown.Config
        {
            UnknownTags = ReverseMarkdown.Config.UnknownTagsOption.Bypass,
            RemoveComments = true,
            SmartHrefHandling = true,
        });
        var content = converter.Convert(post.Summary.Text.Replace("&#149;", "").Replace("// ", "> "));
        var contentSplit = content.Split("**")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();
        
        var embed = _embeds.WithDefault()
            .WithAuthor(new EmbedAuthorBuilder().WithName(contentSplit[0]))
            .WithTitle(post.Title.Text)
            .WithUrl(post.Links.FirstOrDefault()?.Uri.ToString())
            .WithDescription(contentSplit[2].Length <= 4096 ? contentSplit[2] : contentSplit[2].Substring(0, 4093) + "...")
            .WithFields([
                new EmbedFieldBuilder()
                    .WithName(contentSplit[3])
                    .WithValue(contentSplit[4].Length <= 1024 ? contentSplit[4] : contentSplit[4].Substring(0, 1021) + "..."),
                new EmbedFieldBuilder()
                    .WithName(contentSplit[5])
                    .WithValue(contentSplit[6].Length <= 1024 ? contentSplit[6] : contentSplit[6].Substring(0, 1021) + "..."),
            ]);
        
        return embed.Build();
    }
}