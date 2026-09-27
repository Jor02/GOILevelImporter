using UnityEngine;
using UnityEngine.UI;

namespace GOILevelImporter.Core.Menu
{
    /// <summary>
    /// Spins the refresh button's icon on click so the press is visibly acknowledged.
    /// The icon turns a full revolution and eases out, then rests until the next click.
    /// </summary>
    internal class RefreshButtonSpin : MonoBehaviour
    {
        private const float Duration = 0.55f;
        private const float Turns = 1f;

        private RectTransform icon;
        private float elapsed = Duration;

        private void Awake()
        {
            RectTransform iconRect = transform.Find("Image") as RectTransform;
            if (iconRect != null) icon = iconRect;

            Button button = GetComponent<Button>();
            if (button == null)
            {
                Debug.LogWarning("[GOI Level Importer] Refresh button has no Button component, so it cannot spin.");
                return;
            }

            button.onClick.AddListener(OnClicked);
        }

        private void OnClicked()
        {
            elapsed = 0f;
        }

        private void Update()
        {
            if (icon == null || elapsed >= Duration) return;

            elapsed += Time.deltaTime;

            // Eased out so the spin decelerates into its resting angle instead of
            // stopping dead, which reads more like a refresh completing.
            float t = Mathf.Clamp01(elapsed / Duration);
            float eased = 1f - (1f - t) * (1f - t);
            icon.localRotation = Quaternion.Euler(0f, 0f, -360f * Turns * eased);
        }
    }
}
