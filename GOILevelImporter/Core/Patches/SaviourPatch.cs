using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace GOILevelImporter.Core.Patches
{
    [HarmonyPatch(typeof(Saviour), "SaveGameNow")]
    class SaviourPatch
    {
        static bool Prefix()
        {
            if (LevelLoader.Async) return false;
            return true;
        }
    }
}
