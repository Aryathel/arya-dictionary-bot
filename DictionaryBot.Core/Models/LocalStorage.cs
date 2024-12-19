namespace DictionaryBot.Core.Models;

public class LocalStorage
{
    public string DiscordToken { get; set; } = "";
    public ulong DiscordGuildId { get; set; }

    public Dictionary<string, TaskModuleConfig> TaskModuleConfigs { get; set; } =
        new Dictionary<string, TaskModuleConfig>();
}