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
            if (LevelLoader.LoadingMian) return false;
            if (!LevelSelectionState.IsDefault && ___numWins > 0 && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKey(KeyCode.R))
            {
                // Reload(false) clears any sub-scene a SwitchScene trigger saved
                // earlier in the run, so a reset returns to the entry scene.
                LevelLoader.Instance.Reload(false);
                return false;
            }
            return true;
        }
    }
}
