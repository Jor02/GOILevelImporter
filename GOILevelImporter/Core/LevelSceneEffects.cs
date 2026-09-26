using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GOILevelImporter.Core
{
    /// <summary>
    /// One-time scene tweaks applied right after a level's scene finishes loading
    /// </summary>
    static class LevelSceneEffects
    {
        public static void Apply(LevelMetadata settings)
        {
            if (settings.ReplacementMap)
            {
                RemoveNonEssentialObjects();
            }

            GameObject backgroundCamera = GameObject.Find("/Main Camera/BGCamera");
            UnityEngine.Object.Destroy(backgroundCamera.GetComponent<FogControl>());
            UnityEngine.Object.Destroy(backgroundCamera.GetComponent<FogVolumeRenderer>());

            if (settings.ReplaceSky)
            {
                ReplaceSky(backgroundCamera);
            }
            else
            {
                // Without a replacement sky the background camera would draw nothing behind the level.
                backgroundCamera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            }

            if (settings.HideShadow)
            {
                HideHammerShadow();
            }

            if (settings.FixHammerMaterial)
            {
                FixHammerMaterial();
            }

            if (settings.ReplaceLighting)
            {
                ApplyLighting();
            }

            ApplyFog(settings.Fog);
        }

        private static void RemoveNonEssentialObjects()
        {
            foreach (GameObject gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!IsRequiredGameObject(gameObject))
                {
                    UnityEngine.Object.Destroy(gameObject);
                }
            }
        }

        private static bool IsRequiredGameObject(GameObject target)
        {
            switch (target.name)
            {
                case "Player":
                case "Canvas":
                case "EventSystem":
                case "Main Camera":
                case "HitSounds":
                case "ImpactSprites":
                case "Cursor":
                case "Force Camera Ratios":
                case "Rewired Input Manager":
                    return true;
                default:
                    return false;
            }
        }

        private static void ReplaceSky(GameObject backgroundCamera)
        {
            UnityEngine.Object.Destroy(GameObject.Find("CloudSystems"));
            UnityEngine.Object.Destroy(GameObject.Find("SkySphere"));

            var sky = backgroundCamera.transform.Find("Sky");
            if (sky != null) UnityEngine.Object.Destroy(sky.gameObject);

            var starnest = backgroundCamera.transform.Find("Starnest");
            if (starnest != null) UnityEngine.Object.Destroy(starnest.gameObject);

            var camera = backgroundCamera.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = Color.black;
        }

        private static void HideHammerShadow()
        {
            var shadow = GameObject.Find("/Player/Hub/Slider/Handle/PoleMiddle/climbinghammer_remap/RetopoGroup1/Shadow");
            if (shadow != null) shadow.SetActive(false);
        }

        private static void FixHammerMaterial()
        {
            var hammerMesh = GameObject.Find("/Player/handle/Mesh");
            if (hammerMesh == null) return;

            var renderer = hammerMesh.GetComponent<SkinnedMeshRenderer>();
            if (renderer != null) renderer.material.shader = Shader.Find("Standard");
        }

        /// <summary>
        /// Flattens the ambient lighting. Levels that build their own lighting would otherwise not look very good.
        /// </summary>
        private static void ApplyLighting()
        {
            RenderSettings.ambientEquatorColor = new Color(0.3f, 0.3f, 0.3f);
            RenderSettings.ambientGroundColor = new Color(0.5f, 0.5f, 0.5f);
            RenderSettings.ambientLight = Color.white;
            RenderSettings.ambientSkyColor = Color.black;
        }

        /// <summary>
        /// Applies a level's fog color. The value is hex without a leading #,
        /// optionally followed by two more digits for density. Anything that
        /// fails to parse leaves the fog off entirely.
        /// </summary>
        private static void ApplyFog(string fog)
        {
            RenderSettings.fog = false;

            if (string.IsNullOrWhiteSpace(fog))
            {
                return;
            }

            try
            {
                string hex = fog.TrimStart('#');

                int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);

                float density = 0.013f;
                if (hex.Length > 6)
                {
                    density = int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber) / 5100f;
                }

                RenderSettings.fogColor = new Color(r / 255f, g / 255f, b / 255f, 1f);
                RenderSettings.fogDensity = density;
                RenderSettings.fog = true;
            }
            catch (Exception)
            {
                // A malformed value should just mean no fog, not a failed level load.
                RenderSettings.fog = false;
            }
        }
    }
}
