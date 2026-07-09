using System;
using System.Collections;
using UnityEngine;

namespace MaestroZoo
{
    /// <summary>
    /// 个性化手势校准 — 设计文档要求：用户举手→四方向挥手→自动调整阈值。
    /// Personal gesture calibration: user raises hand → waves 4 directions → auto-adjust thresholds.
    /// </summary>
    public class PersonalGestureCalibrator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The gesture input system to calibrate.")]
        public RokidNativeGestureInput gestureInput;

        [Header("Calibration Settings")]
        [Tooltip("How long the user holds their hand steady before each step (seconds).")]
        public float steadyDuration = 1.0f;

        [Tooltip("How long each wave step lasts (seconds).")]
        public float waveRecordDuration = 2.5f;

        [Tooltip("Multiplier applied to recorded range to produce the move threshold. 0.4 = threshold is 40% of range.")]
        [Range(0.2f, 0.7f)]
        public float thresholdRatio = 0.4f;

        [Tooltip("Minimum move threshold after calibration (prevents over-sensitive detection).")]
        public float minMoveThreshold = 0.04f;

        [Tooltip("Maximum move threshold after calibration (prevents impossible detection).")]
        public float maxMoveThreshold = 0.30f;

        // --- State ---
        public enum Step
        {
            Idle,
            WaitingForHand,
            Steady,
            WaveUp,
            WaveDown,
            WaveLeft,
            WaveRight,
            Complete
        }

        public Step CurrentStep { get; private set; } = Step.Idle;

        /// <summary>0–1 progress within the current step.</summary>
        public float StepProgress { get; private set; }

        /// <summary>Overall calibration progress 0–1.</summary>
        public float OverallProgress
        {
            get
            {
                int stepIndex = (int)CurrentStep;
                if (CurrentStep == Step.Idle) return 0f;
                if (CurrentStep == Step.Complete) return 1f;
                int totalSteps = 7; // Idle→Wait→Steady→Up→Down→Left→Right→Complete
                return Mathf.Clamp01((stepIndex + StepProgress) / totalSteps);
            }
        }

        /// <summary>Human-readable instruction for the current step.</summary>
        public string CurrentInstruction => GetInstruction(CurrentStep);

        public CalibrationResult Result { get; private set; }

        // --- Events ---
        public event Action<Step> StepChanged;
        public event Action<CalibrationResult> Completed;

        // --- Internal ---
        private Vector3 baselinePosition;
        private float stepStartTime;
        private float peakDisplacement;
        private CalibrationResult pendingResult;

        private void Start()
        {
            if (gestureInput == null)
                gestureInput = GetComponent<RokidNativeGestureInput>();
        }

        /// <summary>
        /// Begin the calibration sequence. Call from UI button or debug panel.
        /// </summary>
        public void Begin()
        {
            if (gestureInput == null)
            {
                Debug.LogError("[GestureCalib] No RokidNativeGestureInput assigned.");
                return;
            }

            pendingResult = new CalibrationResult();
            GoToStep(Step.WaitingForHand);
        }

        /// <summary>
        /// Cancel calibration at any time.
        /// </summary>
        public void Cancel()
        {
            GoToStep(Step.Idle);
            pendingResult = default;
        }

        private void Update()
        {
            if (CurrentStep == Step.Idle || CurrentStep == Step.Complete)
                return;

            if (gestureInput == null)
                return;

            Vector3? handPos = GetActiveHandPosition();
            float stepElapsed = Time.time - stepStartTime;

            switch (CurrentStep)
            {
                case Step.WaitingForHand:
                    UpdateWaitingForHand(handPos, stepElapsed);
                    break;

                case Step.Steady:
                    UpdateSteady(handPos, stepElapsed);
                    break;

                case Step.WaveUp:
                case Step.WaveDown:
                case Step.WaveLeft:
                case Step.WaveRight:
                    UpdateWave(handPos, stepElapsed);
                    break;
            }
        }

        private void UpdateWaitingForHand(Vector3? handPos, float elapsed)
        {
            StepProgress = elapsed / steadyDuration;

            if (handPos.HasValue)
            {
                GoToStep(Step.Steady);
            }
        }

        private void UpdateSteady(Vector3? handPos, float elapsed)
        {
            StepProgress = elapsed / steadyDuration;

            if (!handPos.HasValue)
            {
                // Hand lost, go back
                GoToStep(Step.WaitingForHand);
                return;
            }

            if (elapsed >= steadyDuration)
            {
                baselinePosition = handPos.Value;
                GoToStep(Step.WaveUp);
            }
        }

