using DictionaryBot.Core.Models;
using Discord;

namespace DictionaryBot.Core.Services;

public class EmbedService
{
    private readonly EmbedServiceConfig _config;
    
    public EmbedService(EmbedServiceConfig config)
    {
        _config = config;
    }

    public EmbedBuilder WithDefault()
    {
        return new EmbedBuilder()
            .WithColor(_config.DefaultColor ?? Color.Default);
    }
    
    public EmbedBuilder WithError() => new EmbedBuilder().WithColor(Color.Red);
}