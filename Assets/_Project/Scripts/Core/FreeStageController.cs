using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaestroZoo
{
    /// <summary>
    /// 自由演奏模式增强 — 节拍器 + 可选手势提示 + 即兴评分。
    /// FreeStage: metronome, optional gesture suggestions, improvisation scoring.
    /// </summary>
    public class FreeStageController : MonoBehaviour
    {
        [Header("References")]
        public MaestroGameDirector gameDirector;
        public OrchestraController orchestra;
        public AudioSource metronomeSource; // Separate AudioSource for clicks

        [Header("Metronome")]
        [Tooltip("Default BPM for the metronome.")]
        [Range(40, 240)]
        public int defaultBpm = 120;

        [Tooltip("Beats per measure (4 = 4/4 time). Strong beat on beat 1.")]
        [Range(1, 8)]
        public int beatsPerMeasure = 4;

        [Tooltip("Volume of the metronome click.")]
        [Range(0.1f, 1f)]
        public float metronomeVolume = 0.4f;

        [Header("Gesture Suggestion")]
        [Tooltip("If true, suggests gestures on each beat.")]
        public bool showGestureSuggestions = true;

        [Tooltip("How often the suggested gesture changes (in beats).")]
        [Range(1, 8)]
        public int suggestionChangeInterval = 2;

        [Header("Scoring")]
        [Tooltip("Points for hitting a gesture on-beat in FreeStage.")]
        public int onBeatScore = 100;

        [Tooltip("Points for any gesture (off-beat) in FreeStage.")]
        public int freeScore = 30;

        // --- State ---
        public bool IsActive { get; private set; }
        public int Score { get; private set; }
        public int GestureCount { get; private set; }
        public int OnBeatCount { get; private set; }
        public float SessionDuration => IsActive ? Time.time - sessionStartTime : 0f;
        public float CurrentBeatTime => metronomeTimer;
        public int CurrentBeatIndex { get; private set; }
        public GestureType SuggestedGesture { get; private set; } = GestureType.Down;
        public bool IsStrongBeat => CurrentBeatIndex % beatsPerMeasure == 0;

        // --- Events ---
        public event Action<int> ScoreChanged;
        public event Action<int> BeatChanged;           // beat index
        public event Action<GestureType> SuggestionChanged;
        public event Action<string> StatusChanged;

        // --- Internal ---
        private float metronomeTimer;
        private float beatInterval;
        private float sessionStartTime;
        private int lastGestureSuggestionBeat = -1;
        private float lastGestureTime = -1f;
        private readonly GestureType[] gesturePool = { GestureType.Down, GestureType.Up, GestureType.Left, GestureType.Right, GestureType.Expand, GestureType.Close };
        private int gesturePoolIndex;
        private int metronomeSampleRate = 24000;

        private void Start()
        {
            if (gameDirector == null)
                gameDirector = GetComponent<MaestroGameDirector>();
            if (orchestra == null)
                orchestra = GetComponent<OrchestraController>();

            // Create a metronome AudioSource if not assigned
            if (metronomeSource == null)
            {
                metronomeSource = gameObject.AddComponent<AudioSource>();
                metronomeSource.playOnAwake = false;
                metronomeSource.loop = false;
            }
        }

        private void OnEnable()
        {
            if (gameDirector != null)
            {
                // Subscribe to all gesture sources for FreeStage
                var dispatcher = gameDirector.gestureInput;
                if (dispatcher?.nativeInput != null)
                    dispatcher.nativeInput.GestureCaptured += HandleFreeGesture;
                if (dispatcher?.handInput != null)
                    dispatcher.handInput.GestureCaptured += HandleFreeGesture;
            }
        }

        private void OnDisable()
        {
            if (gameDirector?.gestureInput != null)
            {
                var dispatcher = gameDirector.gestureInput;
                if (dispatcher?.nativeInput != null)
                    dispatcher.nativeInput.GestureCaptured -= HandleFreeGesture;
                if (dispatcher?.handInput != null)
                    dispatcher.handInput.GestureCaptured -= HandleFreeGesture;
            }
        }

        private void Update()
        {
            if (!IsActive) return;

            // Metronome timer
            metronomeTimer += Time.deltaTime;
            if (metronomeTimer >= beatInterval)
            {
                metronomeTimer -= beatInterval;
                CurrentBeatIndex++;
                OnBeat();
            }

            // Change gesture suggestion periodically
            if (showGestureSuggestions && CurrentBeatIndex != lastGestureSuggestionBeat
                && CurrentBeatIndex % suggestionChangeInterval == 0)
            {
                lastGestureSuggestionBeat = CurrentBeatIndex;
                CycleSuggestion();
            }
        }

        // ═══════════════════════════════════════════════
        //  Public API
        // ═══════════════════════════════════════════════

        /// <summary>Start FreeStage mode with metronome at the given BPM.</summary>
        public void Begin(int? bpm = null)
        {
            int targetBpm = bpm ?? defaultBpm;
            beatInterval = 60f / targetBpm;
            metronomeTimer = 0f;
            CurrentBeatIndex = 0;
            Score = 0;
            GestureCount = 0;
            OnBeatCount = 0;
            sessionStartTime = Time.time;
            IsActive = true;

            // Pick initial suggestion
            CycleSuggestion();

            Debug.Log($"[FreeStage] Started at {targetBpm} BPM, {beatsPerMeasure}/4");
            StatusChanged?.Invoke($"Free Stage — {targetBpm} BPM");
            BeatChanged?.Invoke(0);
            ScoreChanged?.Invoke(0);
        }

        /// <summary>Stop FreeStage mode.</summary>
        public void End()
        {
            IsActive = false;
            float duration = SessionDuration;
            Debug.Log($"[FreeStage] Ended. Duration={duration:F1}s, Score={Score}, Gestures={GestureCount}, OnBeat={OnBeatCount}");
            StatusChanged?.Invoke($"Session: {Score} pts, {GestureCount} gestures, {duration:F0}s");
        }

        /// <summary>Set BPM during active session.</summary>
        public void SetBpm(int bpm)
        {
            if (bpm <= 0 || bpm > 300) return;
            beatInterval = 60f / bpm;
            StatusChanged?.Invoke($"Free Stage — {bpm} BPM");
        }

        /// <summary>Toggle gesture suggestions on/off.</summary>
        public void ToggleSuggestions()
        {
            showGestureSuggestions = !showGestureSuggestions;
            StatusChanged?.Invoke(showGestureSuggestions ? "Suggestions ON" : "Suggestions OFF");
        }

        // ═══════════════════════════════════════════════
        //  Internal
        // ═══════════════════════════════════════════════

        private void OnBeat()
        {
            BeatChanged?.Invoke(CurrentBeatIndex);

            // Play metronome click
            PlayMetronomeClick(IsStrongBeat);
        }

        private void HandleFreeGesture(GestureType gesture, float inputTime)
        {
            if (!IsActive) return;

            // Debounce
            if (Time.time - lastGestureTime < 0.15f) return;
            lastGestureTime = Time.time;

            GestureCount++;

            // Check if gesture is on-beat (within half beat interval of a beat)
            float timeInBeat = metronomeTimer;
            float halfBeat = beatInterval * 0.5f;
            bool onBeat = timeInBeat < 0.12f || timeInBeat > beatInterval - 0.12f;

            if (onBeat)
            {
                OnBeatCount++;
                Score += onBeatScore;

                // Check if it matches suggestion
                if (gesture == SuggestedGesture)
                {
                    Score += onBeatScore / 2; // Bonus for matching suggestion
                    StatusChanged?.Invoke($"{gesture} ✓ +{onBeatScore + onBeatScore / 2}");
                }
                else
                {
                    StatusChanged?.Invoke($"{gesture} ✓ +{onBeatScore}");
                }
            }
            else
            {
                Score += freeScore;
                StatusChanged?.Invoke($"{gesture} +{freeScore}");
            }

            ScoreChanged?.Invoke(Score);

            // Route to orchestra for animal reactions
            if (orchestra != null)
            {
                orchestra.FreeGesture(gesture);
            }
        }

        private void CycleSuggestion()
        {
            // Semi-random: cycle through pool, occasionally random
            if (UnityEngine.Random.value < 0.3f)
            {
                SuggestedGesture = gesturePool[UnityEngine.Random.Range(0, gesturePool.Length)];
            }
            else
            {
                gesturePoolIndex = (gesturePoolIndex + 1) % gesturePool.Length;
                SuggestedGesture = gesturePool[gesturePoolIndex];
            }

            SuggestionChanged?.Invoke(SuggestedGesture);
        }

        private void PlayMetronomeClick(bool strong)
        {
            if (metronomeSource == null) return;

            int clickSamples = Mathf.CeilToInt(0.04f * metronomeSampleRate);
            float[] data = new float[clickSamples];
            float freq = strong ? 1200f : 800f;
            for (int i = 0; i < clickSamples; i++)
            {
                float t = (float)i / metronomeSampleRate;
                float env = Mathf.Exp(-t * 80f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * metronomeVolume;
            }

            AudioClip click = AudioClip.Create("MetronomeClick", clickSamples, 1, metronomeSampleRate, false);
            click.SetData(data, 0);
            metronomeSource.PlayOneShot(click, metronomeVolume);
        }
    }
}
