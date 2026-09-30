using System.Collections.Generic;

namespace GOILevelImporter
{
    internal sealed class OutlineEdgeSet
    {
        private readonly Dictionary<long, OutlineEdge> lookup = new Dictionary<long, OutlineEdge>();

        public void AddCoverage(int from, int to, bool counterClockwise)
        {
            int a = System.Math.Min(from, to);
            int b = System.Math.Max(from, to);
            long key = OutlineEdge.PairKey(a, b);

            if (!lookup.TryGetValue(key, out OutlineEdge edge))
            {
                edge = new OutlineEdge(a, b);
                lookup.Add(key, edge);
            }

            bool coversLeft = (from < to) == counterClockwise;

            edge.Sides |= coversLeft ? 1 : 2;
        }

        public List<OutlineEdge> CollectOutlineCandidates()
        {
            var candidates = new List<OutlineEdge>();

            foreach (OutlineEdge edge in lookup.Values)
            {
                if (edge.Sides != 3)
                    candidates.Add(edge);
            }

            return candidates;
        }
    }
}
