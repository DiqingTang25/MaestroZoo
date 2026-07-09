using System;
using UnityEngine;

namespace MaestroZoo
{
    public class JudgeManager : MonoBehaviour
    {
        public ChartPlayer chartPlayer;
        public NoteSpawner noteSpawner;
        public MonoBehaviour inputBehaviour;

        [Header("Timing Windows")]
        public float perfectWindow = 0.08f;
        public float goodWindow = 0.18f;
        public float missWindow = 0.35f;

        [Header("Difficulty")]
        [Tooltip("Optional difficulty profile. Overrides timing windows on Start.")]
        public DifficultyProfile difficultyProfile;

        [Header("Scoring")]
        public int scorePerPerfect = 1000;
        public int scorePerGood = 500;

        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int PerfectCount { get; private set; }
        public int GoodCount { get; private set; }
        public int MissCount { get; private set; }
        public int WrongGestureCount { get; private set; }
        public int TotalJudged => PerfectCount + GoodCount + MissCount;
        public float Accuracy
        {
            get
            {
                int total = TotalJudged;
                if (total == 0)
                {
                    return 1f;
                }

                return Mathf.Clamp01((PerfectCount + GoodCount * 0.55f) / total);
            }
        }

        public event Action<FlyingNote, JudgeResult> NoteJudged;
        public event Action<int> ScoreChanged;
        public event Action<int> ComboChanged;
        public event Action<GestureType> WrongGesture;
        public event Action<FlyingNote, float> SustainedHoldStarted; // note, holdStartTime
        public event Action<FlyingNote, float> SustainedHoldReleased; // note, holdDuration
        public event Action JudgementReset;

        private IGestureInput input;
        private ChartPlayer subscribedPlayer;

        // Sustained/long-press tracking
        private FlyingNote activeSustainedNote;
        private GestureType sustainedGesture;
        private float sustainedHoldStartTime;

        private void Reset()
        {
            chartPlayer = GetComponent<ChartPlayer>();
            noteSpawner = GetComponent<NoteSpawner>();
            inputBehaviour = GetComponent<GestureInputDispatcher>();
        }

        private void Awake()
        {
            ResolveInput();
        }

        private void Start()
        {
            if (difficultyProfile != null)
            {
                difficultyProfile.ApplyTo(this);
            }
        }

        private void Update()
        {
            UpdateSubscription();

            if (chartPlayer == null || noteSpawner == null || input == null)
            {
                return;
            }

            // Only judge gestures when chart is actively playing.
            // Gestures before playback start or after end are ignored.
            if (chartPlayer.IsPlaying && !chartPlayer.IsPaused && chartPlayer.Chart != null)
            {
                while (input.TryConsumeGesture(out GestureType gesture, out _))
                {
                    JudgeGesture(gesture);
                }

                JudgeExpiredNotes();
                UpdateSustainedHold();
            }
        }

        private void ResolveInput()
        {
            if (input == null && inputBehaviour != null)
            {
                input = inputBehaviour as IGestureInput;
            }
        }

        private void UpdateSubscription()
        {
            if (subscribedPlayer == chartPlayer)
            {
                return;
            }

            if (subscribedPlayer != null)
            {
                subscribedPlayer.PlaybackStarted -= ResetRun;
            }

            subscribedPlayer = chartPlayer;

            if (subscribedPlayer != null)
            {
                subscribedPlayer.PlaybackStarted += ResetRun;
            }
        }

        public void ResetRun()
        {
            Score = 0;
            Combo = 0;
            MaxCombo = 0;
            PerfectCount = 0;
            GoodCount = 0;
            MissCount = 0;
            WrongGestureCount = 0;
            activeSustainedNote = null;
            sustainedHoldStartTime = 0f;
            JudgementReset?.Invoke();
            ScoreChanged?.Invoke(Score);
            ComboChanged?.Invoke(Combo);
        }

