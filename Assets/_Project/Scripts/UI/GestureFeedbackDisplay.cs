using UnityEngine;

namespace MaestroZoo
{
    public class GestureFeedbackDisplay : MonoBehaviour
    {
        [Header("References")]
        public GestureInputDispatcher gestureInput;

        [Header("Display Settings")]
        public float displayDuration = 0.5f;
        public float fadeDuration = 0.2f;
        public Vector2 screenOffset = new Vector2(0f, -120f);

        [Header("Gesture Icons (optional)")]
        public Texture2D upIcon;
        public Texture2D downIcon;
        public Texture2D leftIcon;
        public Texture2D rightIcon;
        public Texture2D expandIcon;
        public Texture2D closeIcon;

        private GestureType? activeGesture;
        private float activeStartTime;
        private GUIStyle labelStyle;
        private bool initialized;
        private bool subscribed;

        private void Start()
        {
            if (gestureInput == null)
            {
                gestureInput = FindObjectOfType<GestureInputDispatcher>();
            }

            SubscribeToSources();
        }

        private void OnEnable()
        {
            SubscribeToSources();
        }

        private void OnDisable()
        {
            UnsubscribeFromSources();
        }

        private void SubscribeToSources()
        {
            if (subscribed || gestureInput == null)
            {
                return;
            }

            if (gestureInput.nativeInput != null)
            {
                gestureInput.nativeInput.GestureCaptured += OnGestureCaptured;
            }

            if (gestureInput.handInput != null)
            {
                gestureInput.handInput.GestureCaptured += OnGestureCaptured;
            }

            subscribed = true;
        }

        private void UnsubscribeFromSources()
        {
            if (!subscribed || gestureInput == null)
            {
                return;
            }

            if (gestureInput.nativeInput != null)
            {
                gestureInput.nativeInput.GestureCaptured -= OnGestureCaptured;
            }

            if (gestureInput.handInput != null)
            {
                gestureInput.handInput.GestureCaptured -= OnGestureCaptured;
            }

            subscribed = false;
        }

        private void OnGestureCaptured(GestureType gesture, float time)
        {
            activeGesture = gesture;
            activeStartTime = Time.time;
        }

        private void OnGUI()
        {
            if (!activeGesture.HasValue)
            {
                return;
            }

            float elapsed = Time.time - activeStartTime;
            if (elapsed > displayDuration + fadeDuration)
            {
                activeGesture = null;
                return;
            }

            InitStyles();

            float alpha = 1f;
            if (elapsed > displayDuration)
            {
                alpha = 1f - Mathf.Clamp01((elapsed - displayDuration) / fadeDuration);
            }

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);

            Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
            Vector2 center = screenRect.center + screenOffset;

            Texture2D icon = GestureIcon(activeGesture.Value);
            float iconSize = 96f;
            float textHeight = 44f;
            float totalHeight = icon != null ? iconSize + textHeight + 8f : textHeight;
            float labelY = center.y - totalHeight / 2f;

            if (icon != null)
            {
                Rect iconRect = new Rect(center.x - iconSize / 2f, labelY, iconSize, iconSize);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                labelY += iconSize + 8f;
            }

            Rect labelRect = new Rect(center.x - 140f, labelY, 280f, textHeight);
            GUI.Label(labelRect, GestureLabel(activeGesture.Value), labelStyle);

            GUI.color = previousColor;
        }

        private void InitStyles()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 36,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        private static string GestureLabel(GestureType gesture)
        {
            return gesture switch
            {
                GestureType.Up => "UP",
                GestureType.Down => "DOWN",
                GestureType.Left => "LEFT",
                GestureType.Right => "RIGHT",
                GestureType.Expand => "EXPAND",
                GestureType.Close => "CLOSE",
                _ => "?"
            };
        }

        private Texture2D GestureIcon(GestureType gesture)
        {
            return gesture switch
            {
                GestureType.Up => upIcon,
                GestureType.Down => downIcon,
                GestureType.Left => leftIcon,
                GestureType.Right => rightIcon,
                GestureType.Expand => expandIcon,
                GestureType.Close => closeIcon,
                _ => null
            };
        }
    }
}
