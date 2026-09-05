using System.IO;
using System.Text.Json;
using KaguraModManager.Models;

namespace KaguraModManager.Services;

public static class FileService
{
    private static readonly string SettingsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "kagura-mod-manager", "settings.json");

    private static readonly string BackupsBase =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "kagura-mod-manager", "Backups");

    public static readonly string MetadataFiles = "mod.ini,mod.json,preview.png,readme.md,readme.txt";

    public static AppSettings LoadSettings()
    {
        if (!File.Exists(SettingsPath)) return new AppSettings();
        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void SaveSettings(AppSettings settings)
    {
        var dir = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static string GetBackupsDir(string gameId, string gameDir)
    {
        var newDir = Path.Combine(BackupsBase, gameId);
        var legacyDir = Path.Combine(gameDir, "ModManagerBackups");

        if (!Directory.Exists(newDir))
        {
            if (Directory.Exists(legacyDir))
            {
                try { CopyDirectory(legacyDir, newDir); Directory.Delete(legacyDir, true); }
                catch { return legacyDir; }
            }
            else
            {
                Directory.CreateDirectory(newDir);
            }
        }
        return newDir;
    }

    public static List<Mod> ReadMods(string gameDir)
    {
        var mods = new List<Mod>();
        var modsDir = Path.Combine(gameDir, "mods");
        if (!Directory.Exists(modsDir))
        {
            try { Directory.CreateDirectory(modsDir); } catch { return mods; }
            return mods;
        }

        var state = LoadModState(gameDir);
        var gameId = new DirectoryInfo(gameDir).Name;
        var favorites = LoadFavorites(gameId);
        var entries = Directory.GetDirectories(modsDir);

        foreach (var entry in entries)
        {
            var dirName = Path.GetFileName(entry);
            var iniPath = Path.Combine(entry, "mod.ini");
            var jsonPath = Path.Combine(entry, "mod.json");

            string title = dirName, author = "Unknown", version = "1.0", description = "";

            if (File.Exists(iniPath))
            {
                var ini = File.ReadAllText(iniPath);
                title = ExtractIniValue(ini, "Title") ?? dirName;
                author = ExtractIniValue(ini, "Author") ?? "Unknown";
                version = ExtractIniValue(ini, "Version") ?? "1.0";
                description = ExtractIniValue(ini, "Description") ?? "";
            }
            else if (File.Exists(jsonPath))
            {
                try
                {
                    var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(jsonPath));
                    if (json != null)
                    {
                        var nameVal = json.TryGetValue("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : null;
                        var titleVal = json.TryGetValue("title", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
                        title = nameVal ?? titleVal ?? dirName;
                        if (json.TryGetValue("author", out var a) && a.ValueKind == JsonValueKind.String)
                            author = a.GetString() ?? "Unknown";
                        if (json.TryGetValue("version", out var v) && v.ValueKind == JsonValueKind.String)
                            version = v.GetString() ?? "1.0";
                        if (json.TryGetValue("description", out var d) && d.ValueKind == JsonValueKind.String)
                            description = d.GetString() ?? "";
                    }
                }
                catch { }
            }

            mods.Add(new Mod
            {
                Id = dirName,
                Title = title,
                Author = author,
                Version = version,
                Description = description,
                IsEnabled = state.Contains(dirName),
                IsFavorite = favorites.Contains(dirName),
                FullPath = entry,
            });
        }

        if (File.Exists(Path.Combine(modsDir, ".modorder")))
        {
            var order = File.ReadAllLines(Path.Combine(modsDir, ".modorder"))
                .Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            mods.Sort((a, b) =>
            {
                int ia = order.IndexOf(a.Id), ib = order.IndexOf(b.Id);
                return (ia == -1 ? int.MaxValue : ia) - (ib == -1 ? int.MaxValue : ib);
            });
        }

        return mods;
    }

    public static void SaveModOrder(string gameDir, List<string> modIds)
    {
        var modsDir = Path.Combine(gameDir, "mods");
        Directory.CreateDirectory(modsDir);
        File.WriteAllLines(Path.Combine(modsDir, ".modorder"), modIds);
    }

    public static void SaveModState(string gameDir, List<string> appliedMods)
    {
        var backupsDir = GetBackupsDir(
            new DirectoryInfo(gameDir).Name, gameDir);
        Directory.CreateDirectory(backupsDir);
        var statePath = Path.Combine(backupsDir, "state.json");
        File.WriteAllText(statePath, JsonSerializer.Serialize(new { appliedMods },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    public static List<string> LoadModState(string gameDir)
    {
        var backupsDir = GetBackupsDir(
            new DirectoryInfo(gameDir).Name, gameDir);
        var statePath = Path.Combine(backupsDir, "state.json");
        if (!File.Exists(statePath)) return new List<string>();
        try
        {
            var doc = JsonDocument.Parse(File.ReadAllText(statePath));
            if (doc.RootElement.TryGetProperty("appliedMods", out var arr))
                return arr.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => s != "").ToList();
        }
        catch { }
        return new List<string>();
    }

    public static void SaveFavorites(string gameId, List<string> favoriteIds)
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "kagura-mod-manager", "favorites");
        Directory.CreateDirectory(dir);
        File.WriteAllLines(Path.Combine(dir, $"{gameId}.txt"), favoriteIds);
    }

    public static List<string> LoadFavorites(string gameId)
    {
        var file = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "kagura-mod-manager", "favorites", $"{gameId}.txt");
        if (!File.Exists(file)) return new List<string>();
        try { return File.ReadAllLines(file).Where(l => !string.IsNullOrWhiteSpace(l)).ToList(); }
        catch { return new List<string>(); }
    }

    public static List<ModConflict> DetectConflicts(string gameDir, List<string> activeMods)
    {
        if (activeMods.Count < 2) return new List<ModConflict>();
        var modsDir = Path.Combine(gameDir, "mods");
        var fileMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var modId in activeMods)
        {
            var modDir = Path.Combine(modsDir, modId);
            if (!Directory.Exists(modDir)) continue;

            foreach (var file in Directory.GetFiles(modDir, "*", SearchOption.AllDirectories))
            {
                var relPath = Path.GetRelativePath(modDir, file).ToLowerInvariant();
                var topFile = relPath.Split(Path.DirectorySeparatorChar)[0];
                if (MetadataFiles.Split(',').Select(s => s.Trim()).Contains(topFile)) continue;

                if (!fileMap.ContainsKey(relPath)) fileMap[relPath] = new List<string>();
                fileMap[relPath].Add(modId);
            }
        }

        return fileMap.Where(kv => kv.Value.Count > 1)
            .Select(kv => new ModConflict { FilePath = kv.Key, ModIds = kv.Value }).ToList();
    }

    public static void ApplyMods(string gameDir, List<string> activeMods)
    {
        var modsDir = Path.Combine(gameDir, "mods");
        var backupsDir = GetBackupsDir(
            new DirectoryInfo(gameDir).Name, gameDir);
        var state = LoadModState(gameDir);

        foreach (var modId in state)
        {
            var modDir = Path.Combine(modsDir, modId);
            if (!Directory.Exists(modDir)) continue;
            foreach (var file in Directory.GetFiles(modDir, "*", SearchOption.AllDirectories))
            {
                var relPath = Path.GetRelativePath(modDir, file);
                var topFile = relPath.Split(Path.DirectorySeparatorChar)[0].ToLowerInvariant();
                if (MetadataFiles.Split(',').Select(s => s.Trim()).Contains(topFile)) continue;

                var gameFile = Path.Combine(gameDir, relPath);
                var backupFile = Path.Combine(backupsDir, relPath);

                if (File.Exists(backupFile))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(gameFile)!);
                    File.Copy(backupFile, gameFile, true);
                }
                else if (File.Exists(gameFile))
                {
                    File.Delete(gameFile);
                }
            }
        }

        foreach (var modId in activeMods)
        {
            var modDir = Path.Combine(modsDir, modId);
            if (!Directory.Exists(modDir)) continue;
            foreach (var file in Directory.GetFiles(modDir, "*", SearchOption.AllDirectories))
            {
                var relPath = Path.GetRelativePath(modDir, file);
                var topFile = relPath.Split(Path.DirectorySeparatorChar)[0].ToLowerInvariant();
                if (MetadataFiles.Split(',').Select(s => s.Trim()).Contains(topFile)) continue;

                var gameFile = Path.Combine(gameDir, relPath);
                var backupFile = Path.Combine(backupsDir, relPath);

                if (File.Exists(gameFile) && !File.Exists(backupFile))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(backupFile)!);
                    File.Copy(gameFile, backupFile);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(gameFile)!);
                File.Copy(file, gameFile, true);
            }
        }

        SaveModState(gameDir, activeMods);
    }

    public static void DeleteMod(string gameDir, string modId)
    {
        var modDir = Path.Combine(gameDir, "mods", modId);
        if (Directory.Exists(modDir)) Directory.Delete(modDir, true);
    }

    private static string? ExtractIniValue(string ini, string key)
    {
        var match = System.Text.RegularExpressions.Regex.Match(ini,
            $@"{key}\s*=\s*(.+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)));
        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
    }
}
