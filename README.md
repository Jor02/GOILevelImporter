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

1. Copy the contents of BepInEx_xxx_x.x.x.x.zip into the directory that contains GettingOverIt.exe<br/>
(Which would most likely be `C:/Program Files (x86)/Steam/steamapps/common/Getting Over It/`)

2. Copy the contents of CustomLeverImporterMod.zip into the `.../Getting Over It/BepInEx/plugins/` directory

## Building the project
1. Install BepInEx *(Follow the first step in the `Installation` section)*

2. Install [Visual Studio](https://visualstudio.microsoft.com/vs/) and download the project *(don't forget to extract it if you're downloading it as a zip)*

3. Go inside the project, into the `BuildVars` directory, make a text file called `getting_over_it_path`, and paste the path to Getting Over It inside that file,<br/>
*Make sure there are no empty spaces around the path!*

4. Make a `Libs` directory at the root of the project and throw in the following things:
    * All the DLLs in `.../Getting Over It/BepInEx/core/`
    * All the DLLs in `.../Getting Over It/GettingOverIt_Data/Managed/` that have "Unity" at the start of their name

5. Open the project's `.sln` file in Visual Studio,<br/>
And just like that you can mess around with the project on your own!

## Third party licenses
> Please check [Third party licenses](./THIRDPARTY.md)
