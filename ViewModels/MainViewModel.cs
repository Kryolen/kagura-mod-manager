using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using KaguraModManager.Models;
using KaguraModManager.Services;
using Microsoft.Win32;

namespace KaguraModManager.ViewModels;

public class GameDir
{
    public Game Game { get; set; } = null!;
    public string Directory { get; set; } = "";
}

public class MainViewModel : ViewModelBase
{
    private AppSettings _settings = new();
    private Game? _selectedGame;
    private Mod? _selectedMod;
    private string _activeTab = "Mods";
    private bool _isLaunching;
    private string _statusMessage = "";
    private string _searchQuery = "";
    private bool _showAddModModal;
    private string _addModOption = "zip";
    private string _newModName = "";
    private string? _selectedGameIcon;

    public ObservableCollection<Game> AllGames { get; } = new(SteamService.GetAllGames());
    public ObservableCollection<Game> FoundGames { get; } = new();
    public ObservableCollection<Mod> Mods { get; } = new();
    public ObservableCollection<ModConflict> Conflicts { get; } = new();
    public ObservableCollection<GameDir> GameDirs { get; } = new();

    public Game? SelectedGame
    {
        get => _selectedGame;
        set
        {
            if (SetProperty(ref _selectedGame, value) && value != null)
            {
                LoadMods();
            }
        }
    }

    public Mod? SelectedMod
    {
        get => _selectedMod;
        set => SetProperty(ref _selectedMod, value);
    }

    public string ActiveTab
    {
        get => _activeTab;
        set => SetProperty(ref _activeTab, value);
    }

    public bool IsLaunching
    {
        get => _isLaunching;
        set => SetProperty(ref _isLaunching, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
                LoadMods();
        }
    }

    public bool ShowAddModModal
    {
        get => _showAddModModal;
        set => SetProperty(ref _showAddModModal, value);
    }

    public string AddModOption
    {
        get => _addModOption;
        set => SetProperty(ref _addModOption, value);
    }

    public string NewModName
    {
        get => _newModName;
        set => SetProperty(ref _newModName, value);
    }

    public string? SelectedGameIcon
    {
        get => _selectedGameIcon;
        set => SetProperty(ref _selectedGameIcon, value);
    }

    public ICommand ToggleModCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand ShowAddModModalCommand { get; }
    public ICommand ConfirmAddModCommand { get; }
    public ICommand DeleteModCommand { get; }
    public ICommand OpenModFolderCommand { get; }
    public ICommand OpenModsFolderCommand { get; }
    public ICommand RefreshModsCommand { get; }
    public ICommand SaveAndPlayCommand { get; }
    public ICommand ScanGamesCommand { get; }
    public ICommand BrowseDirectoryCommand { get; }

    public MainViewModel()
    {
        ToggleModCommand = new RelayCommand<Mod>(mod => { if (mod != null) ToggleMod(mod); });
        ToggleFavoriteCommand = new RelayCommand<Mod>(mod => { if (mod != null) ToggleFavorite(mod); });
        ShowAddModModalCommand = new RelayCommand(_ => ShowAddModModal = true);
        ConfirmAddModCommand = new RelayCommand(_ => ConfirmAddMod());
        DeleteModCommand = new RelayCommand<Mod>(mod => { if (mod != null) DeleteMod(mod); });
        OpenModFolderCommand = new RelayCommand<Mod>(mod => { if (mod != null) OpenModFolder(mod); });
        OpenModsFolderCommand = new RelayCommand(_ => OpenModsFolder());
        RefreshModsCommand = new RelayCommand(_ => LoadMods());
        SaveAndPlayCommand = new RelayCommand(_ => SaveAndPlay());
        ScanGamesCommand = new RelayCommand(_ => ScanGames());
        BrowseDirectoryCommand = new RelayCommand<Game>(game => { if (game != null) BrowseDirectory(game); });

        LoadSettings();
    }

    private void LoadSettings()
    {
        _settings = FileService.LoadSettings();
        SteamService.ScanForGames(_settings);
        FileService.SaveSettings(_settings);
        RefreshFoundGames();
        RefreshGameDirsList();
        LoadGameIcons();
        if (FoundGames.Count > 0)
            SelectedGame = FoundGames[0];
    }

    private void RefreshFoundGames()
    {
        FoundGames.Clear();
        foreach (var game in AllGames)
        {
            if (_settings.GameDirs.TryGetValue(game.Id, out var dir) && !string.IsNullOrEmpty(dir))
                FoundGames.Add(game);
        }
    }

