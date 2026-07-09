using System;
using UnityEngine;

namespace MaestroZoo
{
    /// <summary>
    /// 校准协调器 — 统一管理音频延迟校准和个性化手势校准的完整流程。
    /// Bridges ChartPlayer.RegisterCalibrationTap with gesture input during calibration mode.
    /// </summary>
    public class CalibrationCoordinator : MonoBehaviour
    {
        [Header("References")]
        public ChartPlayer chartPlayer;
        public PersonalGestureCalibrator gestureCalibrator;
        public IGestureInput gestureInput; // Set in Awake from GestureInputDispatcher

        [Header("Persistence")]
        public string prefsKeyLatencyOffset = "maestro.latency_offset";
        public string prefsKeyGestureThreshold = "maestro.gesture_threshold";

        /// <summary>Current calibration mode (null = not calibrating).</summary>
        public CalibrationMode? ActiveMode { get; private set; }

        public enum CalibrationMode { AudioLatency, GesturePersonal }

        // --- Events for UI ---
        public event Action<CalibrationMode> CalibrationStarted;
        public event Action<CalibrationMode, string> CalibrationCompleted;
        public event Action<CalibrationMode, float> ProgressChanged; // 0-1
        public event Action<string> StatusMessageChanged;

        // Tap debounce
        private float lastTapTime = -10f;
        private const float TapCooldown = 0.25f;

        private void Awake()
        {
            if (chartPlayer == null)
                chartPlayer = GetComponent<ChartPlayer>();
            if (gestureCalibrator == null)
                gestureCalibrator = GetComponent<PersonalGestureCalibrator>();

            // Resolve gesture input
            if (gestureInput == null)
            {
                var dispatcher = GetComponent<GestureInputDispatcher>();
                if (dispatcher != null)
                    gestureInput = dispatcher as IGestureInput;
            }
        }

        private void Start()
        {
            // Auto-load saved calibration on game start
            LoadAndApply();
        }

        private void Update()
        {
            // Route gestures during audio latency calibration
            if (ActiveMode == CalibrationMode.AudioLatency && gestureInput != null)
            {
                while (gestureInput.TryConsumeGesture(out GestureType gesture, out _))
                {
                    // Any gesture counts as a tap during audio latency calibration
                    if (gesture == GestureType.Down && Time.time - lastTapTime > TapCooldown)
                    {
                        lastTapTime = Time.time;
                        chartPlayer.RegisterCalibrationTap();
                    }
                }
            }
        }

        private void OnEnable()
        {
            if (gestureCalibrator != null)
            {
                gestureCalibrator.Completed += HandleGestureCalibrationCompleted;
                gestureCalibrator.StepChanged += HandleGestureCalibrationStepChanged;
            }
        }

        private void OnDisable()
        {
            if (gestureCalibrator != null)
            {
                gestureCalibrator.Completed -= HandleGestureCalibrationCompleted;
                gestureCalibrator.StepChanged -= HandleGestureCalibrationStepChanged;
            }
        }

        // ══════════════════════════════════════════════════════
        //  Public API — called from UI buttons / debug panel
        // ══════════════════════════════════════════════════════

        /// <summary>Start audio latency calibration. Plays metronome, captures taps.</summary>
        public void StartAudioLatencyCalibration()
        {
            if (chartPlayer == null)
            {
                StatusMessageChanged?.Invoke("ChartPlayer 未配置 (ChartPlayer not assigned)");
                return;
            }

            ActiveMode = CalibrationMode.AudioLatency;
            lastTapTime = -10f;
            chartPlayer.StartLatencyCalibration();
            CalibrationStarted?.Invoke(CalibrationMode.AudioLatency);
            ProgressChanged?.Invoke(CalibrationMode.AudioLatency, 0f);
            StatusMessageChanged?.Invoke("听节拍，重拍时做 Down 手势 (Tap on the beat with Down gesture)");

            // Auto-finish happens in ChartPlayer.UpdateCalibrationBeats()
            // Hook into completion by checking chartPlayer.latencyOffset change
            StartCoroutine(WatchAudioCalibrationComplete());
        }

        private System.Collections.IEnumerator WatchAudioCalibrationComplete()
        {
            float startTime = Time.time;
            float timeout = 15f; // Max 15s wait

            while (Time.time - startTime < timeout)
            {
                yield return new WaitForSeconds(0.3f);

                if (chartPlayer == null)
                    yield break;

                if (chartPlayer.LatencyCalibrated)
                {
                    float offset = chartPlayer.latencyOffset;
                    SaveLatencyOffset(offset);
                    StatusMessageChanged?.Invoke(
                        $"音频延迟校准完成: {offset * 1000f:F0}ms (Audio latency: {offset * 1000f:F0}ms)");
                    ProgressChanged?.Invoke(CalibrationMode.AudioLatency, 1f);
                    CalibrationCompleted?.Invoke(CalibrationMode.AudioLatency,
                        $"延迟 {offset * 1000f:F0}ms");
                    ActiveMode = null;
                    yield break;
                }
            }

            StatusMessageChanged?.Invoke("校准超时，请重试 (Calibration timed out, retry)");
            ActiveMode = null;
        }

