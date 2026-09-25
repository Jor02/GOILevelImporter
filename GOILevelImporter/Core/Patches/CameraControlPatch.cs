using GOILevelImporter.Utils;
using HarmonyLib;
using UnityEngine;

namespace GOILevelImporter.Core.Patches
{
    /// <summary>
    /// Detaches the camera from the spline that ships with the "Mian" scene while
    /// a level without its own camera path is playing, and follows the player
    /// directly instead.
    ///
    /// Levels that supply a <c>LevelCameraPath</c> have the game's own spline
    /// installed, so the original method is left to run untouched for them.
    /// </summary>
    [HarmonyPatch(typeof(CameraControl), "FixedUpdate")]
    class CameraControlPatch
    {
        static bool Prefix(CameraControl __instance, ref GameObject player, ref Vector3 ___vel)
        {
            if (!LevelLoader.Playing || LevelLoader.HasCustomSpline)
            {
                return true;
            }

            if (!__instance.loadFinished || !Application.isPlaying)
            {
                return false;
            }

            if (player == null)
            {
                player = GameObject.Find("Player");
                __instance.player = player;
            }

            if (player == null)
            {
                return false;
            }

            var target = new Vector3(player.transform.position.x, player.transform.position.y, -20f);

            // Same perlin-ish wobble the game adds, so the camera never sits
            // perfectly still while the player is moving.
            target += new Vector3(0.001f * Mathf.Sin(Time.time), 0.001f * Mathf.Sin(Time.time), 0f);

            Vector3 delta = target - __instance.transform.position;
            ___vel += 60f * delta * Time.fixedDeltaTime - 0.12f * ___vel;
            __instance.transform.position += ___vel * Time.fixedDeltaTime;

            return false;
        }
    }
}