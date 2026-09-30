using System.Collections.Generic;
using UnityEngine;

namespace GOILevelImporter
{
    /// <summary>
    /// One closed path of the outline, in the pipeline's normalized space.
    /// </summary>
    internal sealed class OutlineLoop
    {
        public List<Vector2> Points;
        public bool IsHole;
        public int Depth;
        public Vector2 TestPoint;

        public OutlineLoop(List<Vector2> points)
        {
            Points = points;
        }
    }
}
