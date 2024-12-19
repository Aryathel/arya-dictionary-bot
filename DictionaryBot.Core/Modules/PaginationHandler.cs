using DictionaryBot.Core.Models;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;

namespace DictionaryBot.Core.Modules;

/// <summary>
/// Used for handling paginated embeds in Discord with component buttons for navigation.
/// </summary>
/// <param name="pages">The embeds that make up the pages of the menu.</param>
/// <param name="id">The customer ID of the menu.</param>
/// <param name="isOwned">Whether to restrict control of the menu to only the original menu requester.</param>
/// <param name="showFirstLast">Whether to show the jump to first/last items on top of the always present next/previous buttons.</param>
/// <param name="deleteOnStop">Whether to delete the original message when the menu is stopped. Will only remove the component buttons if false.</param>
public class PaginationHandler(
    IEnumerable<EmbedBuilder> pages, 
    string? id = null, 
    bool isOwned = true, 
    bool showFirstLast = false, 
    bool deleteOnStop = false, 
    bool showQuit = false,
    PageNumberDisplayType pageNumber = PageNumberDisplayType.None,
    TimeSpan? timeout = null)
{
    private readonly List<EmbedBuilder> _pages = pages.ToList();
    private readonly bool _showFirstLast = showFirstLast;
    private readonly bool _isOwned = isOwned;
    private readonly bool _deleteOnStop = deleteOnStop;
    private readonly TimeSpan? _timeout = timeout;
    
    private readonly PageNumberDisplayType _pageNumber = pageNumber;
    private readonly bool _showQuit = showQuit;
    
    private ulong _ownerId;
    private SocketInteractionContext _context;

    private bool _active = false;
    private DateTime _startTime;
    private int _page = 0;

    public bool IsTimedOut => _active && _timeout.HasValue && _startTime + _timeout.Value < DateTime.UtcNow;
    private int _pageCount => _pages.Count;
    public bool IsPaginating => _pageCount > 1;
    
    // Discord limits IDs to 25 characters, and we add a 2 character prefix already.
    public readonly string Id = string.IsNullOrEmpty(id) ? Convert.ToBase64String(Guid.NewGuid().ToByteArray())[..22] : id[..23];
    private string NextButtonId => $"nx{Id}";
    private string PreviousButtonId => $"pr{Id}";
    private string FirstButtonId => $"fi{Id}";
    private string LastButtonId => $"lt{Id}";
    private string QuitButtonId => $"qt{Id}";

    /// <summary>
    /// Creates the component buttons to attach to the message.
    /// </summary>
    private MessageComponent? Components
    {
        get
        {
            if (!IsPaginating) return null;
            var builder = new ComponentBuilder();
            if (_showFirstLast)
                builder = builder.WithButton("<<<", FirstButtonId, ButtonStyle.Secondary, disabled: _page == 0);
            builder = builder.WithButton("<", PreviousButtonId, disabled: _page <= 0)
                .WithButton(">", NextButtonId, disabled: _page >= _pageCount - 1);
            if (_showFirstLast)
                builder = builder.WithButton(">>>", LastButtonId, ButtonStyle.Secondary, disabled: _page == _pageCount - 1);
            if (_showQuit)
                builder = builder.WithButton("Quit", QuitButtonId, ButtonStyle.Danger);
            return builder.Build();
        }
    }

    /// <summary>
    /// Gets the current page for display, adding the page number as necessary.
    /// </summary>
    /// <returns>The built page embed to display.</returns>
    private Embed BuildPage(bool respectPageNums = true)
    {
        var bld = _pages[_page];
        if (respectPageNums)
            switch (_pageNumber)
            {
                case PageNumberDisplayType.Author:
                    bld = bld.WithAuthor($"{_page + 1} out of {_pageCount}");
                    break;
                case PageNumberDisplayType.Footer:
                    bld = bld.WithFooter($"{_page + 1} out of {_pageCount}");
                    break;
                case PageNumberDisplayType.None:
                default:
                    break;
            }

        return bld.Build();
    }
    
    /// <summary>
    /// The callback for when a button is pressed.
    /// </summary>
    /// <param name="cmp">The socket message component event.</param>
    public async Task HandlePagination(SocketMessageComponent cmp)
    {
        // Handle owned menu restrictions
        if (_isOwned && !cmp.User.Id.Equals(_ownerId))
        {
            await cmp.RespondAsync(embed: new EmbedBuilder().WithDescription("Only the owner of this manu can use these buttons!").Build(), ephemeral: true);
            return;
        }
        
        // Handle pagination
        if (cmp.Data.CustomId.Equals(NextButtonId))
            await Next();
        else if (cmp.Data.CustomId.Equals(PreviousButtonId))
            await Previous();
        else if (_showFirstLast && cmp.Data.CustomId.Equals(FirstButtonId))
            await First();
        else if (_showFirstLast && cmp.Data.CustomId.Equals(LastButtonId))
            await Last();
        
        // Defer original response if pagination occurs.
        await cmp.DeferAsync();
    }
    
    /// <summary>
    /// Start the pagination manu, sending the root message and setting the context for the message.
    /// </summary>
    /// <param name="ctx">The Context of the original interaction.</param>
    public async Task Start(SocketInteractionContext ctx)
    {
        _context = ctx;
        if (_isOwned)
            _ownerId = _context.Interaction.User.Id;
        
        await _context.Interaction.RespondAsync(embed: BuildPage(), components: Components);
        _active = true;
        _startTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Stops an active pagination menu, deleting it or removing the buttons as necessary.
    /// </summary>
    public async Task Stop()
    {
        if (!_active) return;
        
        if (_deleteOnStop)
            await _context.Interaction.DeleteOriginalResponseAsync();
        else
            await _context.Interaction.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = BuildPage(false);
                msg.Components = null;
            });
        _active = false;
    }

    /// <summary>
    /// Internally used for updating the message when a pagination event occurs.
    /// </summary>
    private async Task Update()
    {
        await _context.Interaction.ModifyOriginalResponseAsync(msg =>
        {
            msg.Embed = BuildPage();
            msg.Components = Components;
        });
    }

    /// <summary>
    /// Attempts to more the menu to the next page.
    /// </summary>
    public async Task Next()
    {
        if (_page + 1 < _pageCount)
        {
            _page++;
            await Update();
        }
    }

    /// <summary>
    /// Attempts to move the menu to the previous page.
    /// </summary>
    public async Task Previous()
    {
        if (_page - 1 >= 0)
        {
            _page--;
            await Update();
        }
    }

    /// <summary>
    /// Attempts to move the menu to the first page.
    /// </summary>
    public async Task First()
    {
        if (_page != 0)
        {
            _page = 0;
            await Update();
        }
    }

    /// <summary>
    /// Attempts to move the menu to the last page.
    /// </summary>
    public async Task Last()
    {
        if (_page != _pageCount)
        {
            _page = _pageCount - 1;
            await Update();
        }
    }
}