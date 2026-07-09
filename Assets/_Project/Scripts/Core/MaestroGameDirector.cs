using UnityEngine;

namespace MaestroZoo
{
    public class MaestroGameDirector : MonoBehaviour
    {
        public ChartPlayer chartPlayer;
        public NoteSpawner noteSpawner;
        public JudgeManager judgeManager;
        public GestureInputDispatcher gestureInput;
        public OrchestraController orchestra;

        public bool autoStartChallenge = false;
        public MaestroGameMode Mode { get; private set; } = MaestroGameMode.Title;
        public string CurrentModeTitle { get; private set; } = "Maestro Zoo";
        public string CurrentChartPath { get; private set; } = "";

        private void Start()
        {
            Subscribe();

            if (autoStartChallenge)
            {
                StartChallenge();
            }
        }

        private void OnDisable()
        {
            if (chartPlayer != null)
            {
                chartPlayer.PlaybackEnded -= HandlePlaybackEnded;
            }

            UnsubscribeGestureInput();
        }

        public void StartTutorial()
        {
            StartChartMode(MaestroGameMode.Tutorial, "Tutorial: First Conducting", "Charts/tutorial_basic");
        }

        public void StartChallenge()
        {
            StartChartMode(MaestroGameMode.Challenge, "Forest Concert Challenge", "Charts/forest_challenge_full");
        }

        public void StartParty()
        {
            StartChartMode(MaestroGameMode.Party, "Party Quick Round", "Charts/party_quick");
        }

        public void StartFreeStage()
        {
            Mode = MaestroGameMode.FreeStage;
            CurrentModeTitle = "Free Stage";
            CurrentChartPath = "";

            if (chartPlayer != null)
            {
                chartPlayer.StopSong();
            }

            if (noteSpawner != null)
            {
                noteSpawner.ClearNotes();
            }

            if (judgeManager != null)
            {
                judgeManager.ResetRun();
            }
        }

        public void RestartCurrent()
        {
            if (Mode == MaestroGameMode.Tutorial)
            {
                StartTutorial();
            }
            else if (Mode == MaestroGameMode.Party)
            {
                StartParty();
            }
            else if (Mode == MaestroGameMode.FreeStage)
            {
                StartFreeStage();
            }
            else
            {
                StartChallenge();
            }
        }

        public void BackToTitle()
        {
            Mode = MaestroGameMode.Title;
            CurrentModeTitle = "Maestro Zoo";
            if (chartPlayer != null)
            {
                chartPlayer.StopSong();
            }
            if (noteSpawner != null)
            {
                noteSpawner.ClearNotes();
            }
        }

        private void StartChartMode(MaestroGameMode mode, string title, string resourcePath)
        {
            TextAsset chart = Resources.Load<TextAsset>(resourcePath);
            if (chart == null)
            {
                Debug.LogError("Missing chart: " + resourcePath);
                return;
            }

            Mode = mode;
            CurrentModeTitle = title;
            CurrentChartPath = resourcePath;

            if (chartPlayer != null)
            {
                chartPlayer.StartSong(chart);
            }
        }

        private void Subscribe()
        {
            if (chartPlayer != null)
            {
                chartPlayer.PlaybackEnded -= HandlePlaybackEnded;
                chartPlayer.PlaybackEnded += HandlePlaybackEnded;
            }

            SubscribeGestureInput();
        }

        private void SubscribeGestureInput()
        {
            // Subscribe to all gesture sources for FreeStage mode.
            if (gestureInput != null)
            {
                if (gestureInput.nativeInput != null)
                    gestureInput.nativeInput.GestureCaptured += HandleGestureCaptured;
                if (gestureInput.handInput != null)
                    gestureInput.handInput.GestureCaptured += HandleGestureCaptured;
            }
        }

        private void UnsubscribeGestureInput()
        {
            if (gestureInput != null)
            {
                if (gestureInput.nativeInput != null)
                    gestureInput.nativeInput.GestureCaptured -= HandleGestureCaptured;
                if (gestureInput.handInput != null)
                    gestureInput.handInput.GestureCaptured -= HandleGestureCaptured;
            }
        }

        private void HandlePlaybackEnded()
        {
            if (Mode == MaestroGameMode.Tutorial || Mode == MaestroGameMode.Challenge || Mode == MaestroGameMode.Party)
            {
                Mode = MaestroGameMode.Results;
            }
        }

        private void HandleGestureCaptured(GestureType gesture, float inputTime)
        {
            if (Mode == MaestroGameMode.FreeStage && orchestra != null)
            {
                orchestra.FreeGesture(gesture);
            }
        }
    }
}
