using UnityEngine;

namespace MaestroZoo
{
    public class ChartDebugPanel : MonoBehaviour
    {
        public MaestroGameDirector director;
        public ChartPlayer chartPlayer;
        public NoteSpawner noteSpawner;
        public JudgeManager judgeManager;
        public GestureInputDispatcher gestureInput;

        [Header("Display")]
        public bool visible = true;
        public int fontSize = 18;
        public float panelAlpha = 0.72f;
        public Vector2 panelPosition = new Vector2(16f, 520f);

        private GUIStyle headerStyle;
        private GUIStyle bodyStyle;
        private bool stylesBuilt;

        private void Start()
        {
            if (director == null) director = GetComponent<MaestroGameDirector>();
            if (chartPlayer == null) chartPlayer = GetComponent<ChartPlayer>();
            if (noteSpawner == null) noteSpawner = GetComponent<NoteSpawner>();
            if (judgeManager == null) judgeManager = GetComponent<JudgeManager>();
            if (gestureInput == null) gestureInput = GetComponent<GestureInputDispatcher>();
        }

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            BuildStyles();

            float lineHeight = fontSize * 1.45f;
            float x = panelPosition.x;
            float y = panelPosition.y;
            Rect boxRect = new Rect(x - 8f, y - 8f, 560f, lineHeight * 16f + 20f);
            DrawBackground(boxRect);
            GUI.Box(boxRect, "");

            GUI.Label(new Rect(x, y, 520f, lineHeight), "=== CHART DEBUG ===", headerStyle);
            y += lineHeight;

            ChartData chart = chartPlayer != null ? chartPlayer.Chart : null;
            DrawLine(x, ref y, lineHeight, "Mode", director != null ? director.Mode.ToString() : "?");
            DrawLine(x, ref y, lineHeight, "Chart", chart != null ? chart.songName : "(not loaded)");
            DrawLine(x, ref y, lineHeight, "Audio", GetAudioName());

            if (chartPlayer != null)
            {
                DrawLine(x, ref y, lineHeight, "Time",
                    $"{chartPlayer.CompensatedSongTime:F2}s / {chartPlayer.ChartEndTime:F2}s  raw:{chartPlayer.SongTime:F2}s");
                DrawLine(x, ref y, lineHeight, "Latency", $"{chartPlayer.latencyOffset * 1000f:F0} ms");
            }

            if (chart != null)
            {
                DrawLine(x, ref y, lineHeight, "BPM / Notes", $"{chart.bpm} / {CountNotes(chart)}");
                DrawLine(x, ref y, lineHeight, "Next Notes", BuildNextNotes(chart));
            }

            int activeCount = noteSpawner != null && noteSpawner.ActiveNotes != null ? noteSpawner.ActiveNotes.Count : 0;
            DrawLine(x, ref y, lineHeight, "Active Notes", activeCount.ToString());

            if (judgeManager != null)
            {
                DrawLine(x, ref y, lineHeight, "Score", $"{judgeManager.Score}  combo:{judgeManager.Combo} max:{judgeManager.MaxCombo}");
                DrawLine(x, ref y, lineHeight, "Judgement",
                    $"P:{judgeManager.PerfectCount} G:{judgeManager.GoodCount} M:{judgeManager.MissCount} W:{judgeManager.WrongGestureCount}");
                DrawLine(x, ref y, lineHeight, "Accuracy", $"{judgeManager.Accuracy * 100f:F0}%");
            }

            DrawLine(x, ref y, lineHeight, "Input", gestureInput != null ? gestureInput.ActiveSourceName : "?");
        }

        private string GetAudioName()
        {
            if (chartPlayer == null || chartPlayer.musicSource == null || chartPlayer.musicSource.clip == null)
            {
                return "(none)";
            }

            return chartPlayer.musicSource.clip.name;
        }

        private string BuildNextNotes(ChartData chart)
        {
            if (chart == null || chart.notes == null || chartPlayer == null)
            {
                return "(none)";
            }

            float now = chartPlayer.CompensatedSongTime;
            string result = "";
            int count = 0;
            for (int i = 0; i < chart.notes.Length && count < 4; i++)
            {
                ChartNote note = chart.notes[i];
                if (note == null || note.time < now)
                {
                    continue;
                }

                if (result.Length > 0)
                {
                    result += "  ";
                }

                result += $"{note.time:F1}:{note.gesture}";
                count++;
            }

            return result.Length > 0 ? result : "(ending)";
        }

        private static int CountNotes(ChartData chart)
        {
            return chart != null && chart.notes != null ? chart.notes.Length : 0;
        }

        private void DrawLine(float x, ref float y, float lineHeight, string label, string value)
        {
            GUI.Label(new Rect(x, y, 150f, lineHeight), label, bodyStyle);
            GUI.Label(new Rect(x + 160f, y, 380f, lineHeight), value, bodyStyle);
            y += lineHeight;
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
            if (stylesBuilt)
            {
                return;
            }

            stylesBuilt = true;
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize + 3,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                normal = { textColor = Color.white }
            };
        }
    }
}