    private void RefreshGameDirsList()
    {
        GameDirs.Clear();
        foreach (var game in AllGames)
        {
            _settings.GameDirs.TryGetValue(game.Id, out var dir);
            GameDirs.Add(new GameDir { Game = game, Directory = dir ?? "" });
        }
    }

    private string GetGameDir(Game game)
    {
        _settings.GameDirs.TryGetValue(game.Id, out var dir);
        return dir ?? "";
    }

    private void LoadGameIcons()
    {
        foreach (var game in FoundGames)
        {
            if (!_settings.GameDirs.TryGetValue(game.Id, out var dir))
            {
                game.IconBitmap = null;
                continue;
            }

            try
            {
                if (!Directory.Exists(dir)) { game.IconBitmap = null; continue; }
                var exeFile = Directory.GetFiles(dir, "*.exe")
                    .FirstOrDefault(f =>
                    {
                        var name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
                        return !name.Contains("crash") && !name.Contains("config") && !name.Contains("unins");
                    });

                if (exeFile != null)
                {
                    var icon = System.Drawing.Icon.ExtractAssociatedIcon(exeFile);
                    if (icon != null)
                    {
                        using var bmp = icon.ToBitmap();
                        var ms = new MemoryStream();
                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        ms.Position = 0;
                        var bi = new BitmapImage();
                        bi.BeginInit();
                        bi.StreamSource = ms;
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.EndInit();
                        bi.Freeze();
                        game.IconBitmap = bi;
                    }
                }
            }
            catch { }
        }
    }

    private void LoadMods()
    {
        Mods.Clear();
        Conflicts.Clear();
        if (SelectedGame == null || !_settings.GameDirs.TryGetValue(SelectedGame.Id, out var dir)) return;
        if (!Directory.Exists(dir)) return;

        var mods = FileService.ReadMods(dir);
        var filtered = string.IsNullOrWhiteSpace(SearchQuery)
            ? mods
            : mods.Where(m => m.Title.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                              m.Author.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var mod in filtered) Mods.Add(mod);
        RefreshConflicts();
    }

    private void RefreshConflicts()
    {
        Conflicts.Clear();
        if (SelectedGame == null || !_settings.GameDirs.TryGetValue(SelectedGame.Id, out var dir)) return;
        var active = Mods.Where(m => m.IsEnabled).Select(m => m.Id).ToList();
        foreach (var c in FileService.DetectConflicts(dir, active))
            Conflicts.Add(c);
    }

    private void ToggleMod(Mod mod)
    {
        mod.IsEnabled = !mod.IsEnabled;
        OnPropertyChanged(nameof(Mods));
        RefreshConflicts();
    }

    private void ToggleFavorite(Mod mod)
    {
        mod.IsFavorite = !mod.IsFavorite;
        FileService.SaveFavorites(SelectedGame?.Id ?? "", Mods.Where(m => m.IsFavorite).Select(m => m.Id).ToList());
        OnPropertyChanged(nameof(Mods));
    }

