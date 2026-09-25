using System;
using UnityEngine;

namespace GOILevelImporter.Core.Components
{
    /// <summary>
    /// Camera path authored by a level creator. Only plain UnityEngine types are
    /// used so this file can be shared with the editor side level tools, which
    /// cannot reference the game's assemblies.
    ///
    /// The mod converts these points into a CurvySpline at load time. Curvy is a
    /// paid asset and cannot be shipped to creators, so it must not appear in the
    /// serialized data.
    /// </summary>
    [Serializable]
    public class LevelCameraSpline
    {
        /// <summary>World space points in path order, needs at least 2 to be usable.</summary>
        public Vector3[] Points;
    }

    /// <summary>
    /// Drop this in a level scene to give the level its own camera path. When
    /// present the mod hands the points to the game's own camera and pose scripts
    /// and the fallback patches stand down. When absent the patches take over so
    /// the level still plays.
    /// </summary>
    [AddComponentMenu("GOI Level Importer/Level Camera Path")]
    public class LevelCameraPath : MonoBehaviour
    {
        public LevelCameraSpline spline;
    }
}