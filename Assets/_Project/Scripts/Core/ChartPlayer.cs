using System;
using UnityEngine;

namespace MaestroZoo
{
    public class ChartPlayer : MonoBehaviour
    {
        [Header("Chart")]
        public TextAsset chartJson;
        public AudioSource musicSource;

        [Header("Latency Compensation")]
        [Tooltip("Audio output latency in seconds. Positive = user hears audio later than DSP time. " +
                 "Run Latency Calibration in the context menu to auto-measure.")]
        public float latencyOffset = 0f;

        [Tooltip("Min expected latency (seconds). Values below this suggest a measurement error.")]
        public float minExpectedLatency = 0.01f;

        [Tooltip("Max expected latency (seconds). Values above this suggest a measurement error.")]
        public float maxExpectedLatency = 0.25f;

        [Header("Fallback Audio")]
        public bool generatePlaceholderAudio = true;
        public float placeholderVolume = 0.18f;
        public int placeholderSampleRate = 24000;

        public ChartData Chart { get; private set; }

        /// <summary>Raw song position from AudioSettings.dspTime (no latency compensation).</summary>
        public float SongTime { get; private set; }

        /// <summary>Song position compensated for audio output latency. Use this for note spawning and judgment.</summary>
        public float CompensatedSongTime => Mathf.Max(0f, SongTime - latencyOffset);

        public bool IsPlaying { get; private set; }

        /// <summary>True when playback is paused (not stopped — can resume).</summary>
        public bool IsPaused { get; private set; }

        /// <summary>Current BPM at the current song position, accounting for tempo changes.</summary>
        public int CurrentBpm => Chart != null ? Chart.GetBpmAtTime(SongTime) : 120;

        public float ChartEndTime { get; private set; }

        /// <summary>True if latency calibration has been completed this session.</summary>
        public bool LatencyCalibrated { get; private set; }

        public event Action<ChartData> ChartLoaded;
        public event Action PlaybackStarted;
        public event Action PlaybackStopped;
        public event Action PlaybackPaused;
        public event Action PlaybackResumed;
        public event Action PlaybackEnded;

        private float startDspTime;
        private float pauseDspTime;   // DSP time when paused (for resuming)
        private float pauseOffset;     // Accumulated pause duration
        private bool endRaised;

        private void Awake()
        {
            if (chartJson != null)
            {
                LoadChart();
            }
        }

        private void Update()
        {
            UpdateCalibration();

            if (!IsPlaying || IsPaused)
            {
                return;
            }

            // SongTime accounts for pause gaps: total DSP elapsed minus accumulated pause duration
            float elapsed = (float)(AudioSettings.dspTime - startDspTime);
            SongTime = Mathf.Max(0f, elapsed - pauseOffset);

            if (!endRaised && Chart != null && SongTime >= ChartEndTime + 1f)
            {
                endRaised = true;
                IsPlaying = false;
                if (musicSource != null)
                {
                    musicSource.Stop();
                }
                PlaybackEnded?.Invoke();
            }
        }

        [ContextMenu("Load Chart")]
        public void LoadChart()
        {
            if (chartJson == null)
            {
                Debug.LogWarning("ChartPlayer needs a chart JSON file.");
                return;
            }

            Chart = JsonUtility.FromJson<ChartData>(chartJson.text);
            ChartEndTime = Chart != null ? Chart.GetEndTime() : 0f;
            ChartLoaded?.Invoke(Chart);
        }

        public void LoadChart(TextAsset nextChart)
        {
            chartJson = nextChart;
            LoadChart();
        }

        [ContextMenu("Start Song")]
        public void StartSong()
        {
            if (Chart == null)
            {
                LoadChart();
            }

            if (Chart == null)
            {
                return;
            }

            startDspTime = (float)AudioSettings.dspTime;
            SongTime = 0f;
            IsPlaying = true;
            endRaised = false;

            if (musicSource != null && musicSource.clip != null)
            {
                musicSource.Stop();
                musicSource.Play();
            }
            else if (generatePlaceholderAudio && musicSource != null)
            {
                musicSource.Stop();
                musicSource.clip = CreatePlaceholderClip();
                musicSource.Play();
            }

            PlaybackStarted?.Invoke();
        }

        public void StartSong(TextAsset nextChart)
        {
            LoadChart(nextChart);
            StartSong();
        }

        /// <summary>Start playback with a runtime-constructed chart (used by tutorial system).</summary>
        public void StartSong(ChartData chartData)
        {
            if (chartData == null)
            {
                Debug.LogWarning("[ChartPlayer] Cannot start: null ChartData.");
                return;
            }

            Chart = chartData;
            ChartEndTime = Chart.GetEndTime();
            ChartLoaded?.Invoke(Chart);
            StartSong();
        }

        public void StopSong()
        {
            IsPlaying = false;
            IsPaused = false;
            SongTime = 0f;
            endRaised = false;
            pauseOffset = 0f;
            pauseDspTime = 0f;

            if (musicSource != null)
            {
                musicSource.Stop();
            }

            PlaybackStopped?.Invoke();
        }

