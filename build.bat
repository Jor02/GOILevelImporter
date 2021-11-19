@echo off
setlocal

rem -- Input
set /P goi_dir=<BuildVars\getting_over_it_path.txt

rem -- Variables
set "goi_plugins=%goi_dir%\BepInEx\Plugins"
set "goi_exec=%goi_dir%\GettingOverIt.exe"

set "plugin_dll=%*\GOILevelImporter.dll"
set "newtonsoft_json=%*\Newtonsoft.Json.dll"

rem -- Copying DLLs and running the game
copy /Y "%plugin_dll%"      "%goi_plugins%" || call :throw_file_error
copy /Y "%newtonsoft_json%" "%goi_plugins%" || call :throw_file_error
"%goi_exec%"
exit /B %errorlevel%

rem -- Error function in case people don't read the Readme
:throw_file_error
mshta javascript:alert("GOI directory wasn't found!\nTry replacing the text inside of 'BuildVars\\getting_over_it_path.txt' with the path to your Getting Over It directory (the one with BepInEx installed)");close();
exit /B -1
