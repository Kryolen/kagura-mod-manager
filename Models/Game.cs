using System.Windows.Media.Imaging;

namespace KaguraModManager.Models;

public class Game
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string SteamAppId { get; set; } = "";
    public List<string> FolderNames { get; set; } = new();
    public BitmapSource? IconBitmap { get; set; }
}
