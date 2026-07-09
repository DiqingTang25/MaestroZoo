using UnityEngine;
using Rokid.UXR.Interaction;

namespace MaestroZoo
{
    /// <summary>
    /// 比赛真机调试面板。
    /// OnGUI 实现，无需 Canvas，直接显示手部追踪状态和手势识别信息。
    /// 挂在 GameDirector 同一 GameObject 上即可。
    /// </summary>
    public class RokidDebugPanel : MonoBehaviour
    {
        [Header("References")]
        public GestureInputDispatcher dispatcher;
        public RokidNativeGestureInput nativeInput;
        public RokidHandGestureInput handInput;

        [Header("Display")]
        public bool showOnDevice = true;
        public int fontSize = 20;
        public float panelAlpha = 0.75f;

        // --- Internal ---
        private GUIStyle headerStyle;
        private GUIStyle bodyStyle;
        private GUIStyle warnStyle;
        private bool stylesBuilt;

        private void Start()
        {
            if (dispatcher == null)
                dispatcher = GetComponent<GestureInputDispatcher>();
            if (nativeInput == null)
                nativeInput = GetComponent<RokidNativeGestureInput>();
            if (handInput == null)
                handInput = GetComponent<RokidHandGestureInput>();
        }

        private void OnGUI()
        {
            BuildStyles();

            float lineHeight = fontSize * 1.5f;
            float panelX = 16f;
            float panelY = 16f;

            // Background box
            float boxW = 480f;
            float boxH = lineHeight * 20f + 20f;
            Rect boxRect = new Rect(panelX - 8f, panelY - 8f, boxW, boxH);
            GUI.Box(boxRect, "");
            DrawBackground(boxRect);

            panelY = DrawHeader(panelX, panelY, lineHeight);

            // --- Source ---
            string sourceLabel = dispatcher != null ? dispatcher.ActiveSourceName : "?";
            Color sourceColor = sourceLabel switch
            {
                "RokidNative" => Color.green,
                "XRHand" => new Color(1f, 0.65f, 0f), // orange
                _ => Color.red
            };
            DrawLine(panelX, ref panelY, lineHeight, "Active Source", sourceLabel, sourceColor);

            // --- Native SDK Status ---
            bool gesAvailable = GesEventInput.Instance != null;
            DrawLine(panelX, ref panelY, lineHeight, "GesEventInput",
                gesAvailable ? "INITIALIZED" : "NOT FOUND",
                gesAvailable ? Color.green : Color.red);

            // --- Hand Tracking ---
            if (nativeInput != null)
            {
                DrawLine(panelX, ref panelY, lineHeight, "Tracking Available",
                    nativeInput.IsTrackingAvailable ? "YES" : "NO",
                    nativeInput.IsTrackingAvailable ? Color.green : Color.red);

                DrawLine(panelX, ref panelY, lineHeight, "Left Hand",
                    nativeInput.IsLeftHandTracked ? "TRACKED" : "LOST",
                    nativeInput.IsLeftHandTracked ? Color.green : Color.red);

                if (nativeInput.IsLeftHandTracked)
                {
                    Vector3 pos = nativeInput.LeftHandPosition;
                    DrawLine(panelX, ref panelY, lineHeight, "  Pos (L)",
                        $"({pos.x:F2}, {pos.y:F2}, {pos.z:F2})", Color.white);
                }

                DrawLine(panelX, ref panelY, lineHeight, "Right Hand",
                    nativeInput.IsRightHandTracked ? "TRACKED" : "LOST",
                    nativeInput.IsRightHandTracked ? Color.green : Color.red);

                if (nativeInput.IsRightHandTracked)
                {
                    Vector3 pos = nativeInput.RightHandPosition;
                    DrawLine(panelX, ref panelY, lineHeight, "  Pos (R)",
                        $"({pos.x:F2}, {pos.y:F2}, {pos.z:F2})", Color.white);
                }

                // --- Metrics ---
                DrawLine(panelX, ref panelY, lineHeight, "Pinch (L)",
                    nativeInput.LeftPinchDistance.ToString("F4"), Color.white);
                DrawLine(panelX, ref panelY, lineHeight, "Pinch (R)",
                    nativeInput.RightPinchDistance.ToString("F4"), Color.white);
                DrawLine(panelX, ref panelY, lineHeight, "Two-Hand Dist",
                    nativeInput.TwoHandDistance.ToString("F3"), Color.white);
            }

            // --- XRHand Fallback ---
            if (handInput != null)
            {
                DrawLine(panelX, ref panelY, lineHeight, "XRHandSubsystem",
                    handInput.IsTrackingAvailable ? "RUNNING" : "OFF",
                    handInput.IsTrackingAvailable ? new Color(1f, 0.65f, 0f) : Color.gray);
            }

            // --- Last Gesture ---
            string lastGes = "—";
            float lastTime = 0f;
            float lastConf = 0f;
            if (nativeInput != null)
            {
                lastGes = nativeInput.LastGesture.ToString();
                lastTime = nativeInput.LastGestureTimestamp;
                lastConf = nativeInput.LastConfidence;
            }
            DrawLine(panelX, ref panelY, lineHeight, "Last Gesture",
                lastTime > 0f ? $"{lastGes} @ {lastTime:F2}s (conf:{lastConf:F2})" : "(none)",
                Color.cyan);

            // --- Gesture History (last 5) ---
            if (nativeInput != null && nativeInput.GestureHistory.Count > 0)
            {
                int count = Mathf.Min(nativeInput.GestureHistory.Count, 5);
                for (int i = 0; i < count; i++)
                {
                    var rec = nativeInput.GestureHistory[i];
                    string prefix = i == 0 ? "History" : "";
                    Color c = i == 0 ? Color.cyan : new Color(0.5f, 0.7f, 0.7f);
                    DrawLine(panelX, ref panelY, lineHeight, prefix,
                        $"{rec.gesture} @ {rec.time:F2}s (conf:{rec.confidence:F2})", c);
                }
            }

            // --- Calibration ---
            if (nativeInput != null)
            {
                string calibLabel = nativeInput.IsCalibrating ? "RUNNING..." : "IDLE";
                Color calibColor = nativeInput.IsCalibrating ? Color.yellow : Color.gray;
                DrawLine(panelX, ref panelY, lineHeight, "Calibration", calibLabel, calibColor);
            }
        }

        private float DrawHeader(float x, float y, float lineH)
        {
            Rect r = new Rect(x, y, 400f, lineH);
            GUI.Label(r, "=== ROKID DEBUG PANEL ===", headerStyle);
            return y + lineH;
        }

        private void DrawLine(float x, ref float y, float lineH,
            string label, string value, Color valueColor)
        {
            Rect labelRect = new Rect(x, y, 180f, lineH);
            Rect valueRect = new Rect(x + 190f, y, 270f, lineH);

            GUI.Label(labelRect, label, bodyStyle);
            Color prev = GUI.color;
            GUI.color = valueColor;
            GUI.Label(valueRect, value, bodyStyle);
            GUI.color = prev;

            y += lineH;
        }

        private void DrawBackground(Rect rect)
        {
            Color prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, panelAlpha);
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = prev;
        }

        private void BuildStyles()
        {
            if (stylesBuilt) return;
            stylesBuilt = true;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize + 4,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Normal,
                normal = { textColor = Color.white }
            };

            warnStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.red }
            };
        }
    }
}
