using System.Net;
using System.Text;
using DictionaryBot.Core.Models;
using DictionaryBot.Core.Modules;
using DictionaryBot.Core.Services;
using DictionaryBot.WindowsService.Models;
using Discord;
using Discord.Interactions;
using Newtonsoft.Json;

namespace DictionaryBot.WindowsService.Modules;

public class DictionaryModule(EmbedService embedService, LogService logService, PaginationService paginationService) : InteractionModuleBase<SocketInteractionContext>
{
    private const string apiTemplate = "https://api.dictionaryapi.dev/api/v2/entries/en/{0}";

    private readonly EmbedService _embedService = embedService;
    private readonly LogService _logger = logService;
    private readonly PaginationService _paginationService = paginationService;

    [SlashCommand("define", description: "Get a dictionary definition for a word.")]
    public async Task DefinitionLookup([Summary(description: "The word to look up.")] string word)
    {
        word = word.Trim().Split(" ").First();

        try
        {
            List<DefinitionResponseEntry> definitions;
            using (var http = new HttpClient())
            {
                var req = await http.GetAsync(string.Format(apiTemplate, word));
                req.EnsureSuccessStatusCode();
                string response = await req.Content.ReadAsStringAsync();
                definitions = JsonConvert.DeserializeObject<List<DefinitionResponseEntry>>(response);
                definitions = CoalesceDefinitions(definitions).ToList();
            }

            var embeds = definitions?.Select(DefinitionToEmbedBuilder).ToList();
            MessageComponent? components = null;
            if (embeds != null && embeds?.Count > 0)
            {
                var paginator = new PaginationHandler(
                    embeds, 
                    showFirstLast: embeds.Count > 2,
                    pageNumber: PageNumberDisplayType.Footer,
                    deleteOnStop: true,
                    timeout: TimeSpan.FromMinutes(5)
                    );
                await _paginationService.Start(paginator, Context);
            }
            else
                await RespondAsync(
                    embed: _embedService.WithDefault().WithDescription($"No definitions found for `{word}`.").Build(),
                    ephemeral: true);
        }
        catch (HttpRequestException ex) when (ex.StatusCode.Equals(HttpStatusCode.NotFound))
        {
            await RespondAsync(
                embed: _embedService.WithDefault().WithDescription($"No definitions found for `{word}`.").Build(),
                ephemeral: true);
        }
        catch (Exception ex)
        {
            await _logger.LogAsync(LogSeverity.Error, "DictionaryModule", $"Error getting dictionary definition for {word}.", ex);
            var emb = _embedService.WithError().WithDescription($"Failed to get dictionary definitions for `{word}`.");
            await RespondAsync(embed: emb.Build(), ephemeral: true);
        }
    }


    private IEnumerable<DefinitionResponseEntry> CoalesceDefinitions(IEnumerable<DefinitionResponseEntry> definitions)
    {
        var defs = new Dictionary<string, DefinitionResponseEntry>();

        foreach (var def in definitions)
        {
            foreach (var meaning in def.Meanings)
            {
                if (!defs.ContainsKey(meaning.PartOfSpeech))
                    defs.Add(
                        meaning.PartOfSpeech, 
                        new DefinitionResponseEntry
                        {
                            Word = def.Word,
                            Phonetic = def.Phonetic,
                            SourceUrls = def.SourceUrls,
                            Meanings = [meaning],
                        });
                else
                {
                    defs[meaning.PartOfSpeech].Meanings[0].Definitions.AddRange(meaning.Definitions);
                    defs[meaning.PartOfSpeech].Meanings[0].Synonyms.AddRange(meaning.Synonyms);
                    defs[meaning.PartOfSpeech].Meanings[0].Antonyms.AddRange(meaning.Antonyms);
                }
            }
        }
        
        return defs.Values;
    }

    private EmbedBuilder DefinitionToEmbedBuilder(DefinitionResponseEntry entry)
    {
        var desc = new StringBuilder()
            .AppendLine($"**{entry.Phonetic}**")
            .AppendLine()
            .AppendLine($"*{entry.Meanings[0].PartOfSpeech}*");

        var ln = 1;
        foreach (var def in entry.Meanings[0].Definitions)
        {
            desc = desc.AppendLine($"{ln}. {def.Definition}");
            if (!string.IsNullOrEmpty(def.Example))
                desc = desc.AppendLine($"  - E.g. *{def.Example}*");
        }
        
        return _embedService.WithDefault()
            .WithTitle(entry.Word)
            .WithUrl(entry.SourceUrls.FirstOrDefault())
            .WithDescription(desc.ToString());
    }
}