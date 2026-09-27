using TMPro;
using UnityEngine;

namespace GOILevelImporter.Core.Menu
{
    internal class CardTitleCentering : MonoBehaviour
    {
        private TextMeshProUGUI label;
        private bool corrected;

        void LateUpdate()
        {
            if (corrected) return;

            if (label == null)
            {
                RectTransform labelRect = transform.Find("TextArea/Label") as RectTransform;
                if (labelRect == null) return;
                label = labelRect.GetComponent<TextMeshProUGUI>();
                if (label == null) return;
            }

            if (string.IsNullOrEmpty(label.text)) return; // LevelButton fills it in later
            if (label.textInfo.characterCount == 0) return;

            if (!TryMeasureFirstLine(out float bottom, out float top)) return;

            float glyphCenter = (bottom + top) * 0.5f;
            if (Mathf.Approximately(glyphCenter, 0f)) { corrected = true; return; }

            label.rectTransform.anchoredPosition += new Vector2(0f, -glyphCenter);
            corrected = true;
        }

        private bool TryMeasureFirstLine(out float bottom, out float top)
        {
            TMP_CharacterInfo[] chars = label.textInfo.characterInfo;
            int firstLine = chars[0].lineNumber;
            bottom = float.MaxValue;
            top = float.MinValue;

            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i].lineNumber != firstLine) break;
                if (!chars[i].isVisible) continue;

                bottom = Mathf.Min(bottom, chars[i].bottomLeft.y);
                top = Mathf.Max(top, chars[i].topRight.y);
            }

            if (bottom > top) return false;
            return true;
        }
    }
}
