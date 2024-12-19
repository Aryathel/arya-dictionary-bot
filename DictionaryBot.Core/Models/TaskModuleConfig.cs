namespace DictionaryBot.Core.Models;

public class TaskModuleConfig
{
    public DateTime? LastRunUTC { get; set; } = null;
    public TimeOnly? RunTimeUTC { get; set; } = null;
    public TimeSpan? RunInterval { get; set; } = null;
    public Dictionary<string, object?> Extras { get; set; } = new Dictionary<string, object?>();
}