        private void JudgeGesture(GestureType gesture)
        {
            // If currently holding a sustained note, a new gesture breaks the hold
            if (activeSustainedNote != null && gesture != sustainedGesture)
            {
                ReleaseSustainedHold(false);
            }

            FlyingNote best = null;
            float bestAbsOffset = float.MaxValue;

            foreach (FlyingNote note in noteSpawner.ActiveNotes)
            {
                if (note == null || note.Judged || note.Note.GestureType != gesture)
                {
                    continue;
                }

                float offset = Mathf.Abs(chartPlayer.CompensatedSongTime - note.Note.time);
                if (offset < bestAbsOffset)
                {
                    bestAbsOffset = offset;
                    best = note;
                }
            }

            if (best == null || bestAbsOffset > missWindow)
            {
                BreakCombo(gesture);
                return;
            }

            // Sustained/long-press note: start hold tracking instead of immediate judgment
            if (best.Note.IsSustained)
            {
                StartSustainedHold(best, gesture);
                return;
            }

            JudgeResult result = bestAbsOffset <= perfectWindow ? JudgeResult.Perfect : JudgeResult.Good;
            ApplyResult(best, result);
        }

        private void StartSustainedHold(FlyingNote note, GestureType gesture)
        {
            activeSustainedNote = note;
            sustainedGesture = gesture;
            sustainedHoldStartTime = chartPlayer.CompensatedSongTime;
            SustainedHoldStarted?.Invoke(note, sustainedHoldStartTime);
        }

        private void ReleaseSustainedHold(bool completed)
        {
            if (activeSustainedNote == null) return;

            float holdDuration = chartPlayer.CompensatedSongTime - sustainedHoldStartTime;
            SustainedHoldReleased?.Invoke(activeSustainedNote, holdDuration);

            if (completed)
            {
                // Held for the full duration → Perfect
                ApplyResult(activeSustainedNote, JudgeResult.Perfect);
            }
            else
            {
                // Released early → Good (partial credit)
                ApplyResult(activeSustainedNote, JudgeResult.Good);
            }

            activeSustainedNote = null;
        }

        /// <summary>
        /// Called each frame to check if sustained hold has been maintained long enough.
        /// Auto-completes when hold duration >= note.duration.
        /// </summary>
        private void UpdateSustainedHold()
        {
            if (activeSustainedNote == null) return;

            // Check if note expired (should not happen if hold is active, but guard)
            if (chartPlayer.CompensatedSongTime > activeSustainedNote.Note.time + missWindow + activeSustainedNote.Note.duration)
            {
                // Expired despite hold — still give Good
                ReleaseSustainedHold(true);
                return;
            }

            // Auto-complete when hold >= required duration
            float holdDuration = chartPlayer.CompensatedSongTime - sustainedHoldStartTime;
            if (holdDuration >= activeSustainedNote.Note.duration)
            {
                ReleaseSustainedHold(true);
            }
        }

        private void JudgeExpiredNotes()
        {
            foreach (FlyingNote note in noteSpawner.ActiveNotes)
            {
                if (note == null || note.Judged)
                {
                    continue;
                }

                if (chartPlayer.CompensatedSongTime > note.Note.time + missWindow)
                {
                    ApplyResult(note, JudgeResult.Miss);
                }
            }
        }

        private void ApplyResult(FlyingNote note, JudgeResult result)
        {
            note.MarkJudged(result);

            if (result == JudgeResult.Perfect)
            {
                Score += scorePerPerfect;
                Combo++;
                PerfectCount++;
            }
            else if (result == JudgeResult.Good)
            {
                Score += scorePerGood;
                Combo++;
                GoodCount++;
            }
            else
            {
                Combo = 0;
                MissCount++;
            }

            if (Combo > MaxCombo)
            {
                MaxCombo = Combo;
            }

            NoteJudged?.Invoke(note, result);
            ScoreChanged?.Invoke(Score);
            ComboChanged?.Invoke(Combo);
            Debug.Log($"{result}: {note.Note.gesture} at {note.Note.time:0.00}s, score={Score}, combo={Combo}");
        }

        private void BreakCombo(GestureType gesture)
        {
            WrongGestureCount++;
            Combo = 0;
            WrongGesture?.Invoke(gesture);
            ComboChanged?.Invoke(Combo);
            Debug.Log("Wrong gesture.");
        }
    }

    public enum JudgeResult
    {
        Perfect,
        Good,
        Miss
    }
}
