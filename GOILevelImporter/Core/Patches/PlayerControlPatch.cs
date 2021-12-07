using GOILevelImporter.Core.Menu;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GOILevelImporter.Core.Patches
{
    [HarmonyPatch(typeof(PlayerControl), "Update")]
    class PlayerControlPatch
    {
        static bool Prefix(ref int ___numWins)
        {
            if (LevelLoader.Async) return false;
            if (!Base.isDefault && ___numWins > 0 && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKey(KeyCode.R))
            {
                Time.timeScale = 0;
                Physics2D.simulationMode = SimulationMode2D.Script;

                LevelLoader.Async = true;
                LevelTransitionScreen.Instance.FadeOut();

                PlayerPrefs.DeleteKey("NumSaves");
                PlayerPrefs.DeleteKey("SaveGame0");
                PlayerPrefs.DeleteKey("SaveGame1");
                PlayerPrefs.Save();

                LevelLoader.Instance.LoadLevelAsync(SceneManager.LoadSceneAsync("Mian"));

                return false;
            }
            return true;
        }
    }
}
