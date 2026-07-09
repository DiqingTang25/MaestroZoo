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
        public event Action JudgementReset;

        private IGestureInput input;
        private ChartPlayer subscribedPlayer;

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

        private void Update()
        {
            UpdateSubscription();

            if (chartPlayer == null || noteSpawner == null || input == null)
            {
                return;
            }

            // Only judge gestures when chart is actively playing.
            // Gestures before playback start or after end are ignored.
            if (chartPlayer.IsPlaying && chartPlayer.Chart != null)
            {
                while (input.TryConsumeGesture(out GestureType gesture, out _))
                {
                    JudgeGesture(gesture);
                }

                JudgeExpiredNotes();
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
            JudgementReset?.Invoke();
            ScoreChanged?.Invoke(Score);
            ComboChanged?.Invoke(Combo);
        }

        private void JudgeGesture(GestureType gesture)
        {
            FlyingNote best = null;
            float bestAbsOffset = float.MaxValue;

            foreach (FlyingNote note in noteSpawner.ActiveNotes)
            {
                if (note == null || note.Judged || note.Note.GestureType != gesture)
                {
                    continue;
                }

                float offset = Mathf.Abs(chartPlayer.SongTime - note.Note.time);
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

            JudgeResult result = bestAbsOffset <= perfectWindow ? JudgeResult.Perfect : JudgeResult.Good;
            ApplyResult(best, result);
        }

        private void JudgeExpiredNotes()
        {
            foreach (FlyingNote note in noteSpawner.ActiveNotes)
            {
                if (note == null || note.Judged)
                {
                    continue;
                }

                if (chartPlayer.SongTime > note.Note.time + missWindow)
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
                Score += 1000;
                Combo++;
                PerfectCount++;
            }
            else if (result == JudgeResult.Good)
            {
                Score += 500;
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
