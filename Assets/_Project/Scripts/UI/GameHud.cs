using UnityEngine;
using UnityEngine.UI;

namespace MaestroZoo
{
    public class GameHud : MonoBehaviour
    {
        [Header("Score")]
        public Text scoreText;
        public Text comboText;
        public Text maxComboText;

        [Header("Judgment")]
        public Text judgeText;
        public float judgeFlashDuration = 0.4f;

        [Header("Progress")]
        public Slider progressBar;
        public Text songNameText;

        [Header("Mood")]
        public Slider moodBar;
        public Image feverGlow;

        [Header("Tutorial")]
        public GameObject tutorialPanel;
        public Text tutorialInstructionText;
        public Text tutorialFeedbackText;

        [Header("Results")]
        public GameObject resultsPanel;
        public Text resultsTitleText;
        public Text resultsScoreText;
        public Text resultsStatsText;

        private float judgeFlashTimer;
        private float tutorialFeedbackFlashTimer;

        public void SetScore(int score)
        {
            if (scoreText != null) scoreText.text = score.ToString("N0");
        }

        public void SetCombo(int combo, int maxCombo)
        {
            if (comboText != null)
            {
                comboText.text = combo > 1 ? $"<size=28>{combo}</size>\nCOMBO" : "";
                comboText.color = combo >= 50 ? Color.HSVToRGB(0.13f, 1f, 1f) : Color.white;
            }
            if (maxComboText != null) maxComboText.text = $"Best: {maxCombo}";
        }

        public void ShowJudgment(string result)
        {
            if (judgeText != null)
            {
                judgeText.text = result;
                judgeFlashTimer = judgeFlashDuration;

                switch (result)
                {
                    case "Perfect":
                        judgeText.color = Color.HSVToRGB(0.13f, 1f, 1f);
                        break;
                    case "Great":
                        judgeText.color = Color.HSVToRGB(0.55f, 1f, 1f);
                        break;
                    case "Good":
                        judgeText.color = Color.HSVToRGB(0.08f, 1f, 1f);
                        break;
                    case "Miss":
                        judgeText.color = Color.red;
                        break;
                }
            }
        }

        public void SetProgress(float t)
        {
            if (progressBar != null) progressBar.normalizedValue = t;
        }

        public void SetSongName(string name)
        {
            if (songNameText != null) songNameText.text = name;
        }

        public void SetMood(float mood01)
        {
            if (moodBar != null) moodBar.normalizedValue = mood01;
        }

        public void SetFever(bool active)
        {
            if (feverGlow != null) feverGlow.enabled = active;
        }

        public void SetResultsVisible(bool visible)
        {
            if (resultsPanel != null) resultsPanel.SetActive(visible);
        }

        public void ShowTutorialInstruction(string text)
        {
            if (tutorialPanel != null)
                tutorialPanel.SetActive(true);
            if (tutorialInstructionText != null)
                tutorialInstructionText.text = text;
        }

        public void ShowTutorialFeedback(string text)
        {
            if (tutorialFeedbackText != null)
            {
                tutorialFeedbackText.text = text;
                tutorialFeedbackFlashTimer = 3f;
            }
        }

        public void HideTutorial()
        {
            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);
        }

        public void ShowResults(JudgeManager judgeManager, string title)
        {
            if (judgeManager == null) return;

            SetResultsVisible(true);

            if (resultsTitleText != null)
            {
                string diffLabel = judgeManager?.difficultyProfile != null
                    ? $" [{judgeManager.difficultyProfile.DisplayName}]"
                    : "";
                resultsTitleText.text = (string.IsNullOrEmpty(title) ? "Results" : title) + diffLabel;
            }

            if (resultsScoreText != null)
            {
                resultsScoreText.text = judgeManager.Score.ToString("N0");
            }

            if (resultsStatsText != null)
            {
                int accuracy = Mathf.RoundToInt(judgeManager.Accuracy * 100f);
                resultsStatsText.text =
                    $"Accuracy {accuracy}%\n" +
                    $"Max Combo {judgeManager.MaxCombo}\n" +
                    $"Perfect {judgeManager.PerfectCount}   Good {judgeManager.GoodCount}   Miss {judgeManager.MissCount}\n" +
                    $"Wrong Gesture {judgeManager.WrongGestureCount}";
            }
        }

        private void Update()
        {
            if (judgeFlashTimer > 0f)
            {
                judgeFlashTimer -= Time.deltaTime;
                if (judgeFlashTimer <= 0f && judgeText != null)
                {
                    judgeText.text = "";
                }
            }

            if (tutorialFeedbackFlashTimer > 0f)
            {
                tutorialFeedbackFlashTimer -= Time.deltaTime;
                if (tutorialFeedbackFlashTimer <= 0f && tutorialFeedbackText != null)
                {
                    tutorialFeedbackText.text = "";
                }
            }
        }
    }
}
