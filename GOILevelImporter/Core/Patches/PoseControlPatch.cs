using GOILevelImporter.Utils;
using HarmonyLib;
using UnityEngine;

namespace GOILevelImporter.Core.Patches
{
    /// <summary>
    /// Keeps the character and pot on a fixed Z while a level without its own
    /// camera path is playing. The game normally sets that Z from the spline it
    /// ships with, which is meaningless once a custom level is loaded.
    ///
    /// Levels that supply a <c>LevelCameraPath</c> drive their own Z through the
    /// game's spline, so this stands down for them.
    /// </summary>
    [HarmonyPatch(typeof(PoseControl), "LateUpdate")]
    class PoseControlPatch
    {
        private const float CustomLevelZ = -0.7f;

        static void Postfix(PoseControl __instance)
        {
            if (!LevelLoader.Playing || LevelLoader.HasCustomSpline)
            {
                return;
            }

            // Set after the game's LateUpdate so our value wins, since the game
            // reassigns both hubs every frame from GetNearestSplineZ.
            float potOffset = __instance.GetPrivateFieldValue<float>("dudePotOffset");

            __instance.dudeMeshHub.position = new Vector3(
                __instance.dudeMeshHub.position.x,
                __instance.dudeMeshHub.position.y,
                CustomLevelZ);

            __instance.potMeshHub.position = new Vector3(
                __instance.potMeshHub.position.x,
                __instance.potMeshHub.position.y,
                CustomLevelZ - potOffset);
        }
    }
}