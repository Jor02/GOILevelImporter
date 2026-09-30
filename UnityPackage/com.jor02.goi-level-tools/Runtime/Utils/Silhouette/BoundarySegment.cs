namespace GOILevelImporter
{
    /// <summary>
    /// A piece of an outline edge, between two merged nodes, that lies on the silhouette.
    /// </summary>
    internal readonly struct BoundarySegment
    {
        public readonly int A;
        public readonly int B;

        public BoundarySegment(int a, int b)
        {
            A = a;
            B = b;
        }
    }
}
