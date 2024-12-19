using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DictionaryBot.ConfigUI.Resources.Models;

namespace DictionaryBot.ConfigUI;

public class MainWindowContext : INotifyPropertyChanged
{
    private bool _isLoading { get; set; }
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; NotifyPropertyChanged(); }
    }
    
    private string? _inviteLink { get; set; }

    public string InviteLink
    {
        get => _inviteLink ?? string.Empty;
        set { _inviteLink = value; NotifyPropertyChanged(); }
    }
    
    public ObservableCollection<GuildItem> Guilds { get; set; }

    private GuildItem? _selectedGuildItem;
    public GuildItem? SelectedGuildItem
    {
        get => _selectedGuildItem;
        set { _selectedGuildItem = value; NotifyPropertyChanged(); }
    }

    public MainWindowContext()
    {
        IsLoading = false;
        Guilds = new ObservableCollection<GuildItem>();
        Guilds.CollectionChanged += (_, _) => NotifyPropertyChanged();
    }
    
    /// <summary>
    /// Property Handler
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}