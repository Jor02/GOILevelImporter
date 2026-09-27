using System;
using System.Collections.Generic;
using System.Text;
using GOILevelImporter.Core.Menu;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GOILevelImporter.Core.Patches
{
    [HarmonyPatch(typeof(Loader), "ContinueGame")]
    class LoaderPatch
    {
        static void Prefix(ref bool ___loadFinished, ref bool ___safeToClick)
        {
            if (___loadFinished && ___safeToClick && !LevelSelectionState.IsDefault)
            {
                LevelTransitionScreen.Instance.FadeOut();
                LevelLoader.Instance.BeginLoadLevel();
            }
		}
    }

    [HarmonyPatch(typeof(Loader), "StartGame")]
    class LoaderStartGamePatch
    {
        static void Prefix()
        {
            LevelSelectionState.ClearPendingScene();
        }
    }
}
