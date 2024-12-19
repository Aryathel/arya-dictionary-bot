using DictionaryBot.Core.Modules;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace DictionaryBot.Core.Services;

public class PaginationService
{
    private readonly LogService _logger;
    private DiscordSocketClient _client;
    
    private Dictionary<string, PaginationHandler> _paginationHandlers = new Dictionary<string, PaginationHandler>();

    public PaginationService(LogService logger, DiscordSocketClient client)
    {
        _logger = logger;
        _client = client;

        _client.ButtonExecuted += HandleButtonExecuted;
    }

    public async Task Start(PaginationHandler handler, SocketInteractionContext context)
    {
        await RegisterHandler(handler);
        await handler.Start(context);
    }
    
    public async Task RegisterHandler(PaginationHandler handler)
    {
        // Only register the handler if it actually needs to be used.
        if (!handler.IsPaginating) return;
        
        if (_paginationHandlers.ContainsKey(handler.Id))
            await Dispose(handler.Id);
        _paginationHandlers.Add(handler.Id, handler);
    }

    public async Task Dispose(string id)
    {
        if (_paginationHandlers.ContainsKey(id))
        {
            await _paginationHandlers[id].Stop();
            _paginationHandlers.Remove(id);
        }
    }

    private async Task Dispose(IEnumerable<string> handlerIds)
    {
        foreach (var id in handlerIds)
            await Dispose(id);
    }

    private async Task HandleButtonExecuted(SocketMessageComponent component)
    {
        await _logger.DebugAsync("PaginationService", $"Button press received: {component.Data.CustomId}");
        
        // Dispose of timed out pagination handlers
        await Dispose(_paginationHandlers.Where(x => x.Value.IsTimedOut).Select(x => x.Key));
        
        var exists = _paginationHandlers.TryGetValue(component.Data.CustomId[2..], out var handler);
        if (!exists) return;
        
        if (component.Data.CustomId.StartsWith("qt"))
            await Dispose(handler!.Id);
        else
            await handler!.HandlePagination(component).ConfigureAwait(false);
    }
}