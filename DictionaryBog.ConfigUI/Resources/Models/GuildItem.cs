using System.Windows.Media.Imaging;

namespace DictionaryBot.ConfigUI.Resources.Models;

public class GuildItem(BitmapImage image, string name, ulong id)
{
    public BitmapImage Image { get; set; } = image;
    public string Name { get; set; } = name;
    public ulong Id { get; set; } = id;
}