        /// <summary>Start personal gesture calibration. Guides through 4-direction wave.</summary>
        public void StartGestureCalibration()
        {
            if (gestureCalibrator == null)
            {
                StatusMessageChanged?.Invoke("PersonalGestureCalibrator 未配置");
                return;
            }

            ActiveMode = CalibrationMode.GesturePersonal;
            gestureCalibrator.Begin();
            CalibrationStarted?.Invoke(CalibrationMode.GesturePersonal);
            ProgressChanged?.Invoke(CalibrationMode.GesturePersonal, 0f);
        }

        /// <summary>Cancel any active calibration.</summary>
        public void CancelCalibration()
        {
            if (ActiveMode == CalibrationMode.GesturePersonal && gestureCalibrator != null)
                gestureCalibrator.Cancel();

            ActiveMode = null;
            StatusMessageChanged?.Invoke("校准已取消 (Calibration cancelled)");
        }

        // ══════════════════════════
        //  Persistence
        // ══════════════════════════

        /// <summary>Load saved calibration values and apply them.</summary>
        public void LoadAndApply()
        {
            // Load latency offset
            if (PlayerPrefs.HasKey(prefsKeyLatencyOffset))
            {
                float savedOffset = PlayerPrefs.GetFloat(prefsKeyLatencyOffset);
                if (chartPlayer != null)
                    chartPlayer.SetLatencyOffset(savedOffset);
                Debug.Log($"[CalibCoord] Loaded latency offset: {savedOffset * 1000f:F0}ms");
            }

            // Load gesture threshold
            if (PlayerPrefs.HasKey(prefsKeyGestureThreshold))
            {
                float savedThreshold = PlayerPrefs.GetFloat(prefsKeyGestureThreshold);
                var gestureInput = gestureCalibrator?.gestureInput;
                if (gestureInput != null)
                    gestureInput.moveThreshold = savedThreshold;
                Debug.Log($"[CalibCoord] Loaded gesture threshold: {savedThreshold * 1000f:F1}m");
            }
        }

        public void SaveLatencyOffset(float offset)
        {
            PlayerPrefs.SetFloat(prefsKeyLatencyOffset, offset);
            PlayerPrefs.Save();
            Debug.Log($"[CalibCoord] Saved latency offset: {offset * 1000f:F0}ms");
        }

        public void SaveGestureThreshold(float threshold)
        {
            PlayerPrefs.SetFloat(prefsKeyGestureThreshold, threshold);
            PlayerPrefs.Save();
            Debug.Log($"[CalibCoord] Saved gesture threshold: {threshold * 1000f:F1}m");
        }

        /// <summary>Reset all saved calibration to defaults.</summary>
        public void ResetAll()
        {
            PlayerPrefs.DeleteKey(prefsKeyLatencyOffset);
            PlayerPrefs.DeleteKey(prefsKeyGestureThreshold);
            PlayerPrefs.Save();

            if (chartPlayer != null)
            {
                chartPlayer.latencyOffset = 0f;
                chartPlayer.SetLatencyOffset(0f);
            }

            if (gestureCalibrator?.gestureInput != null)
            {
                gestureCalibrator.gestureInput.moveThreshold = 0.12f; // Default
            }

            Debug.Log("[CalibCoord] All calibration reset to defaults.");
        }

        // ══════════════════════════
        //  Callbacks
        // ══════════════════════════

        private void HandleGestureCalibrationStepChanged(PersonalGestureCalibrator.Step step)
        {
            float progress = gestureCalibrator != null ? gestureCalibrator.OverallProgress : 0f;
            ProgressChanged?.Invoke(CalibrationMode.GesturePersonal, progress);
            StatusMessageChanged?.Invoke(gestureCalibrator != null
                ? gestureCalibrator.CurrentInstruction : "");
        }

        private void HandleGestureCalibrationCompleted(CalibrationResult result)
        {
            ActiveMode = null;

            if (result.success && gestureCalibrator?.gestureInput != null)
            {
                SaveGestureThreshold(gestureCalibrator.gestureInput.moveThreshold);
            }

            ProgressChanged?.Invoke(CalibrationMode.GesturePersonal, 1f);
            StatusMessageChanged?.Invoke(result.message);
            CalibrationCompleted?.Invoke(CalibrationMode.GesturePersonal, result.message);
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}
