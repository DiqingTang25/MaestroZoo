using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MaestroZoo
{
    /// <summary>
    /// 谱面 JSON 校验工具。
    /// 在 Project 窗口选中 chart JSON → 右键 → Validate Chart。
    /// 也提供静态方法供 CI/自动化使用。
    /// </summary>
    public static class ChartValidator
    {
        private static readonly HashSet<string> ValidGestures = new(StringComparer.OrdinalIgnoreCase)
        {
            "Up", "Down", "Left", "Right", "Expand", "Close"
        };

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
                if (asset == null) continue;

                var (valid, errors, warnings) = Validate(asset);
                if (valid)
                {
                    Debug.Log($"[ChartValidator] ✅ {path} — VALID ({warnings.Count} warning(s))");
                    foreach (string w in warnings)
                        Debug.LogWarning($"  ⚠ {w}");
                }
                else
                {
                    Debug.LogError($"[ChartValidator] ❌ {path} — {errors.Count} ERROR(S)");
                    foreach (string e in errors)
                        Debug.LogError($"  ❌ {e}");
                    foreach (string w in warnings)
                        Debug.LogWarning($"  ⚠ {w}");
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
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 校验谱面。返回 (是否通过, 错误列表, 警告列表)。
        /// </summary>
        public static (bool valid, List<string> errors, List<string> warnings) Validate(TextAsset asset)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

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
                errors.Add("Deserialized ChartData is null — check JSON structure.");
                return (false, errors, warnings);
            }

            // --- Metadata ---
            if (string.IsNullOrWhiteSpace(chart.songName))
                warnings.Add("Song name is empty.");
            if (chart.bpm <= 0 || chart.bpm > 300)
                errors.Add($"BPM={chart.bpm} is out of reasonable range (1–300).");
            if (chart.leadTime <= 0f || chart.leadTime > 10f)
                warnings.Add($"Lead time={chart.leadTime}s is unusual (expected 1–5s).");

            // --- Notes ---
            if (chart.notes == null || chart.notes.Length == 0)
            {
                errors.Add("Chart has no notes.");
                return (false, errors, warnings);
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
                    errors.Add($"{prefix} time={note.time} is negative.");
                if (note.time < lastTime)
                    errors.Add($"{prefix} time={note.time} is before previous note ({lastTime}). Notes must be sorted by time.");
                if (note.time - lastTime < 0.05f && lastTime >= 0f)
                    warnings.Add($"{prefix} time={note.time} is very close to previous note ({lastTime}). May be impossible to play.");

                if (string.IsNullOrWhiteSpace(note.gesture))
                    errors.Add($"{prefix} gesture field is empty.");
                else if (!ValidGestures.Contains(note.gesture))
                    errors.Add($"{prefix} gesture='{note.gesture}' is not a valid GestureType. Valid: {string.Join(", ", ValidGestures)}");

                lastTime = note.time;
            }

            // --- End time ---
            float endTime = chart.GetEndTime();
            if (endTime < 10f)
                warnings.Add($"Chart is very short ({endTime:F1}s).");
            if (endTime > 600f)
                warnings.Add($"Chart is very long ({endTime:F1}s).");

            // --- Note density ---
            float density = chart.notes.Length / Mathf.Max(endTime, 1f);
            if (density > 4f)
                warnings.Add($"Note density is high ({density:F1} notes/sec). May be too hard.");
            if (density < 0.1f)
                warnings.Add($"Note density is very low ({density:F2} notes/sec). May be too easy.");

            bool valid = errors.Count == 0;
            return (valid, errors, warnings);
        }
    }
}
