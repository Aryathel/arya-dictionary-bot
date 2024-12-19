namespace DictionaryBot.WindowsService.Models;

public struct Meaning
{
    public string PartOfSpeech { get; init; }
    public List<DefinitionEntry> Definitions { get; init; }
    public List<string> Synonyms { get; init; }
    public List<string> Antonyms { get; init; }
}