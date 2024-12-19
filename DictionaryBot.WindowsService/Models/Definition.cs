namespace DictionaryBot.WindowsService.Models;

public struct DefinitionEntry
{
    public string Definition { get; init; }
    public IEnumerable<string> Synonyms { get; init; }
    public IEnumerable<string> Antonyms { get; init; }
    public string? Example { get; init; }
}