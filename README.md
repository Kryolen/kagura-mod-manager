# kagura-mod-manager

A Senran Kagura mod manager for the Steam games. Windows desktop app, .NET 8.

Works with all of them:

- Senran Kagura Shinovi Versus
- Senran Kagura Estival Versus
- Senran Kagura Peach Beach Splash
- Senran Kagura Burst Re:Newal
- Senran Kagura Bon Appétit! - Full Course
- Senran Kagura Peach Ball
- Senran Kagura Reflexions

## Getting mods

Grab mods from wherever people share them (Nexus Mods, Discord, etc.). Each mod is usually a folder with some files in it, sometimes a `mod.ini` or preview image.

## Installing mods

1. Pick your game from the dropdown at the top.
2. Hit **Open Mods Folder**.
3. Drop each mod in its own subfolder inside that `mods` folder. So it looks like `mods/My Mod/...`.
4. Tick the mods you want on. Star your favorites if you want, drag to reorder.
5. Click **Save and Play** and you're in the game with your mods loaded.

That's it. Next time you swap mods around it cleans up after itself, so you don't have to mess with the game files by hand.

## Building it

You need the [.NET 8 SDK](https://dotnet.microsoft.com/download) on Windows.

```bash
git clone https://github.com/Kryolen/kagura-mod-manager.git
cd kagura-mod-manager
dotnet build -c Release
dotnet run -c Release
```

Or just open `KaguraModManager.csproj` in Visual Studio / Rider / whatever and hit run.