        private void UpdateWave(Vector3? handPos, float elapsed)
        {
            StepProgress = Mathf.Clamp01(elapsed / waveRecordDuration);

            if (handPos.HasValue)
            {
                float displacement = GetTargetDisplacement(handPos.Value, CurrentStep);
                if (Mathf.Abs(displacement) > Mathf.Abs(peakDisplacement))
                    peakDisplacement = Mathf.Abs(displacement);
            }

            if (elapsed >= waveRecordDuration)
            {
                RecordStepResult(CurrentStep, peakDisplacement);

                // Advance to next step
                Step next = CurrentStep switch
                {
                    Step.WaveUp    => Step.WaveDown,
                    Step.WaveDown  => Step.WaveLeft,
                    Step.WaveLeft  => Step.WaveRight,
                    Step.WaveRight => Step.Complete,
                    _              => Step.Complete
                };

                if (next == Step.Complete)
                {
                    ComputeAndApply();
                }
                else
                {
                    // Reset baseline for next direction
                    if (handPos.HasValue)
                        baselinePosition = handPos.Value;
                    GoToStep(next);
                }
            }
        }

        private float GetTargetDisplacement(Vector3 current, Step step)
        {
            Vector3 delta = current - baselinePosition;
            return step switch
            {
                Step.WaveUp    =>  delta.y,
                Step.WaveDown  => -delta.y,
                Step.WaveLeft  => -delta.x,
                Step.WaveRight =>  delta.x,
                _              => delta.magnitude
            };
        }

        private void RecordStepResult(Step step, float peak)
        {
            switch (step)
            {
                case Step.WaveUp:    pendingResult.upRange = peak; break;
                case Step.WaveDown:  pendingResult.downRange = peak; break;
                case Step.WaveLeft:  pendingResult.leftRange = peak; break;
                case Step.WaveRight: pendingResult.rightRange = peak; break;
            }

            Debug.Log($"[GestureCalib] {step}: peak displacement = {peak:F3}m");
        }

        private void ComputeAndApply()
        {
            // Use the minimum of the 4 directional ranges as the basis
            float minRange = Mathf.Min(
                pendingResult.upRange,
                pendingResult.downRange,
                pendingResult.leftRange,
                pendingResult.rightRange);

            if (minRange < 0.01f)
            {
                Debug.LogWarning("[GestureCalib] Recorded range too small. " +
                                 "Using existing thresholds. Try larger gestures.");
                pendingResult.success = false;
                pendingResult.message = "手势幅度太小，请重试。Make larger gestures and retry.";
            }
            else
            {
                float computedThreshold = Mathf.Clamp(
                    minRange * thresholdRatio,
                    minMoveThreshold,
                    maxMoveThreshold);

                pendingResult.success = true;
                pendingResult.computedMoveThreshold = computedThreshold;
                pendingResult.message =
                    $"校准完成！\n" +
                    $"上:{pendingResult.upRange * 100:F1}cm " +
                    $"下:{pendingResult.downRange * 100:F1}cm " +
                    $"左:{pendingResult.leftRange * 100:F1}cm " +
                    $"右:{pendingResult.rightRange * 100:F1}cm\n" +
                    $"阈值: {computedThreshold * 100:F1}cm";

                // Apply to gesture input
                if (gestureInput != null)
                {
                    gestureInput.moveThreshold = computedThreshold;
                    Debug.Log($"[GestureCalib] Applied moveThreshold={computedThreshold * 100:F1}cm");
                }
            }

            Result = pendingResult;
            GoToStep(Step.Complete);
            Completed?.Invoke(Result);
        }

        private void GoToStep(Step step)
        {
            CurrentStep = step;
            stepStartTime = Time.time;
            StepProgress = 0f;
            peakDisplacement = 0f;
            StepChanged?.Invoke(step);
            Debug.Log($"[GestureCalib] Step: {step} — \"{GetInstruction(step)}\"");
        }

        private Vector3? GetActiveHandPosition()
        {
            if (gestureInput == null) return null;

            // Prefer right hand (baton hand), fallback to left
            if (gestureInput.IsRightHandTracked)
                return gestureInput.RightHandPosition;
            if (gestureInput.IsLeftHandTracked)
                return gestureInput.LeftHandPosition;
            return null;
        }

        private static string GetInstruction(Step step)
        {
            return step switch
            {
                Step.Idle           => "准备校准",
                Step.WaitingForHand => "请举手面对摄像头 (Raise your hand)",
                Step.Steady         => "保持不动... (Hold steady...)",
                Step.WaveUp         => "向上挥手 ↗ (Wave UP)",
                Step.WaveDown       => "向下挥手 ↘ (Wave DOWN)",
                Step.WaveLeft       => "向左挥手 ↙ (Wave LEFT)",
                Step.WaveRight      => "向右挥手 ↗ (Wave RIGHT)",
                Step.Complete       => "校准完成! (Calibration complete!)",
                _                   => ""
            };
        }

        private void OnDrawGizmos()
        {
            if (CurrentStep == Step.Idle || CurrentStep == Step.Complete)
                return;

            // Visualize baseline hand position for calibration feedback
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(baselinePosition, 0.05f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(baselinePosition, peakDisplacement);
        }
    }

    /// <summary>
    /// Result of a personal gesture calibration session.
    /// </summary>
    [System.Serializable]
    public struct CalibrationResult
    {
        public bool success;
        public string message;
        public float upRange;
        public float downRange;
        public float leftRange;
        public float rightRange;
        public float computedMoveThreshold;

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static CalibrationResult FromJson(string json)
        {
            return JsonUtility.FromJson<CalibrationResult>(json);
        }
    }
}
