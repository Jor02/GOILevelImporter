![image](./GOILevelImporterBanner.png)

# `⚠️ THIS MOD IS STILL A WIP ⚠️`

# GOI Level Importer <sub><sup>(BepInEx rewrite)</sup></sub>
![GitHub code size in bytes](https://img.shields.io/github/languages/code-size/Jor02/GOILevelImporter?style=flat-square&color=brightgreen)
![Lines of code](https://img.shields.io/tokei/lines/github/Jor02/GOILevelImporter?style=flat-square)
> _A mod for [GOI](https://store.steampowered.com/app/240720/Getting_Over_It_with_Bennett_Foddy/) to import custom levels_<br>
> Completely remade from from scratch cause the original mod was bad.

## Installation
- [Get latest release here](https://github.com/Jor02/GOILevelImporter/releases/latest)
- [Get BepInEx here here](https://github.com/BepInEx/BepInEx/releases/latest)

1. Copy the contents of BepInEx_xxx_x.x.x.x.zip into the directory that contains GettingOverIt.exe  
(Which would most likely be `C:/Program Files (x86)/Steam/steamapps/common/Getting Over It/`)

2. Copy the contents of CustomLeverImporterMod.zip into the `.../Getting Over It/BepInEx/plugins/` directory

## Building the project
1. Install BepInEx *(Follow the first step in the `Installation` section)*

2. Install [Visual Studio](https://visualstudio.microsoft.com/vs/) and download the project *(don't forget to extract it if you're downloading it as a zip)*

3. Open the project's `.sln` file in Visual Studio,  
Building the project copies `GOILevelImporter.dll` into `.../Getting Over It/BepInEx/plugins/` automatically, and the play button launches the game.

Everything else is pulled in for you: BepInEx, Harmony and the Unity assemblies come from NuGet, and the rest are referenced straight from your game folder, which is auto-detected from Steam. No extra DLLs to copy anywhere.

### Custom game directory
The game installation is auto-detected from your Steam library, so no setup file is needed.
If your copy lives somewhere else (or auto-detection picks the wrong folder), create a `Directory.GOI.props` file in the project root, next to `Directory.Build.props`:
```xml
<Project>
  <PropertyGroup>
    <GameDirectory>C:\Path\To\Getting Over It\</GameDirectory>
  </PropertyGroup>
</Project>
```
The file is optional, everything works without it. A `Directory.GOI.targets` file in the same place is picked up before the default targets, for overriding build behavior.

Other properties you can override in those files:
* `GameExe` (default: `GettingOverIt.exe`)
* `PluginsDirectory` (default: `$(GameDirectory)BepInEx\plugins\`)
* `DeployPluginOnBuild` (default: `true`), set to `false` to stop copying the DLL into the plugins folder on every build
* `StartGameOnRun` (default: `true`), set to `false` to stop the play button from launching the game

## Third party licenses
> Please check [Third party licenses](./THIRDPARTY.md)
