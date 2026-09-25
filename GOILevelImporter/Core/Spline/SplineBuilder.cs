using System;
using FluffyUnderware.Curvy;
using GOILevelImporter.Components;
using UnityEngine;

namespace GOILevelImporter.Core.Spline
{
    /// <summary>
    /// Turns a creator authored <see cref="LevelCameraSpline"/> into a real
    /// CurvySpline. Curvy is a paid asset, so it is only referenced from the mod
    /// and never from the serialized level data.
    /// </summary>
    internal static class SplineBuilder
    {
        /// <summary>
        /// Builds a CurvySpline from the given points and parents it under
        /// <paramref name="parent"/>. Returns null when there is nothing to build.
        /// </summary>
        public static CurvySpline Build(LevelCameraSpline data, Transform parent)
        {
            if (data == null || data.Points == null || data.Points.Length < 2)
            {
                return null;
            }

            var host = new GameObject("LevelCameraSpline");
            host.transform.SetParent(parent, false);

            var spline = host.AddComponent<CurvySpline>();

            // Add() pulls control points from CurvyGlobalManager's pool when
            // UsePooling is on, and that manager is not guaranteed to exist by the
            // time a custom level loads. Turning it off makes Add create plain
            // GameObjects instead.
            spline.UsePooling = false;
            spline.Add(data.Points);

            // ProcessDirtyControlPoints has to run before the spline will answer
            // Interpolate/GetNearestPointTF calls.
            spline.Refresh();

            return spline;
        }
    }
}