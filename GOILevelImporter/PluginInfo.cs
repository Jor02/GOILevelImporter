using GOILevelImporter;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

[assembly: AssemblyVersion(PluginInfo.VERSION)]
[assembly: AssemblyTitle(PluginInfo.NAME + " (" + PluginInfo.GUID + ")")]
[assembly: AssemblyProduct(PluginInfo.NAME)]

namespace GOILevelImporter
{
    /// <summary>
    /// Plugin infomation
    /// Based on https://github.com/BepInEx/BepInEx.PluginTemplate/blob/main/src/PluginInfo.cs
    /// </summary>
    internal static class PluginInfo
    {
        public const string GUID = "com.jor02.goilevelimporter";
        public const string NAME = "GOI Level Importer";
        public const string VERSION = "3.0.0";
        public const string VERSIONPOST = "beta.1";
        
        public const string FULLVERSION = VERSION + "-" + VERSIONPOST;
    }
}
