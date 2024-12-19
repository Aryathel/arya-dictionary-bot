namespace DictionaryBot.WindowsService.Models;

public struct DefinitionResponseEntry
{
    public string Word { get; init; }
    public string Phonetic { get; init; }
    public IEnumerable<Phonetic> Phonetics { get; init; }
    public List<Meaning> Meanings { get; init; }
    public License License { get; init; }
    public IEnumerable<string> SourceUrls { get; init; }
}