using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaestroZoo
{
    /// <summary>
    /// 猫头鹰老师教程系统 — 分步引导玩家学习 6 种指挥手势。
    /// Owl Teacher: step-by-step gesture tutorial with real-time feedback.
    /// </summary>
    public class OwlTutorialController : MonoBehaviour
    {
        [Header("References")]
        public MaestroGameDirector gameDirector;
        public JudgeManager judgeManager;
        public GameHud gameHud;

        [Header("Tutorial Settings")]
        [Tooltip("Minimum hits required per gesture step (out of notes in that step).")]
        [Range(1, 4)]
        public int requiredHitsPerStep = 1;

        [Tooltip("If true, player can retry failed steps.")]
        public bool allowRetry = true;

        // --- State ---
        public enum TutorialStep
        {
            Idle,
            Intro,
            LearnDown,
            LearnUp,
            LearnLeft,
            LearnRight,
            LearnExpand,
            LearnClose,
            Complete
        }

        public TutorialStep CurrentStep { get; private set; } = TutorialStep.Idle;

        /// <summary>0–1 progress within the current gesture step.</summary>
        public float StepProgress
        {
            get
            {
                int total = GetNoteCountForStep(CurrentStep);
                if (total <= 0) return 0f;
                return Mathf.Clamp01((float)stepHitCount / total);
            }
        }

        /// <summary>Overall tutorial progress 0–1.</summary>
        public float OverallProgress
        {
            get
            {
                int stepIndex = (int)CurrentStep;
                int totalSteps = 8; // Idle→Intro→6 gestures→Complete
                if (CurrentStep == TutorialStep.Idle) return 0f;
                if (CurrentStep == TutorialStep.Complete) return 1f;
                return Mathf.Clamp01((stepIndex + StepProgress) / totalSteps);
            }
        }

        /// <summary>Target gesture being taught in the current step.</summary>
        public GestureType TargetGesture
        {
            get
            {
                return CurrentStep switch
                {
                    TutorialStep.LearnDown   => GestureType.Down,
                    TutorialStep.LearnUp     => GestureType.Up,
                    TutorialStep.LearnLeft   => GestureType.Left,
                    TutorialStep.LearnRight  => GestureType.Right,
                    TutorialStep.LearnExpand => GestureType.Expand,
                    TutorialStep.LearnClose  => GestureType.Close,
                    _                        => GestureType.Down
                };
            }
        }

        /// <summary>The animal associated with the current gesture.</summary>
        public string TargetAnimal => GetAnimalForGesture(TargetGesture);

        // --- Events (for UI) ---
        public event Action<TutorialStep> StepChanged;
        public event Action<string> InstructionChanged;    // Current instruction text
        public event Action<string> FeedbackChanged;      // Real-time feedback text
        public event Action TutorialCompleted;

        // --- Internal ---
        private int stepHitCount;
        private int stepMissCount;
        private float stepStartTime;
        private bool stepCompleted;
        private readonly HashSet<string> feedbackShown = new HashSet<string>();

        private void Start()
        {
            if (gameDirector == null)
                gameDirector = GetComponent<MaestroGameDirector>();
            if (judgeManager == null)
                judgeManager = GetComponent<JudgeManager>();
        }

        private void OnEnable()
        {
            if (judgeManager != null)
            {
                judgeManager.NoteJudged += HandleNoteJudged;
                judgeManager.WrongGesture += HandleWrongGesture;
            }

            if (gameDirector?.chartPlayer != null)
            {
                gameDirector.chartPlayer.PlaybackEnded += HandleTutorialPlaybackEnded;
            }
        }

        private void OnDisable()
        {
            if (judgeManager != null)
            {
                judgeManager.NoteJudged -= HandleNoteJudged;
                judgeManager.WrongGesture -= HandleWrongGesture;
            }

            if (gameDirector?.chartPlayer != null)
            {
                gameDirector.chartPlayer.PlaybackEnded -= HandleTutorialPlaybackEnded;
            }
        }

        // ═══════════════════════════════════════════════
        //  Public API — call from UI buttons
        // ═══════════════════════════════════════════════

        /// <summary>Start the full tutorial sequence from the beginning.</summary>
        public void StartTutorial()
        {
            GoToStep(TutorialStep.Intro);
            feedbackShown.Clear();
        }

        /// <summary>Skip to the next step (for impatient players).</summary>
        public void SkipCurrentStep()
        {
            TutorialStep next = GetNextStep(CurrentStep);
            if (next != TutorialStep.Complete && next != TutorialStep.Idle)
                GoToStep(next);
        }

        /// <summary>Restart the current step (retry).</summary>
        public void RetryStep()
        {
            if (!allowRetry) return;
            stepHitCount = 0;
            stepMissCount = 0;
            stepCompleted = false;
            stepStartTime = Time.time;
            ReloadTutorialChart();
            FeedbackChanged?.Invoke("再试一次！(Try again!)");
        }

        /// <summary>Cancel tutorial and return to title.</summary>
        public void CancelTutorial()
        {
            GoToStep(TutorialStep.Idle);
            if (gameDirector != null)
                gameDirector.BackToTitle();
        }

        // ═══════════════════════════════════════════════
        //  Callbacks
        // ═══════════════════════════════════════════════

        private void HandleNoteJudged(FlyingNote note, JudgeResult result)
        {
            if (CurrentStep < TutorialStep.LearnDown) return;

            bool targetsCurrentGesture = note.Note.GestureType == TargetGesture;
            bool isPracticeNote = note.Note.gesture == TargetGesture.ToString();

            if (targetsCurrentGesture)
            {
                if (result == JudgeResult.Perfect || result == JudgeResult.Good)
                {
                    stepHitCount++;
                    string key = $"hit_{TargetGesture}_{stepHitCount}";
                    if (!feedbackShown.Contains(key))
                    {
                        feedbackShown.Add(key);
                        ShowHitFeedback(result, note);
                    }

                    CheckStepComplete();
                }
                else
                {
                    stepMissCount++;
                    ShowMissFeedback(TargetGesture);
                }
            }
            else if (isPracticeNote && result == JudgeResult.Miss)
            {
                // Player missed a note of the target gesture
                ShowMissFeedback(TargetGesture);
            }
        }

        private void HandleWrongGesture(GestureType wrongGesture)
        {
            if (CurrentStep < TutorialStep.LearnDown) return;

            // Only show hint periodically (not every wrong gesture)
            if (stepMissCount > 0 && stepMissCount % 3 == 0)
            {
                string hint = GetMistakeHint(wrongGesture, TargetGesture);
                FeedbackChanged?.Invoke(hint);
            }
        }

        /// <summary>
        /// When the tutorial mini-chart ends but the step hasn't been completed,
        /// auto-retry. If step IS complete, advance to next step.
        /// </summary>
        private void HandleTutorialPlaybackEnded()
        {
            if (!IsLearnStep(CurrentStep)) return;

            if (stepCompleted)
            {
                // Step was already completed — advance should have been scheduled.
                // If it fired too early and playback ended before advancing, force advance now.
                CancelInvoke(nameof(AdvanceToNextStep));
                AdvanceToNextStep();
            }
            else
            {
                // Player didn't hit enough notes — auto-retry
                Debug.Log($"[OwlTeacher] Step {CurrentStep} chart ended incomplete. Auto-retrying.");
                stepHitCount = 0;
                stepMissCount = 0;
                ReloadTutorialChart();
                FeedbackChanged?.Invoke("再试一次! 注意看手势提示~\n(Try again! Watch the gesture hint~)");
            }
        }

        // ═══════════════════════════════════════════════
        //  Step Management
        // ═══════════════════════════════════════════════

        private void GoToStep(TutorialStep step)
        {
            CurrentStep = step;
            stepHitCount = 0;
            stepMissCount = 0;
            stepCompleted = false;
            stepStartTime = Time.time;
            feedbackShown.Clear();

            StepChanged?.Invoke(step);
            InstructionChanged?.Invoke(GetStepInstruction(step));
            FeedbackChanged?.Invoke("");

            Debug.Log($"[OwlTeacher] Step: {step}");

            if (step == TutorialStep.Intro)
            {
                // Brief intro pause, then start first gesture
                Invoke(nameof(StartFirstGestureStep), 2.5f);
            }
            else if (IsLearnStep(step))
            {
                ReloadTutorialChart();
            }
            else if (step == TutorialStep.Complete)
            {
                TutorialCompleted?.Invoke();
            }
        }

        private void StartFirstGestureStep()
        {
            GoToStep(TutorialStep.LearnDown);
        }

        private void CheckStepComplete()
        {
            if (stepCompleted) return;

            if (stepHitCount >= requiredHitsPerStep)
            {
                stepCompleted = true;

                TutorialStep next = GetNextStep(CurrentStep);
                if (next == TutorialStep.Complete)
                {
                    GoToStep(TutorialStep.Complete);
                }
                else
                {
                    // Brief celebration, then next step
                    FeedbackChanged?.Invoke(GetCelebrationMessage(CurrentStep));
                    float delay = next == TutorialStep.Complete ? 3f : 2f;
                    TutorialStep currentStepCapture = CurrentStep;
                    Invoke(nameof(AdvanceToNextStep), delay);
                }
            }
        }

        private void AdvanceToNextStep()
        {
            GoToStep(GetNextStep(CurrentStep));
        }

        private void ReloadTutorialChart()
        {
            if (gameDirector == null || gameDirector.chartPlayer == null) return;

            ChartPlayer cp = gameDirector.chartPlayer;
            ChartData stepChart = GenerateStepChart(CurrentStep);

            // If step has notes, play them. Otherwise use the full tutorial chart.
            if (stepChart != null && stepChart.notes != null && stepChart.notes.Length > 0)
            {
                cp.StartSong(stepChart);
            }
            else
            {
                gameDirector.StartTutorial();
            }
        }

        /// <summary>
        /// Generate a mini-chart for a tutorial step: 2 notes of the target gesture,
        /// spaced 1.5s apart, starting at t=2.0s.
        /// </summary>
        private ChartData GenerateStepChart(TutorialStep step)
        {
            if (!IsLearnStep(step)) return null;

            GestureType gesture = TargetGesture;
            string animalId = TargetAnimal;
            int noteCount = GetNoteCountForStep(step);

            var notes = new ChartNote[noteCount];
            for (int i = 0; i < noteCount; i++)
            {
                notes[i] = new ChartNote
                {
                    time = 2f + i * 1.5f,
                    gesture = gesture.ToString(),
                    lane = 0,
                    animal = animalId
                };
            }

            return new ChartData
            {
                songName = $"Tutorial: {gesture}",
                bpm = 96,
                difficulty = "Tutorial",
                leadTime = 2f,
                notes = notes
            };
        }

        // ═══════════════════════════════════════════════
        //  Feedback Messages
        // ═══════════════════════════════════════════════

        private void ShowHitFeedback(JudgeResult result, FlyingNote note)
        {
            string animal = string.IsNullOrEmpty(note.Note.animal) ? "" : GetAnimalDisplayName(note.Note.animal);
            string quality = result == JudgeResult.Perfect ? "完美!" : "不错!";
            string msg = string.IsNullOrEmpty(animal)
                ? $"{quality} ({(result == JudgeResult.Perfect ? "Perfect!" : "Good!")})"
                : $"{quality} {animal} 在演奏! ({animal} is playing!)";
            FeedbackChanged?.Invoke(msg);
        }

        private void ShowMissFeedback(GestureType target)
        {
            string hint = GetGestureHint(target);
            FeedbackChanged?.Invoke($"没打中... {hint} (Missed! {hint})");
        }

        private string GetMistakeHint(GestureType actual, GestureType expected)
        {
            return actual switch
            {
                GestureType.Up when expected == GestureType.Down =>
                    "方向反了! 试试向下挥 ↘ (Wrong direction! Try DOWN)",
                GestureType.Down when expected == GestureType.Up =>
                    "方向反了! 试试向上挥 ↗ (Wrong direction! Try UP)",
                GestureType.Left when expected == GestureType.Right =>
                    "方向反了! 试试向右挥 → (Try RIGHT instead of LEFT!)",
                GestureType.Right when expected == GestureType.Left =>
                    "方向反了! 试试向左挥 ← (Try LEFT instead of RIGHT!)",
                _ => $"注意看提示! 应该做 {expected} (Watch the hint! Do {expected} gesture)"
            };
        }

        private string GetCelebrationMessage(TutorialStep step)
        {
            return step switch
            {
                TutorialStep.LearnDown   => "太棒了! Down 是节奏的基础! 🥁",
                TutorialStep.LearnUp     => "很好! Up 让旋律飞翔! 🎵",
                TutorialStep.LearnLeft   => "厉害! 小提琴在你手下歌唱! 🎻",
                TutorialStep.LearnRight  => "精彩! 大提琴带来深沉的低音! 🎻",
                TutorialStep.LearnExpand => "震撼! 整个乐队都在你的掌控中! ✨",
                TutorialStep.LearnClose  => "优雅! 完美的收束! 👏",
                _                        => "很好! 继续下一个! 🎶"
            };
        }

        // ═══════════════════════════════════════════════
        //  Static Helpers
        // ═══════════════════════════════════════════════

        private static bool IsLearnStep(TutorialStep step)
        {
            return step >= TutorialStep.LearnDown && step <= TutorialStep.LearnClose;
        }

        private static TutorialStep GetNextStep(TutorialStep current)
        {
            return current switch
            {
                TutorialStep.Intro      => TutorialStep.LearnDown,
                TutorialStep.LearnDown  => TutorialStep.LearnUp,
                TutorialStep.LearnUp    => TutorialStep.LearnLeft,
                TutorialStep.LearnLeft  => TutorialStep.LearnRight,
                TutorialStep.LearnRight => TutorialStep.LearnExpand,
                TutorialStep.LearnExpand => TutorialStep.LearnClose,
                TutorialStep.LearnClose => TutorialStep.Complete,
                _                       => TutorialStep.Complete
            };
        }

        private static string GetStepInstruction(TutorialStep step)
        {
            return step switch
            {
                TutorialStep.Intro =>
                    "欢迎! 我是猫头鹰老师 🦉\n让我教你如何指挥管弦乐队!\n(Welcome! I'm the Owl Teacher.\nLet me teach you to conduct!)",

                TutorialStep.LearnDown =>
                    "【第1步】向下挥手 → Down\n这是指挥棒下击的动作!\n鼓手小兔会响应你的节拍 🥁\n(Wave DOWN — the baton downbeat!\nRabbitDrum will follow your beat)",

                TutorialStep.LearnUp =>
                    "【第2步】向上挥手 → Up\n这是指挥棒上挑的动作!\n小鸟长笛会应声高歌 🎵\n(Wave UP — the baton upbeat!\nBirdFlute will sing for you)",

                TutorialStep.LearnLeft =>
                    "【第3步】向左挥手 → Left\n指向舞台左侧!\n狐狸小提琴会用弦乐回应 🎻\n(Wave LEFT — cue the left side!\nFoxViolin will answer with strings)",

                TutorialStep.LearnRight =>
                    "【第4步】向右挥手 → Right\n指向舞台右侧!\n大提琴小熊带来深沉的低音 🎻\n(Wave RIGHT — cue the right side!\nBearCello brings deep bass tones)",

                TutorialStep.LearnExpand =>
                    "【第5步】双手张开 → Expand\n让整个乐队渐强!\n全员齐奏，气势磅礴 ✨\n(Open your hands — CRESCENDO!\nThe full orchestra swells together)",

                TutorialStep.LearnClose =>
                    "【第6步】双手收拢 → Close\n优雅地结束乐句!\n让你的乐队收束 🎶\n(Close your hands — end the phrase!\nBring your orchestra to rest)",

                TutorialStep.Complete =>
                    "🎉 恭喜! 你已经是合格的指挥家了! 🎉\n准备迎接真正的音乐会挑战吧!\n(Congratulations! You're a conductor now!\nReady for the concert hall!)",

                _ => ""
            };
        }

        private static string GetGestureHint(GestureType gesture)
        {
            return gesture switch
            {
                GestureType.Down   => "用力向下挥! (Swing DOWN firmly!)",
                GestureType.Up     => "轻快向上挑! (Flick UP lightly!)",
                GestureType.Left   => "果断向左指! (Point LEFT decisively!)",
                GestureType.Right  => "果断向右指! (Point RIGHT decisively!)",
                GestureType.Expand => "双手张开! (Open both hands!)",
                GestureType.Close  => "双手收拢! (Close both hands!)",
                _                  => ""
            };
        }

        private static string GetAnimalDisplayName(string animalId)
        {
            return animalId switch
            {
                "RabbitDrum"    => "小兔鼓手",
                "FoxViolin"     => "狐狸小提琴",
                "BearCello"     => "小熊大提琴",
                "BirdFlute"     => "小鸟长笛",
                "ElephantHorn"  => "大象号角",
                "FullOrchestra"  => "全体乐队",
                _               => animalId
            };
        }

        private static string GetAnimalForGesture(GestureType gesture)
        {
            return gesture switch
            {
                GestureType.Down   => "RabbitDrum",
                GestureType.Up     => "BirdFlute",
                GestureType.Left   => "FoxViolin",
                GestureType.Right  => "BearCello",
                GestureType.Expand => "FullOrchestra",
                GestureType.Close  => "FullOrchestra",
                _                  => ""
            };
        }

        private static int GetNoteCountForStep(TutorialStep step)
        {
            // tutorial_basic.json has 2 notes per gesture (Down×2, Up×2, ...)
            return step switch
            {
                TutorialStep.LearnDown   => 2,
                TutorialStep.LearnUp     => 2,
                TutorialStep.LearnLeft   => 2,
                TutorialStep.LearnRight  => 2,
                TutorialStep.LearnExpand => 1,
                TutorialStep.LearnClose  => 1,
                _                        => 0
            };
        }
    }
}
