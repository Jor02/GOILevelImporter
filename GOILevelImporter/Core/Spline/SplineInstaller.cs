using System;
using FluffyUnderware.Curvy;
using GOILevelImporter.Components;
using GOILevelImporter.Utils;
using UnityEngine;

namespace GOILevelImporter.Core.Spline
{
    /// <summary>
    /// Installs a creator authored camera path into the game's camera and pose
    /// scripts, so they follow the level's spline instead of the one baked into
    /// the "Mian" scene.
    /// </summary>
    internal static class SplineInstaller
    {
        /// <summary>
        /// Builds the level's spline and hands it to the game scripts.
        /// Returns false when the level has no usable path, which leaves the
        /// fallback patches in charge.
        /// </summary>
        public static bool TryInstall(LevelCameraPath path)
        {
            if (path == null)
            {
                return false;
            }

            var cameraControl = UnityEngine.Object.FindObjectOfType<CameraControl>();
            if (cameraControl == null)
            {
                return false;
            }

            var spline = SplineBuilder.Build(path.spline, path.transform.parent);
            if (spline == null)
            {
                return false;
            }

            var meter = ResolveProgressMeter(cameraControl);
            if (meter == null)
            {
                // The camera reads currentTF off the meter to advance along the
                // path, so without one a creator spline would never move the
                // camera. Report failure and let the fallback patches handle it.
                UnityEngine.Object.Destroy(spline.gameObject);
                return false;
            }

            cameraControl.spline = spline;
            cameraControl.progressMeter = meter;
            meter.SetPrivateFieldValue("spline", spline);
            ApplyToPoseControl(spline);

            return true;
        }

        /// <summary>
        /// ProgressMeter grabs its own spline in Start via GetComponent, so the
        /// field has to be overwritten directly. The existing meter is reused so
        /// the narrator keeps hearing about progress and retreats.
        /// </summary>
        private static ProgressMeter ResolveProgressMeter(CameraControl cameraControl)
        {
            if (cameraControl.progressMeter != null)
            {
                return cameraControl.progressMeter;
            }

            return UnityEngine.Object.FindObjectOfType<ProgressMeter>();
        }

        /// <summary>
        /// PoseControl snapshots the spline's control points into a private cache
        /// during Awake and never re-reads them. Swapping the spline without
        /// rebuilding that cache would leave the character stuck on the old
        /// path's Z for the whole level.
        /// </summary>
        private static void ApplyToPoseControl(CurvySpline spline)
        {
            var poses = Resources.FindObjectsOfTypeAll<PoseControl>();
            if (poses.Length == 0)
            {
                return;
            }

            var pose = poses[0];
            pose.spline = spline;

            var points = spline.ControlPointsList;
            var cached = new Vector3[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                cached[i] = points[i].position;
            }

            pose.SetPrivateFieldValue("controlPoints", cached);
        }
    }
}