        /// <summary>Pause playback. Audio stops, song time freezes. Can Resume later.</summary>
        public void PauseSong()
        {
            if (!IsPlaying || IsPaused) return;

            IsPaused = true;
            pauseDspTime = (float)AudioSettings.dspTime;

            if (musicSource != null)
            {
                musicSource.Pause();
            }

            Debug.Log($"[ChartPlayer] Paused at SongTime={SongTime:F2}s");
            PlaybackPaused?.Invoke();
        }

        /// <summary>Resume playback from where it was paused.</summary>
        public void ResumeSong()
        {
            if (!IsPlaying || !IsPaused) return;

            // Accumulate the pause duration so SongTime doesn't jump forward
            float pauseDuration = (float)(AudioSettings.dspTime - pauseDspTime);
            pauseOffset += pauseDuration;

            IsPaused = false;

            if (musicSource != null)
            {
                musicSource.UnPause();
            }

            Debug.Log($"[ChartPlayer] Resumed at SongTime={SongTime:F2}s (pause was {pauseDuration * 1000f:F0}ms)");
            PlaybackResumed?.Invoke();
        }

        private AudioClip CreatePlaceholderClip()
        {
            float duration = Mathf.Max(ChartEndTime + 1f, 4f);
            int sampleCount = Mathf.CeilToInt(duration * placeholderSampleRate);
            float[] samples = new float[sampleCount];
            float beatInterval = Chart != null && Chart.bpm > 0 ? 60f / Chart.bpm : 0.5f;
            float strongBeatInterval = beatInterval * 4f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / placeholderSampleRate;
                float beatPhase = Mathf.Repeat(t, beatInterval);
                float strongPhase = Mathf.Repeat(t, strongBeatInterval);
                float envelope = Mathf.Exp(-beatPhase * 28f);
                bool strongBeat = strongPhase < beatInterval * 0.35f;
                float frequency = strongBeat ? 880f : 660f;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * placeholderVolume;
            }

