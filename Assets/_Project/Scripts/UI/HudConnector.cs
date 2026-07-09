using UnityEngine;

namespace MaestroZoo
{
    public class HudConnector : MonoBehaviour
    {
        public GameHud gameHud;
        public MaestroGameDirector gameDirector;
        public JudgeManager judgeManager;
        public OrchestraController orchestra;

        private MaestroGameMode lastMode;

        private void Start()
        {
            if (judgeManager != null)
            {
                judgeManager.ScoreChanged += OnScoreChanged;
                judgeManager.NoteJudged += OnNoteJudged;
            }
        }

        private void Update()
        {
            if (gameDirector == null || gameHud == null) return;

            if (gameDirector.chartPlayer != null && gameDirector.chartPlayer.IsPlaying)
            {
                float t = gameDirector.chartPlayer.ChartEndTime > 0f
                    ? Mathf.Clamp01(gameDirector.chartPlayer.SongTime / gameDirector.chartPlayer.ChartEndTime)
                    : 0f;
                gameHud.SetProgress(t);
            }

            if (orchestra != null)
            {
                gameHud.SetMood(Mathf.Clamp01(orchestra.Mood / 100f));
                gameHud.SetFever(orchestra.FeverActive);
            }

            if (gameDirector.Mode != lastMode)
            {
                lastMode = gameDirector.Mode;
                if (lastMode == MaestroGameMode.Results)
                {
                    gameHud.ShowResults(judgeManager, gameDirector.CurrentModeTitle);
                }
                else
                {
                    gameHud.SetResultsVisible(false);
                }
            }
        }

        private void OnDestroy()
        {
            if (judgeManager != null)
            {
                judgeManager.ScoreChanged -= OnScoreChanged;
                judgeManager.NoteJudged -= OnNoteJudged;
            }
        }

        private void OnScoreChanged(int score)
        {
            if (gameHud != null)
                gameHud.SetScore(score);
        }

        private void OnNoteJudged(FlyingNote note, JudgeResult result)
        {
            if (gameHud != null)
            {
                gameHud.ShowJudgment(result.ToString());
                if (judgeManager != null)
                    gameHud.SetCombo(judgeManager.Combo, judgeManager.MaxCombo);
            }
        }
    }
}
