namespace KaguraModManager.Models;

public class ModConflict
{
    public string FilePath { get; set; } = "";
    public List<string> ModIds { get; set; } = new();
}
