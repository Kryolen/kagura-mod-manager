namespace KaguraModManager.Models;

public class Mod
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "Unknown";
    public string Version { get; set; } = "1.0";
    public string Description { get; set; } = "";
    public bool IsEnabled { get; set; }
    public bool IsFavorite { get; set; }
    public string FullPath { get; set; } = "";
}
