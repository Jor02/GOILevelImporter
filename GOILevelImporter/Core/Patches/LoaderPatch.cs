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
            if (___loadFinished && ___safeToClick && !Base.isDefault)
            {
                LevelTransitionScreen.Instance.FadeOut();
                LevelLoader.Instance.BeginLoadLevel(Base.levelPath, Base.legacy, Base.levelHeaderSize);
            }
		}
    }
}