            AudioClip clip = AudioClip.Create("Generated Placeholder Beat", sampleCount, 1, placeholderSampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Set the latency offset directly. Clamped to a reasonable range.
        /// Use the result from a manual calibration or device-specific preset.
        /// </summary>
        public void SetLatencyOffset(float offsetSeconds)
        {
            latencyOffset = Mathf.Clamp(offsetSeconds, 0f, maxExpectedLatency);
            LatencyCalibrated = true;
            Debug.Log($"[ChartPlayer] Latency offset set to {latencyOffset * 1000f:F0} ms");
        }

        /// <summary>
        /// Start a latency calibration beat sequence. The user should tap/clap
        /// on each beat. After calibration beats complete, call FinishLatencyCalibration.
        /// </summary>
        [ContextMenu("Latency Calibration")]
        public void StartLatencyCalibration()
        {
            if (musicSource == null)
            {
                Debug.LogError("[ChartPlayer] Cannot calibrate: no AudioSource assigned.");
                return;
            }

            StopCalibrationBeats();
            calibrationTapTimes.Clear();
            calibrationBeatCount = 8;
            calibrationBeatInterval = 0.5f; // 120 BPM calibration beat
            calibrationNextBeatTime = (float)AudioSettings.dspTime + 0.5f; // Start after 500ms
            calibrationNextBeatIndex = 0;
            calibrationRunning = true;

            Debug.Log("[ChartPlayer] Latency calibration started. Tap/gesture on each beat. " +
                      $"{calibrationBeatCount} beats at {60f / calibrationBeatInterval:F0} BPM.");
        }

        /// <summary>
        /// Register a tap/gesture for latency calibration. Call this from your input handler
        /// when the user makes a deliberate tap (e.g., Down gesture detected).
        /// </summary>
        public void RegisterCalibrationTap()
        {
            if (!calibrationRunning) return;
            float tapTime = (float)AudioSettings.dspTime;
            if (calibrationTapTimes.Count > 0 && tapTime - calibrationTapTimes[calibrationTapTimes.Count - 1] < 0.15f)
            {
                // Debounce: ignore taps too close together
                return;
            }
            calibrationTapTimes.Add(tapTime);
            Debug.Log($"[ChartPlayer] Calibration tap #{calibrationTapTimes.Count} at DSP {tapTime:F3}");
        }

        /// <summary>
        /// Finish latency calibration and compute the recommended offset from collected taps.
        /// Returns the measured offset in seconds, or -1 if not enough data.
        /// </summary>
        public float FinishLatencyCalibration()
        {
            calibrationRunning = false;

            if (calibrationTapTimes.Count < 3)
            {
                Debug.LogWarning("[ChartPlayer] Latency calibration failed: not enough taps collected " +
                                 $"({calibrationTapTimes.Count} taps, need at least 3).");
                return -1f;
            }

            // Calculate offset: difference between expected beat time and actual tap time
            // Expected beat: calibrationStartTime + beatIndex * beatInterval
            // Offset = avg(tapTime - expectedBeatTime)
            float totalOffset = 0f;
            int matchedTaps = 0;

            foreach (float tapTime in calibrationTapTimes)
            {
                // Find which beat this tap is closest to
                float bestOffset = float.MaxValue;
                for (int i = 0; i < calibrationBeatCount; i++)
                {
                    float expected = calibrationStartTime + i * calibrationBeatInterval;
                    float offset = tapTime - expected;
                    if (Mathf.Abs(offset) < Mathf.Abs(bestOffset))
                    {
                        bestOffset = offset;
                    }
                }

                // Only count taps within reasonable range (half a beat)
                if (Mathf.Abs(bestOffset) < calibrationBeatInterval * 0.5f)
                {
                    totalOffset += bestOffset;
                    matchedTaps++;
                }
            }

            if (matchedTaps < 3)
            {
                Debug.LogWarning($"[ChartPlayer] Latency calibration failed: only {matchedTaps} taps matched beats.");
                return -1f;
            }

            float avgOffset = totalOffset / matchedTaps;

            if (avgOffset < minExpectedLatency && avgOffset >= 0f)
            {
                Debug.Log($"[ChartPlayer] Measured latency is very low ({avgOffset * 1000f:F1} ms). " +
                          "Using measured value but you may want to verify.");
            }

            latencyOffset = Mathf.Clamp(avgOffset, 0f, maxExpectedLatency);
            LatencyCalibrated = true;
            calibrationStartTime = 0f;

            Debug.Log($"[ChartPlayer] Latency calibration complete: {latencyOffset * 1000f:F1} ms " +
                      $"(from {matchedTaps} taps, raw avg={avgOffset * 1000f:F1} ms).");

            return latencyOffset;
        }

        // --- Calibration internal state ---
        private bool calibrationRunning;
        private int calibrationBeatCount;
        private float calibrationBeatInterval;
        private float calibrationNextBeatTime;
        private float calibrationStartTime;
        private int calibrationNextBeatIndex;
        private readonly System.Collections.Generic.List<float> calibrationTapTimes = new System.Collections.Generic.List<float>();

        private void UpdateCalibrationBeats()
        {
            if (!calibrationRunning) return;

            float now = (float)AudioSettings.dspTime;

            // Play calibration beats
            while (calibrationNextBeatIndex < calibrationBeatCount && now >= calibrationNextBeatTime)
            {
                if (calibrationNextBeatIndex == 0)
                {
                    calibrationStartTime = calibrationNextBeatTime;
                }

                PlayCalibrationClick(calibrationNextBeatIndex == 0);
                calibrationNextBeatTime += calibrationBeatInterval;
                calibrationNextBeatIndex++;
            }

            // Check if all beats played and enough time passed for last tap
            if (calibrationNextBeatIndex >= calibrationBeatCount &&
                now > calibrationStartTime + calibrationBeatCount * calibrationBeatInterval + 1f)
            {
                float result = FinishLatencyCalibration();
                if (result < 0f)
                {
                    // Auto-retry once if calibration failed
                    Debug.Log("[ChartPlayer] Auto-retrying latency calibration...");
                    StartLatencyCalibration();
                }
            }
        }

        private void PlayCalibrationClick(bool first)
        {
            // Play a short click sound via the audio source
            // Use PlayClipAtPoint for a simple click, or generate a burst
            if (musicSource != null)
            {
                // Burst a short sine tone at the calibration beat time
                StartCoroutine(PlayClickCoroutine(calibrationNextBeatTime, first));
            }
        }

        private System.Collections.IEnumerator PlayClickCoroutine(double scheduledTime, bool accent)
        {
            // Wait until just before the scheduled time
            while (AudioSettings.dspTime < scheduledTime - 0.01f)
            {
                yield return null;
            }

            // Create a short click tone
            int sampleRate = 24000;
            int clickSamples = Mathf.CeilToInt(0.06f * sampleRate); // 60ms click
            float[] clickData = new float[clickSamples];
            float freq = accent ? 1200f : 900f;
            for (int i = 0; i < clickSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 60f);
                clickData[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.3f;
            }

            AudioClip click = AudioClip.Create("CalibClick", clickSamples, 1, sampleRate, false);
            click.SetData(clickData, 0);
            musicSource.PlayOneShot(click, 0.8f);
        }

        private void StopCalibrationBeats()
        {
            calibrationRunning = false;
            calibrationTapTimes.Clear();
            calibrationNextBeatIndex = 0;
        }

        private void OnDestroy()
        {
            StopCalibrationBeats();
        }

        // Hook calibration updates into the main Update loop
        private void UpdateCalibration()
        {
            if (calibrationRunning)
            {
                UpdateCalibrationBeats();
            }
        }
    }
}
