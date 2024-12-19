using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DictionaryBot.ConfigUI.Resources.Models;
using DictionaryBot.Core;
using DictionaryBot.Core.Services;
using Discord;
using Discord.Net;
using Discord.Rest;
using Serilog;

namespace DictionaryBot.ConfigUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const string InviteTemplate = "https://discord.com/oauth2/authorize?client_id={0}&scope=bot&permissions=2147559488";
    
    private readonly LocalStorageService _localStorage;
    private readonly LogService _logService;
    
    private readonly AutoResetEvent _botFetchSignal = new AutoResetEvent(false);
    
    private string BotAvatar { get; set; }
    private string BotName { get; set; }
    private string BotDiscriminator { get; set; }
    private ulong BotClientId { get; set; }
    private IReadOnlyCollection<RestGuild> BotGuilds { get; set; }
    
    private ServiceController BotServiceController { get; set; }
    private bool serviceInstalled = false;

    private string? BotDisplayName => string.IsNullOrEmpty(BotName) ? null : string.IsNullOrEmpty(BotDiscriminator) ? BotName : $"{BotName}#{BotDiscriminator}";

    public MainWindow()
    {
        InitializeComponent();
        (DataContext as MainWindowContext).IsLoading = true;
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AryaCore", "DictionaryBot", "Logs", "log_ui_.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileTimeLimit: TimeSpan.FromDays(30))
            .CreateLogger();
        
        _logService = new LogService();
        _localStorage = new LocalStorageService();
        _localStorage.Load();
        
        DiscordToken.Text = _localStorage.Data.DiscordToken;
        _logService.Log("ConfigUI", "Loaded local storage.");

        BotDataRefresh();

        UpdateServiceStatus();
    }
    
    private void BotGuildsRefresh(object sender, EventArgs e)
    {
        _logService.Log("ConfigUI", "Refreshing bot guilds.");
        BotDataRefresh();
    }

    private void BotDataRefresh()
    {
        _logService.Log("ConfigUI", "Refreshing bot data.");
        (DataContext as MainWindowContext).IsLoading = true;
        Task.Run(LoadBotDataAsync)
            .ContinueWith(_ => BotPostLoad(), TaskScheduler.FromCurrentSynchronizationContext());
    }
    
    private async Task LoadBotDataAsync()
    {
        if (string.IsNullOrEmpty(_localStorage.Data.DiscordToken))
        {
            BotAvatar = null;
            BotName = null;
            BotDiscriminator = null;
            BotGuilds = null;
            return;
        };
        
        var socketConf = new DiscordRestConfig()
        {
            LogLevel = LogSeverity.Debug,
        };
        var client = new DiscordRestClient(socketConf);
        
        client.LoggedIn += (async () =>
        {
            BotAvatar = client.CurrentUser.GetDisplayAvatarUrl(ImageFormat.Png, 512);
            BotName = client.CurrentUser.Username;
            BotDiscriminator = client.CurrentUser.Discriminator;
            var info = await client.GetApplicationInfoAsync();
            BotClientId = info.Id;
            BotGuilds = await client.GetGuildsAsync();
            _botFetchSignal.Set();
        });

        try
        {
            await client.LoginAsync(TokenType.Bot, _localStorage.Data.DiscordToken);
            _botFetchSignal.WaitOne(5000);
            await client.LogoutAsync();

            var guilds = string.Join(", ", BotGuilds.Select(g => $"[{g.Name}]({g.IconUrl})"));
            await _logService.LogAsync("ConfigUI", $"Name: {BotDisplayName} - Avatar: {BotAvatar} - Guilds: {guilds}");
            await _logService.LogAsync("ConfigUI", "Done loading bot data.");
        }
        catch (HttpException ex)
        {
            _logService.Log(LogSeverity.Error, "ConfigUI", "Failed Discord login", ex);
            BotAvatar = null;
            BotName = null;
            BotDiscriminator = null;
            BotGuilds = null;
        }
    }
    
    private void BotPostLoad()
    {
        var ctx = (DataContext as MainWindowContext)!;
        
        // Bot Display Name
        if (BotDisplayName != null)
        {
            ctx.InviteLink = string.Format(InviteTemplate, BotClientId);
            BotNameDisplay.Text = BotDisplayName;
            BotNameDisplay.Foreground = (Brush)TryFindResource("Text");
            if (serviceInstalled)
            {
                ServerRow.IsEnabled = true;
                Notification.Text = null;
                Notification.Foreground = (Brush)TryFindResource("Text");
            }
            else ServerRow.IsEnabled = false;
        }
        else
        {
            BotNameDisplay.Text = string.IsNullOrEmpty(_localStorage.Data.DiscordToken) ? "No Token Provided" : "Invalid Token";
            BotNameDisplay.Foreground = (Brush)TryFindResource("Error");
            if (!string.IsNullOrEmpty(_localStorage.Data.DiscordToken))
            {
                Notification.Text = "Login with the given token failed. Please use a different token.";;
                Notification.Foreground = (Brush)TryFindResource("Error");
            }
            else
            {
                Notification.Text = null;
                Notification.Foreground = (Brush)TryFindResource("Text");
            }
            ctx.InviteLink = null;
            ServerRow.IsEnabled = false;
        }
        // Bot Avatar
        if (BotAvatar != null)
        {
            var bmpImg = new BitmapImage();
            bmpImg.BeginInit();
            bmpImg.UriSource = new Uri(BotAvatar);
            bmpImg.DecodePixelWidth = 64;
            bmpImg.EndInit();
            
            Avatar.ImageSource = bmpImg;
        }
        else Avatar.ImageSource = (ImageSource)TryFindResource("DefaultAvatar");

        ctx.Guilds.Clear();
        if (BotGuilds != null)
            foreach (var guild in BotGuilds)
            {
                var gImg = new BitmapImage();
                gImg.BeginInit();
                gImg.UriSource = string.IsNullOrEmpty(guild.IconUrl) ? new Uri("./Resources/Images/AryathelLogo.png", UriKind.Relative) : new Uri(guild.IconUrl);
                gImg.DecodePixelWidth = 128;
                gImg.EndInit();
                ctx.Guilds.Add(new GuildItem(gImg, guild.Name, guild.Id));
            }
        if (_localStorage.Data.DiscordGuildId != null && _localStorage.Data.DiscordGuildId > 0 && ctx.Guilds.Count > 0)
            ctx.SelectedGuildItem = ctx.Guilds.FirstOrDefault(g => g.Id == _localStorage.Data.DiscordGuildId, ctx.Guilds.First() ?? null);
        
        // Is Loading Toggle
        ctx.IsLoading = false;
    }

    private void UpdateServiceStatus()
    {
        try
        {
            if (BotServiceController == null) BotServiceController = new ServiceController("DictionaryBot.WindowsService");
            else BotServiceController.Refresh();
            
            var status = BotServiceController.Status;
            _logService.Log("ConfigUI", $"Service Status: {status.ToString()}");
            
            serviceInstalled = true;
            
            if (status.Equals(ServiceControllerStatus.Paused))
            {
                BotServiceController.Stop();
                BotServiceController.Refresh();
                status = BotServiceController.Status;
            }

            if (status.Equals(ServiceControllerStatus.StopPending) || status.Equals(ServiceControllerStatus.StartPending))
            {
                (DataContext as MainWindowContext).IsLoading = true;
                BotServiceController.WaitForStatus(status.Equals(ServiceControllerStatus.StopPending) ? ServiceControllerStatus.Stopped : ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
                (DataContext as MainWindowContext).IsLoading = false;
                BotServiceController.Refresh();
                status = BotServiceController.Status;
            }

            int btnType = -1;
            
            switch (BotServiceController.Status)
            {
                case ServiceControllerStatus.Running:
                    ServiceStatus.Text = status.ToString();
                    ServiceStatus.Foreground = (Brush)TryFindResource("Success");
                    DiscordTokenRow.IsEnabled = false;
                    ServerRow.IsEnabled = false;
                    btnType = 2;
                    break;
                case ServiceControllerStatus.Stopped:
                    ServiceStatus.Text = status.ToString();
                    ServiceStatus.Foreground = (Brush)TryFindResource("Error");
                    btnType = 1;
                    break;
                case ServiceControllerStatus.Paused:
                case ServiceControllerStatus.StartPending:
                case ServiceControllerStatus.StopPending:
                default:
                    ServiceStatus.Text = "Unknown";
                    ServiceStatus.Foreground = (Brush)TryFindResource("Text2");
                    break;
            }

            if (string.IsNullOrEmpty(_localStorage.Data.DiscordToken) || _localStorage.Data.DiscordGuildId <= 0)
                btnType = 0;
            
            switch (btnType)
            {
                case 2:
                    _logService.Log(LogSeverity.Debug, "ConfigUI", "Showing stop service button.");
                    ServiceControlButton.Text = "Stop Bot";
                    ServiceControlButtonBg.Background = (Brush)TryFindResource("Error");
                    ServiceControlButtonBg.IsEnabled = true;
                    DiscordTokenRow.IsEnabled = false;
                    ServerRow.IsEnabled = false;
                    break;
                case 1:
                    _logService.Log(LogSeverity.Debug, "ConfigUI", "Showing start service button.");
                    ServiceControlButton.Text = "Start Bot";
                    ServiceControlButtonBg.Background = (Brush)TryFindResource("Success");
                    ServiceControlButtonBg.IsEnabled = true;
                    DiscordTokenRow.IsEnabled = true;
                    ServerRow.IsEnabled = !string.IsNullOrEmpty(BotDisplayName);
                    break;
                case 0:
                    _logService.Log(LogSeverity.Debug, "ConfigUI", "Showing missing configs button.");
                    ServiceControlButton.Text = "Missing Settings";
                    ServiceControlButtonBg.Background = (Brush)TryFindResource("Errors");
                    ServiceControlButtonBg.IsEnabled = false;
                    DiscordTokenRow.IsEnabled = true;
                    ServerRow.IsEnabled = !string.IsNullOrEmpty(BotDisplayName);
                    break;
                default:
                    _logService.Log(LogSeverity.Debug, "ConfigUI", "Showing unknown button.");
                    ServiceControlButton.Text = "Unknown";
                    ServiceControlButtonBg.Background = (Brush)TryFindResource("Bg1");
                    DiscordTokenRow.IsEnabled = true;
                    ServerRow.IsEnabled = !string.IsNullOrEmpty(BotDisplayName);
                    break;
            }
        }
        catch (InvalidOperationException ex)
        {
            _logService.Log(LogSeverity.Error, "ConfigUI", "Service not found on machine.", ex);
            ServiceStatus.Text = $"Not Installed";
            ServiceStatus.Foreground = (Brush)TryFindResource("Error");
            Notification.Text = "Please uninstall and reinstall the application to install the service properly.";
            Notification.Foreground = (Brush)TryFindResource("Error");
        }
    }
    
    private void ServiceControl(object sender, EventArgs e)
    {
        BotServiceController.Refresh();
        try
        {
            switch (BotServiceController.Status)
            {
                case ServiceControllerStatus.Paused:
                case ServiceControllerStatus.Running:
                    _logService.Log("ConfigUI", "Stopping bot service.");
                    BotServiceController.Stop();
                    break;
                case ServiceControllerStatus.Stopped:
                    _logService.Log("ConfigUI", "Starting bot service.");
                    BotServiceController.Start();
                    break;
                case ServiceControllerStatus.StopPending:
                case ServiceControllerStatus.StartPending:
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            _logService.Log(LogSeverity.Error, "ConfigUI", "Failed to perform service operation.", ex);
            Notification.Text = "Failed to start/stop service, make sure you run the application as Administrator.";
            Notification.Foreground = (Brush)TryFindResource("Error");
        }
        UpdateServiceStatus();
    }
    
    private void UpdateBotToken(object sender, EventArgs e)
    {
        if (sender is not Hyperlink || DataContext is not MainWindowContext { IsLoading: false }) return;
        if (DiscordToken.Text.Trim() == _localStorage.Data.DiscordToken) return;
        
        _logService.Log("ConfigUI", "Updating bot token from UI button.");
        (DataContext as MainWindowContext).IsLoading = true;
        
        _localStorage.Data.DiscordToken = DiscordToken.Text.Trim();
        _localStorage.Save();

        BotDataRefresh();
    }

    private void UpdateBotGuild(object sender, EventArgs e)
    {
        var guild = (DataContext as MainWindowContext).SelectedGuildItem;
        _logService.LogAsync("ConfigUI", "Bot guild selected: " + guild.Name);
        
        _localStorage.Data.DiscordGuildId = guild.Id;
        _localStorage.Save();
        
        UpdateServiceStatus();
    }
    
    /// <summary>
    /// Property Handler
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
    private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
    {
        if (PropertyChanged != null)
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }
}