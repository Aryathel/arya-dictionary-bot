namespace DictionaryBot.WindowsService.Models;

public struct Phonetic
{
    public string Text { get; init; }
    public string Audio { get; init; }
    public string SourceUrl { get; init; }
    public License License { get; init; }
}