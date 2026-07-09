using UnityEngine;

namespace MaestroZoo
{
    public class GestureNoteVisual : MonoBehaviour
    {
        public Renderer targetRenderer;
        public TextMesh label;

        public Color upColor = new Color(0.2f, 0.55f, 1f);
        public Color downColor = new Color(1f, 0.45f, 0.15f);
        public Color leftColor = new Color(0.25f, 0.85f, 0.35f);
        public Color rightColor = new Color(0.7f, 0.35f, 1f);
        public Color expandColor = new Color(1f, 0.85f, 0.15f);
        public Color closeColor = new Color(0.75f, 0.85f, 1f);

        private void Reset()
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        public void Apply(GestureType gesture)
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            if (targetRenderer == null)
            {
                return;
            }

            targetRenderer.material.color = ColorForGesture(gesture);
            ApplyLabel(gesture);
        }

        private void ApplyLabel(GestureType gesture)
        {
            if (label == null)
            {
                Transform existing = transform.Find("Gesture Label");
                if (existing != null)
                {
                    label = existing.GetComponent<TextMesh>();
                }
            }

            if (label == null)
            {
                GameObject labelObject = new GameObject("Gesture Label");
                labelObject.transform.SetParent(transform);
                labelObject.transform.localPosition = new Vector3(0f, 0f, -0.12f);
                labelObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                label = labelObject.AddComponent<TextMesh>();
                label.fontSize = 48;
                label.characterSize = 0.06f;
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.color = Color.black;
            }

            label.text = TextForGesture(gesture);
        }

        private string TextForGesture(GestureType gesture)
        {
            switch (gesture)
            {
                case GestureType.Up:
                    return "UP";
                case GestureType.Down:
                    return "DOWN";
                case GestureType.Left:
                    return "LEFT";
                case GestureType.Right:
                    return "RIGHT";
                case GestureType.Expand:
                    return "OPEN";
                case GestureType.Close:
                    return "CLOSE";
                default:
                    return "?";
            }
        }

        private Color ColorForGesture(GestureType gesture)
        {
            switch (gesture)
            {
                case GestureType.Up:
                    return upColor;
                case GestureType.Down:
                    return downColor;
                case GestureType.Left:
                    return leftColor;
                case GestureType.Right:
                    return rightColor;
                case GestureType.Expand:
                    return expandColor;
                case GestureType.Close:
                    return closeColor;
                default:
                    return Color.white;
            }
        }
    }
}
