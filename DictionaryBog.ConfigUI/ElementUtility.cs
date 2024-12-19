using System.Windows;
using System.Windows.Navigation;

namespace DictionaryBot.ConfigUI;

public static class ElementUtility
{
    public static void AddHandler(DependencyObject d, RoutedEvent e, RequestNavigateEventHandler handler)
    {
        if (d is UIElement element)
            element.AddHandler(e, handler);
        else if (d is ContentElement contentElement)
            contentElement.AddHandler(e, handler);
        else if (d is UIElement3D element3D)
            element3D.AddHandler(e, handler);
    }
    
    public static void RemoveHandler(DependencyObject d, RoutedEvent e, RequestNavigateEventHandler handler)
    {
        if (d is UIElement element)
            element.RemoveHandler(e, handler);
        else if (d is ContentElement contentElement)
            contentElement.RemoveHandler(e, handler);
        else if (d is UIElement3D element3D)
            element3D.RemoveHandler(e, handler);
    }
}