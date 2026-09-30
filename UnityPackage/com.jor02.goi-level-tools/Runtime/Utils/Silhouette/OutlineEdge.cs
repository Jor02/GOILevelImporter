using System.Collections.Generic;

namespace GOILevelImporter
{
    internal sealed class OutlineEdge
    {
        public readonly int A;              // Lower node id
        public readonly int B;              // Higher node id
        public int Sides;                   // Bit 0: a triangle lies left of A to B. Bit 1: one lies right
        public List<int> SplitNodes;        // Nodes where other edges cross or touch this one

        public OutlineEdge(int a, int b)
        {
            A = a;
            B = b;
        }

        public void AddSplit(int node)
        {
            if (node == A || node == B)
                return;

            if (SplitNodes == null)
                SplitNodes = new List<int>();

            SplitNodes.Add(node);
        }

        /// <summary>
        /// Packs two node ids into one key that is the same for either order.
        /// </summary>
        public static long PairKey(int a, int b)
        {
            return a < b
                ? ((long)a << 32) | (uint)b
                : ((long)b << 32) | (uint)a;
        }
    }
}
