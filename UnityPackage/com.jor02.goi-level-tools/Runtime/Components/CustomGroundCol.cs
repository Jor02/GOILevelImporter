using UnityEngine;

namespace GOILevelImporter.Components
{
    [AddComponentMenu("GOI Level Importer/Ground Collider")]
    public class CustomGroundCol : MonoBehaviour
    {
        [Tooltip("Tint of the dust and debris particles the hammer kicks up on this surface.")]
        public Color groundCol = new Color(0.7f, 0.6f, 0.3f);

        [Tooltip("Which hit, hard hit, and scrape sounds play here. Built-in names use the game's sounds, custom names come from the Custom Material Provider in the scene. Empty falls back to Rock.")]
        public string material;
    }
}
