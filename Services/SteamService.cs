using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using KaguraModManager.Models;
using Microsoft.Win32;

namespace KaguraModManager.Services;

public static class SteamService
{
    private static readonly List<Game> KnownGames = new()
    {
        new() { Id = "sk-sv", Name = "Senran Kagura Shinovi Versus", SteamAppId = "411830",
            FolderNames = new() { "Senran Kagura Shinovi Versus", "SENRAN KAGURA SHINOVI VERSUS" }},
        new() { Id = "sk-ev", Name = "Senran Kagura Estival Versus", SteamAppId = "502800",
            FolderNames = new() { "Senran Kagura Estival Versus", "SENRAN KAGURA ESTIVAL VERSUS" }},
        new() { Id = "sk-pbs", Name = "Senran Kagura Peach Beach Splash", SteamAppId = "696170",
            FolderNames = new() { "Senran Kagura Peach Beach Splash", "SENRAN KAGURA Peach Beach Splash" }},
        new() { Id = "sk-br", Name = "Senran Kagura Burst Re:Newal", SteamAppId = "889510",
            FolderNames = new() { "Senran Kagura Burst Re:Newal", "SENRAN KAGURA Burst Re:Newal",
                "Senran Kagura Burst ReNewal", "SENRAN KAGURA Burst ReNewal", "SENRAN KAGURA Burst Re Newal" }},
        new() { Id = "sk-ba", Name = "Senran Kagura Bon Appétit! - Full Course", SteamAppId = "514310",
            FolderNames = new() { "Senran Kagura Bon Appetit", "SENRAN KAGURA Bon Appetit",
                "Senran Kagura Bon Appétit! - Full Course" }},
        new() { Id = "sk-pb", Name = "Senran Kagura Peach Ball", SteamAppId = "1074080",
            FolderNames = new() { "Senran Kagura Peach Ball", "SENRAN KAGURA Peach Ball" }},
        new() { Id = "sk-ref", Name = "Senran Kagura Reflexions", SteamAppId = "907250",
            FolderNames = new() { "Senran Kagura Reflexions", "SENRAN KAGURA Reflexions" }},
    };

    public static List<Game> GetAllGames() => KnownGames;

    public static string GetSteamPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var path = key?.GetValue("SteamPath")?.ToString();
            if (!string.IsNullOrEmpty(path)) return path;
        }
        catch { }
        return @"C:\Program Files (x86)\Steam";
    }

    public static List<string> GetSteamLibraries()
    {
        var steamPath = GetSteamPath();
        var paths = new List<string> { Path.Combine(steamPath, "steamapps", "common") };

        var vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (File.Exists(vdfPath))
        {
            foreach (Match m in Regex.Matches(File.ReadAllText(vdfPath), @"""path""\s+""([^""]+)"""))
            {
                var libPath = m.Groups[1].Value.Replace("\\\\", "\\");
                var common = Path.Combine(libPath, "steamapps", "common");
                if (!paths.Contains(common)) paths.Add(common);
            }
        }
        return paths;
    }

    public static Dictionary<string, string> ScanForGames(AppSettings settings)
    {
        var libraries = GetSteamLibraries();
        foreach (var lib in libraries)
        {
            if (!Directory.Exists(lib)) continue;
            foreach (var game in KnownGames)
            {
                if (settings.GameDirs.ContainsKey(game.Id) &&
                    Directory.Exists(settings.GameDirs[game.Id])) continue;

                foreach (var folder in game.FolderNames)
                {
                    var fullPath = Path.Combine(lib, folder);
                    if (Directory.Exists(fullPath))
                    {
                        settings.GameDirs[game.Id] = fullPath;
                        break;
                    }
                }
            }
        }
        return settings.GameDirs;
    }

    public static void LaunchGame(string steamAppId)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = $"steam://rungameid/{steamAppId}",
            UseShellExecute = true
        });
    }
}
