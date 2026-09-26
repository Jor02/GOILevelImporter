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
        static bool Prefix(CameraControl __instance, ref Vector3 ___vel, Camera ___mainCam)
        {
            if (!LevelLoader.Playing || LevelLoader.HasCustomSpline)
            {
                return true;
            }

            if (!__instance.loadFinished || !Application.isPlaying)
            {
                return false;
            }

            var player = __instance.player;

            if (player == null)
            {
                player = GameObject.Find("Player");
                __instance.player = player;
            }

            if (player == null)
            {
                return false;
            }

            var settings = LevelSelectionState.Metadata;

            var target = new Vector3(player.transform.position.x, player.transform.position.y, settings.ZPlane);

            // Same perlin-ish wobble the game adds, so the camera never sits
            // perfectly still while the player is moving.
            target += new Vector3(0.001f * Mathf.Sin(Time.time), 0.001f * Mathf.Sin(Time.time), 0f);

            Vector3 delta = target - __instance.transform.position;
            ___vel += 60f * delta * Time.fixedDeltaTime - 0.12f * ___vel;
            __instance.transform.position += ___vel * Time.fixedDeltaTime;

            ApplyClipPlanes(___mainCam, settings.FarPlane, settings.BGFarPlane);
            ApplyPerspective(___mainCam, settings.CameraMode);

            return false;
        }

        private static void ApplyClipPlanes(Camera mainCam, float farPlane, float bgFarPlane)
        {
            if (mainCam != null)
            {
                mainCam.farClipPlane = farPlane;
            }

            var backgroundCam = GetBackgroundCam(mainCam);
            if (backgroundCam != null)
            {
                backgroundCam.farClipPlane = bgFarPlane;
            }
        }

        /// <summary>
        /// cam=1 puts the main camera in perspective and drops the background camera's fov so the sky lines up.
        /// </summary>
        private static void ApplyPerspective(Camera mainCam, int cameraMode)
        {
            if (mainCam == null || cameraMode != 1)
            {
                return;
            }

            mainCam.orthographic = false;

            var backgroundCam = GetBackgroundCam(mainCam);
            if (backgroundCam != null)
            {
                backgroundCam.fieldOfView = 60f;
                backgroundCam.transform.localPosition = Vector3.zero;
            }
        }

        /// <summary>
        /// Find the background camera through the transform hierarchy.
        /// </summary>
        private static Camera GetBackgroundCam(Camera mainCam)
        {
            if (mainCam == null)
            {
                return null;
            }

            var background = mainCam.transform.Find("BGCamera");
            return background != null ? background.GetComponent<Camera>() : null;
        }
    }
}