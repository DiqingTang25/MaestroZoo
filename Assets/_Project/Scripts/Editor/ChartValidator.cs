using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MaestroZoo
{
    public static class ChartValidator
    {
        private static readonly HashSet<string> ValidGestures = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Up",
            "Down",
            "Left",
            "Right",
            "Expand",
            "Close"
        };

        [MenuItem("Maestro Zoo/Validate All Charts")]
        private static void ValidateAllCharts()
        {
            string[] guids = AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets/_Project/Resources/Charts" });
            int validCount = 0;
            int invalidCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (asset == null || !path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (LogValidation(path, asset))
                {
                    validCount++;
                }
                else
                {
                    invalidCount++;
                }
            }

            Debug.Log($"[ChartValidator] Completed. Valid={validCount}, Invalid={invalidCount}");
        }

        [MenuItem("Assets/MaestroZoo/Validate Chart", false, 200)]
        private static void ValidateSelected()
        {
            foreach (UnityEngine.Object obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning($"[ChartValidator] Skipped non-JSON: {path}");
                    continue;
                }

                TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (asset != null)
                {
                    LogValidation(path, asset);
                }
            }
        }

        [MenuItem("Assets/MaestroZoo/Validate Chart", true)]
        private static bool ValidateSelectedValidation()
        {
            foreach (UnityEngine.Object obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static (bool valid, List<string> errors, List<string> warnings) Validate(TextAsset asset)
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();

            if (asset == null)
            {
                errors.Add("TextAsset is null.");
                return (false, errors, warnings);
            }

            ChartData chart;
            try
            {
                chart = JsonUtility.FromJson<ChartData>(asset.text);
            }
            catch (Exception ex)
            {
                errors.Add($"JSON parse failed: {ex.Message}");
                return (false, errors, warnings);
            }

            if (chart == null)
            {
                errors.Add("Deserialized ChartData is null. Check the JSON structure.");
                return (false, errors, warnings);
            }

            ValidateMetadata(chart, warnings, errors);
            ValidateTempoChanges(chart, warnings, errors);
            ValidateNotes(chart, warnings, errors);
            ValidateLengthAndDensity(chart, warnings);

            return (errors.Count == 0, errors, warnings);
        }

        private static bool LogValidation(string path, TextAsset asset)
        {
            (bool valid, List<string> errors, List<string> warnings) = Validate(asset);
            if (valid)
            {
                Debug.Log($"[ChartValidator] VALID: {path} ({warnings.Count} warning(s))");
            }
            else
            {
                Debug.LogError($"[ChartValidator] INVALID: {path} ({errors.Count} error(s))");
            }

            foreach (string error in errors)
            {
                Debug.LogError($"[ChartValidator] {path}: {error}");
            }

            foreach (string warning in warnings)
            {
                Debug.LogWarning($"[ChartValidator] {path}: {warning}");
            }

            return valid;
        }

        private static void ValidateMetadata(ChartData chart, List<string> warnings, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(chart.songName))
            {
                warnings.Add("Song name is empty.");
            }

            if (chart.bpm <= 0 || chart.bpm > 300)
            {
                errors.Add($"BPM={chart.bpm} is outside the expected range (1-300).");
            }

            if (chart.leadTime <= 0f || chart.leadTime > 10f)
            {
                warnings.Add($"Lead time={chart.leadTime}s is unusual (expected 1-10s).");
            }
        }

        private static void ValidateTempoChanges(ChartData chart, List<string> warnings, List<string> errors)
        {
            if (chart.tempoChanges == null || chart.tempoChanges.Length == 0)
                return;

            float lastTime = -1f;
            for (int i = 0; i < chart.tempoChanges.Length; i++)
            {
                TempoChange tc = chart.tempoChanges[i];
                if (tc == null)
                {
                    errors.Add($"tempoChanges[{i}] is null.");
                    continue;
                }

                string prefix = $"TempoChange[{i}] (t={tc.time}s)";

                if (tc.time < 0f)
                    errors.Add($"{prefix} time is negative.");

                if (tc.time < lastTime)
                    errors.Add($"{prefix} time={tc.time} is before previous tempo change ({lastTime}). Must be sorted by time.");

                if (tc.bpm <= 0 || tc.bpm > 300)
                    errors.Add($"{prefix} BPM={tc.bpm} is outside the expected range (1-300).");

                lastTime = tc.time;
            }

            if (chart.tempoChanges.Length > 50)
                warnings.Add($"Chart has {chart.tempoChanges.Length} tempo changes — unusual.");
        }

        private static void ValidateNotes(ChartData chart, List<string> warnings, List<string> errors)
        {
            if (chart.notes == null || chart.notes.Length == 0)
            {
                errors.Add("Chart has no notes.");
                return;
            }

            float lastTime = -1f;
            for (int i = 0; i < chart.notes.Length; i++)
            {
                ChartNote note = chart.notes[i];
                string prefix = $"Note[{i}]";

                if (note == null)
                {
                    errors.Add($"{prefix} is null.");
                    continue;
                }

                if (note.time < 0f)
                {
                    errors.Add($"{prefix} time={note.time} is negative.");
                }

                if (note.time < lastTime)
                {
                    errors.Add($"{prefix} time={note.time} is before previous note ({lastTime}). Notes must be sorted by time.");
                }

                if (lastTime >= 0f && note.time - lastTime < 0.05f)
                {
                    warnings.Add($"{prefix} time={note.time} is very close to previous note ({lastTime}).");
                }

                if (string.IsNullOrWhiteSpace(note.gesture))
                {
                    errors.Add($"{prefix} gesture field is empty.");
                }
                else if (!ValidGestures.Contains(note.gesture))
                {
                    errors.Add($"{prefix} gesture='{note.gesture}' is invalid. Valid gestures: {string.Join(", ", ValidGestures)}");
                }

                if (note.duration < 0f)
                    errors.Add($"{prefix} duration={note.duration} is negative.");
                if (note.duration > 12f)
                    warnings.Add($"{prefix} duration={note.duration}s is very long — unusual for a sustained note.");
                if (note.duration > 0f && note.time + note.duration > chart.GetEndTime() + 2f)
                    warnings.Add($"{prefix} sustained note extends beyond chart end.");

                lastTime = note.time;
            }
        }

        private static void ValidateLengthAndDensity(ChartData chart, List<string> warnings)
        {
            if (chart.notes == null || chart.notes.Length == 0)
            {
                return;
            }

            float endTime = chart.GetEndTime();
            if (endTime < 10f)
            {
                warnings.Add($"Chart is very short ({endTime:F1}s).");
            }

            if (endTime > 600f)
            {
                warnings.Add($"Chart is very long ({endTime:F1}s).");
            }

            float density = chart.notes.Length / Mathf.Max(endTime, 1f);
            if (density > 4f)
            {
                warnings.Add($"Note density is high ({density:F1} notes/sec).");
            }

            if (density < 0.1f)
            {
                warnings.Add($"Note density is very low ({density:F2} notes/sec).");
            }
        }
    }
}
