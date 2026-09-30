namespace GOILevelImporter
{
    /// <summary>
    /// Tolerances for the silhouette pipeline. It runs in a normalized space where the
    /// projected mesh fits in a 1 x 1 box, so each value is a fraction of the model size.
    /// </summary>
    internal static class ProjectionTolerance
    {
        public const float Epsilon = 1e-6f;
        public const float MinLoopArea = 1e-9f;
    }
}
