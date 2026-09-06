# Kagura Mod Manager

A Senran Kagura mod manager for the Steam games. 

Works with all of them:

- Senran Kagura Shinovi Versus
- Senran Kagura Estival Versus
- Senran Kagura Peach Beach Splash
- Senran Kagura Burst Re:Newal
- Senran Kagura Bon Appétit! - Full Course
- Senran Kagura Peach Ball
- Senran Kagura Reflexions

## Getting mods

Grab mods from wherever people share them (Nexus Mods, Discord, etc.). Each mod is usually a folder with some files in it with a `mod.ini`. Out of the box, most mods won't work with the mod manager. To make the mods work, you have to copy the folder structure in the root folder., i.e. 'GameData/Model/Costume/Uniform/'.

## Installing mods

1. Pick your game from the dropdown at the bottom.
2. Hit **Open Mods Folder**.
3. Drop each mod in its own subfolder inside that `mods` folder. So it looks like `mods/My Mod/...`.
4. Tick the mods you want on. Star your favorites if you want, drag to reorder.
5. Click **Save and Play** and you're in the game with your mods loaded.

That's it. Next time you swap mods around it cleans up after itself, so you don't have to mess with the game files by hand.

## Running the release

If you just want to use it, grab the `.exe` from the [releases page](https://github.com/Kryolen/kagura-mod-manager/releases) and run it.

You'll need the [.NET 8 desktop runtime](https://dotnet.microsoft.com/download/dotnet/8.0) installed. Grab the "Desktop Runtime" for Windows x64 and install it, then the `.exe` just works.

## Building it

You need the [.NET 8 SDK](https://dotnet.microsoft.com/download) on Windows.

```bash
git clone https://github.com/Kryolen/kagura-mod-manager.git
cd kagura-mod-manager
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false
```

