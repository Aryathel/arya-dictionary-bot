using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace DictionaryBot.ConfigUI;

public static class HyperlinkUtility
{
    public static readonly DependencyProperty LaunchDefaultBrowserProperty =
        DependencyProperty.RegisterAttached("LaunchDefaultBrowser", typeof(bool),
            typeof(HyperlinkUtility), new PropertyMetadata(false,
                HyperlinkUtility_LaunchDefaultBrowserChanged));

    public static bool GetLaunchDefaultBrowser(DependencyObject d)
    {
        return (bool) d.GetValue(LaunchDefaultBrowserProperty);
    }

    public static void SetLaunchDefaultBrowser(DependencyObject d, bool value)
    {
        d.SetValue(LaunchDefaultBrowserProperty, value);
    }

    private static void HyperlinkUtility_LaunchDefaultBrowserChanged(object
        sender, DependencyPropertyChangedEventArgs e)
    {
        var d = (DependencyObject) sender;
        if ((bool) e.NewValue)
            ElementUtility.AddHandler(d, Hyperlink.RequestNavigateEvent, Hyperlink_RequestNavigateEvent);
        else
            ElementUtility.RemoveHandler(d, Hyperlink.RequestNavigateEvent, Hyperlink_RequestNavigateEvent);
    }

    private static void Hyperlink_RequestNavigateEvent(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo() { FileName = e.Uri.AbsoluteUri, UseShellExecute = true });
        e.Handled = true;
    }
}