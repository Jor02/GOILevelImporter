﻿using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GOILevelImporter.Core.Patches
{
    [HarmonyPatch(typeof(Loader), "ContinueGame")]
    class LoaderPatch
    {
        static void Prefix(Loader __instance, ref bool ___loadFinished, ref bool ___safeToClick)
        {
            if (___loadFinished && ___safeToClick && !Base.isDefault)
            {
                __instance.StartCoroutine(LevelLoader.Instance.BeginLoadLevel(Base.levelPath, Base.levelHeaderSize));
            }
		}
    }
}