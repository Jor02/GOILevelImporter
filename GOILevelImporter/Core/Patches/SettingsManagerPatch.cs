using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.SceneManagement;

namespace GOILevelImporter.Core.Patches
{
    [HarmonyPatch(typeof(SettingsManager), "Update")]
    class SettingsManagerPatch
    {
        static bool Prefix(ref bool __runOriginal, ref SettingsManager __instance, ref RestartOnContact ___underwaterDetector)
        {
            if (Time.timeSinceLevelLoad > 1.5f && Input.GetKeyDown(KeyCode.Escape) &&
                (LevelLoader.Playing || (___underwaterDetector != null && !___underwaterDetector.resetting && SceneManager.GetActiveScene().name == "Mian")
            ))
            {
                __instance.ToggleMenu();
            }

            //Skip original update
            return false;
        }
    }
}