    private void ConfirmAddMod()
    {
        if (!string.IsNullOrEmpty(AddModOption))
        {
            if (AddModOption == "zip") AddModZip();
            else if (AddModOption == "folder") AddModFolder();
            else if (AddModOption == "new")
            {
                if (string.IsNullOrWhiteSpace(NewModName))
                {
                    MessageBox.Show("Please enter a name for the new mod.", "Missing Name", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                CreateNewMod(NewModName);
            }
        }
        ShowAddModModal = false;
        AddModOption = "zip";
        NewModName = "";
    }

    private void AddModZip()
    {
        if (SelectedGame == null || !_settings.GameDirs.TryGetValue(SelectedGame.Id, out var dir))
        {
            MessageBox.Show("Please select a game first.", "No Game Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var dlg = new OpenFileDialog { Title = "Select Mod ZIP", Filter = "ZIP files (*.zip)|*.zip" };
        if (dlg.ShowDialog() != true) return;

        var modsDir = Path.Combine(dir, "mods");
        Directory.CreateDirectory(modsDir);

        try
        {
            var folderName = Path.GetFileNameWithoutExtension(dlg.FileName);
            var destPath = Path.Combine(modsDir, folderName);
            Directory.CreateDirectory(destPath);

            System.IO.Compression.ZipFile.ExtractToDirectory(dlg.FileName, destPath);
            StatusMessage = $"Added mod: {folderName}";
            LoadMods();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to add mod: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddModFolder()
    {
        if (SelectedGame == null || !_settings.GameDirs.TryGetValue(SelectedGame.Id, out var dir))
        {
            MessageBox.Show("Please select a game first.", "No Game Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var dlg = new OpenFolderDialog { Title = "Select Mod Folder" };
        if (dlg.ShowDialog() != true) return;

        var modsDir = Path.Combine(dir, "mods");
        Directory.CreateDirectory(modsDir);

        try
        {
            var folderName = new DirectoryInfo(dlg.FolderName).Name;
            var destPath = Path.Combine(modsDir, folderName);
            CopyDirectory(dlg.FolderName, destPath);
            StatusMessage = $"Added mod: {folderName}";
            LoadMods();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to add mod: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CreateNewMod(string modName)
    {
        if (SelectedGame == null || !_settings.GameDirs.TryGetValue(SelectedGame.Id, out var dir)) return;
        var modsDir = Path.Combine(dir, "mods");
        Directory.CreateDirectory(modsDir);

        var folderName = System.Text.RegularExpressions.Regex.Replace(modName, @"[^a-zA-Z0-9 -]", "").Trim();
        if (string.IsNullOrEmpty(folderName)) folderName = "New Mod";

        var destPath = Path.Combine(modsDir, folderName);
        int counter = 1;
        while (Directory.Exists(destPath))
        {
            destPath = Path.Combine(modsDir, $"{folderName} ({counter})");
            counter++;
        }

        try
        {
            Directory.CreateDirectory(destPath);
            if (SelectedGame.Id != "sk-sv")
                Directory.CreateDirectory(Path.Combine(destPath, "GameData"));

            File.WriteAllText(Path.Combine(destPath, "mod.ini"),
                $"Title={modName}\nDescription=A new mod for {SelectedGame.Id}\nVersion=1.0\nAuthor=Unknown");
            StatusMessage = $"Created mod: {Path.GetFileName(destPath)}";
            LoadMods();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to create mod: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteMod(Mod mod)
    {
        if (SelectedGame == null || !_settings.GameDirs.TryGetValue(SelectedGame.Id, out var dir)) return;
        var result = MessageBox.Show(
            $"Are you sure you want to delete \"{mod.Title}\"?\nThis action cannot be undone.",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            FileService.DeleteMod(dir, mod.Id);
            StatusMessage = $"Deleted mod: {mod.Title}";
            LoadMods();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to delete mod: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenModFolder(Mod mod)
    {
        if (Directory.Exists(mod.FullPath))
            OpenFolderInExplorer(mod.FullPath);
    }

    private void OpenModsFolder()
    {
        if (SelectedGame == null || !_settings.GameDirs.TryGetValue(SelectedGame.Id, out var dir)) return;
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return;
        var modsDir = Path.Combine(dir, "mods");
        Directory.CreateDirectory(modsDir);
        OpenFolderInExplorer(modsDir);
    }

    private static void OpenFolderInExplorer(string path)
    {
        // Use the shell to open the folder directly. This handles paths with
        // spaces (e.g. "Senran Kagura Burst Re:Newal") without quoting issues
        // and avoids explorer.exe falling back to the Documents folder.
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch
        {
            // Fallback: launch explorer explicitly with a quoted path.
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            });
        }
    }

    private async void SaveAndPlay()
    {
        if (SelectedGame == null) return;
        if (!_settings.GameDirs.TryGetValue(SelectedGame.Id, out var dir)) return;

        IsLaunching = true;
        StatusMessage = "Applying mods...";

        var activeMods = Mods.Where(m => m.IsEnabled).Select(m => m.Id).Reverse().ToList();

        await Task.Run(() =>
        {
            FileService.ApplyMods(dir, activeMods);
            FileService.SaveModOrder(dir, Mods.Select(m => m.Id).ToList());
        });

        StatusMessage = "Launching game...";
        SteamService.LaunchGame(SelectedGame.SteamAppId);

        IsLaunching = false;
        StatusMessage = "Done";
    }

    private void ScanGames()
    {
        FileService.SaveSettings(_settings);
        SteamService.ScanForGames(_settings);
        FileService.SaveSettings(_settings);
        RefreshFoundGames();
        RefreshGameDirsList();
        LoadGameIcons();
        StatusMessage = $"Scan complete. Found {FoundGames.Count} game(s).";
    }

    private void BrowseDirectory(Game game)
    {
        var dlg = new OpenFolderDialog { Title = $"Select {game.Name} directory" };
        if (dlg.ShowDialog() != true) return;

        _settings.GameDirs[game.Id] = dlg.FolderName;
        FileService.SaveSettings(_settings);
        RefreshFoundGames();
        RefreshGameDirsList();
        LoadGameIcons();
        if (SelectedGame?.Id == game.Id) LoadMods();
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)));
        foreach (var d in Directory.GetDirectories(source))
            CopyDirectory(d, Path.Combine(dest, Path.GetFileName(d)));
    }
}
