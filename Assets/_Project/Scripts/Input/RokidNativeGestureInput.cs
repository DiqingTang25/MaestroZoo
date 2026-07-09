using System;
using System.Collections.Generic;
using Rokid.UXR.Interaction;
using Rokid.UXR.Utility;
using UnityEngine;

namespace MaestroZoo
{
    public class RokidNativeGestureInput : MonoBehaviour, IGestureInput
    {
        [Header("Detection Thresholds")]
        public float moveThreshold = 0.12f;
        public float detectWindow = 0.4f;
        public float cooldown = 0.25f;
        public float expandContractThreshold = 0.15f;

        [Range(1f, 3f)]
        public float axisDominance = 1.25f;

        [Header("Pinch Detection")]
        public bool usePinchForExpandClose = true;
        public float pinchThreshold = 0.02f;

        [Header("Confidence")]
        [Tooltip("手势位移主方向/次方向比值低于此值则丢弃。降低 = 更宽松。")]
        [Range(0.3f, 2.0f)]
        public float minConfidence = 0.8f;

        [Header("Preset")]
        [Tooltip("可选：拖入 GestureThresholdPreset，Start 时自动应用。")]
        public GestureThresholdPreset preset;

        public bool IsTrackingAvailable { get; private set; }
        public event Action<MaestroZoo.GestureType, float> GestureCaptured;

        // --- Device Readiness ---
        public enum ReadinessState
        {
            Unknown,
            Initializing,
            Ready,
            Error_NoGesEventInput,
            Error_NoHandTracking
        }

        public ReadinessState DeviceReadiness { get; private set; } = ReadinessState.Unknown;
        public string DeviceReadinessMessage { get; private set; } = "Not initialized.";

        // Track which gestures have been detected this session (for device verification)
        private readonly HashSet<MaestroZoo.GestureType> gesturesDetectedThisSession = new HashSet<MaestroZoo.GestureType>();
        public bool AllGesturesDetected => gesturesDetectedThisSession.Count >= 6;
        public string DetectedGesturesReport => $"Detected [{gesturesDetectedThisSession.Count}/6]: {string.Join(", ", gesturesDetectedThisSession)}";

        // --- Debug Info (read by RokidDebugPanel) ---
        public bool IsLeftHandTracked => trackers[HandType.LeftHand].CurrentPosition.HasValue;
        public bool IsRightHandTracked => trackers[HandType.RightHand].CurrentPosition.HasValue;
        public Vector3 LeftHandPosition => trackers[HandType.LeftHand].CurrentPosition ?? Vector3.zero;
        public Vector3 RightHandPosition => trackers[HandType.RightHand].CurrentPosition ?? Vector3.zero;
        public float LeftPinchDistance { get; private set; }
        public float RightPinchDistance { get; private set; }
        public float TwoHandDistance { get; private set; }
        public MaestroZoo.GestureType LastGesture { get; private set; }
        public float LastGestureTimestamp { get; private set; }
        public float LastConfidence { get; private set; }

        // Gesture history ring buffer (last 8 entries for debug)
        public readonly List<GestureRecord> GestureHistory = new(8);

        private readonly Queue<GestureEvent> bufferedGestures = new Queue<GestureEvent>();
        private readonly Dictionary<HandType, HandTracker> trackers = new Dictionary<HandType, HandTracker>
        {
            { HandType.LeftHand, new HandTracker() },
            { HandType.RightHand, new HandTracker() }
        };

        private float lastGestureTime = -10f;
        private float? twoHandBaselineDistance;
        private float? leftPinchBaseline;
        private float? rightPinchBaseline;

        public bool TryConsumeGesture(out MaestroZoo.GestureType gesture, out float inputTime)
        {
            if (bufferedGestures.Count > 0)
            {
                GestureEvent gestureEvent = bufferedGestures.Dequeue();
                gesture = gestureEvent.gesture;
                inputTime = gestureEvent.time;
                return true;
            }

            gesture = default;
            inputTime = default;
            return false;
        }

        private void OnEnable()
        {
            try
            {
                GesEventInput.OnProcessGesData += HandleProcessGesData;
                GesEventInput.OnTrackedSuccess += HandleTrackedSuccess;
                GesEventInput.OnTrackedFailed += HandleTrackedFailed;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RokidNative] Failed to subscribe to GesEventInput events: {ex.Message}");
                DeviceReadiness = ReadinessState.Error_NoGesEventInput;
                DeviceReadinessMessage = $"GesEventInput subscription failed: {ex.Message}";
            }
        }

        private void OnDisable()
        {
            try
            {
                GesEventInput.OnProcessGesData -= HandleProcessGesData;
                GesEventInput.OnTrackedSuccess -= HandleTrackedSuccess;
                GesEventInput.OnTrackedFailed -= HandleTrackedFailed;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RokidNative] Error unsubscribing from GesEventInput: {ex.Message}");
            }
        }

        private void Start()
        {
            DeviceReadiness = ReadinessState.Initializing;
            DeviceReadinessMessage = "Checking GesEventInput availability...";

            if (preset != null)
            {
                preset.ApplyTo(this);
            }

            if (GesEventInput.Instance == null)
            {
                DeviceReadiness = ReadinessState.Error_NoGesEventInput;
                DeviceReadinessMessage = "GesEventInput.Instance is null — not running on Rokid device or gesture service unavailable.";
                Debug.LogWarning($"[RokidNative] {DeviceReadinessMessage}");
            }
            else
            {
                DeviceReadiness = ReadinessState.Ready;
                DeviceReadinessMessage = "GesEventInput available. Waiting for hand tracking...";
                Debug.Log("[RokidNative] GesEventInput initialized. Ready for hand tracking.");
            }
        }

        private void HandleTrackedSuccess(HandType handType)
        {
            IsTrackingAvailable = true;
        }

        private void HandleTrackedFailed(HandType handType)
        {
            if (trackers.TryGetValue(handType, out HandTracker tracker))
            {
                tracker.MarkLost();
            }

            ResetDistanceBaselines();
            IsTrackingAvailable = IsAnyHandTracked();

            if (!IsTrackingAvailable && DeviceReadiness == ReadinessState.Ready)
            {
                DeviceReadiness = ReadinessState.Error_NoHandTracking;
                DeviceReadinessMessage = $"Hand tracking lost ({handType}).";
                Debug.LogWarning($"[RokidNative] {DeviceReadinessMessage}");
            }
        }

        private void HandleProcessGesData(HandType handType, GestureBean bean)
        {
            if (!trackers.ContainsKey(handType))
            {
                return;
            }

            if (bean == null || bean.skeletons == null || bean.skeletons.Length == 0)
            {
                trackers[handType].MarkLost();
                ResetDistanceBaselines();
                IsTrackingAvailable = IsAnyHandTracked();
                return;
            }

            IsTrackingAvailable = true;

            Vector3 handCenter = bean.position;
            HandTracker tracker = trackers[handType];
            tracker.Update(handCenter, Time.time, detectWindow);

            // Capture debug info
            if (handType == HandType.LeftHand)
                LeftPinchDistance = bean.pinchDistance;
            else
                RightPinchDistance = bean.pinchDistance;

            if (IsLeftHandTracked && IsRightHandTracked)
                TwoHandDistance = Vector3.Distance(LeftHandPosition, RightHandPosition);

            if (usePinchForExpandClose)
            {
                TryDetectPinchGesture(handType, bean.pinchDistance);
            }

            if (TryDetectTwoHandGesture())
            {
                return;
            }

            MaestroZoo.GestureType? detected = tracker.Detect(moveThreshold, axisDominance, minConfidence, out float conf);
            if (detected.HasValue && TryBufferGesture(detected.Value))
            {
                LastConfidence = conf;
                tracker.ResetWindow();
            }
        }

        private void TryDetectPinchGesture(HandType handType, float pinchDistance)
        {
            if (pinchDistance <= 0f)
            {
                return;
            }

            ref float? baseline = ref GetPinchBaseline(handType);
            if (!baseline.HasValue)
            {
                baseline = pinchDistance;
                return;
            }

            float delta = pinchDistance - baseline.Value;
            if (Mathf.Abs(delta) < pinchThreshold)
            {
                return;
            }

            MaestroZoo.GestureType gesture = delta > 0f ? MaestroZoo.GestureType.Expand : MaestroZoo.GestureType.Close;
            if (TryBufferGesture(gesture))
            {
                trackers[handType].ResetWindow();
                baseline = pinchDistance;
            }
        }

        private ref float? GetPinchBaseline(HandType handType)
        {
            if (handType == HandType.LeftHand)
            {
                return ref leftPinchBaseline;
            }

            return ref rightPinchBaseline;
        }

        private bool TryDetectTwoHandGesture()
        {
            HandTracker left = trackers[HandType.LeftHand];
            HandTracker right = trackers[HandType.RightHand];
            if (!left.CurrentPosition.HasValue || !right.CurrentPosition.HasValue)
            {
                twoHandBaselineDistance = null;
                return false;
            }

            float distance = Vector3.Distance(left.CurrentPosition.Value, right.CurrentPosition.Value);
            if (!twoHandBaselineDistance.HasValue)
            {
                twoHandBaselineDistance = distance;
                return false;
            }

            float delta = distance - twoHandBaselineDistance.Value;
            if (Mathf.Abs(delta) < expandContractThreshold)
            {
                return false;
            }

            MaestroZoo.GestureType gesture = delta > 0f ? MaestroZoo.GestureType.Expand : MaestroZoo.GestureType.Close;
            if (!TryBufferGesture(gesture))
            {
                return false;
            }

            twoHandBaselineDistance = distance;
            left.ResetWindow();
            right.ResetWindow();
            return true;
        }

        private bool TryBufferGesture(MaestroZoo.GestureType gesture)
        {
            if (Time.time - lastGestureTime <= cooldown)
            {
                return false;
            }

            float inputTime = Time.time;
            bufferedGestures.Enqueue(new GestureEvent
            {
                gesture = gesture,
                time = inputTime
            });
            lastGestureTime = inputTime;
            LastGesture = gesture;
            LastGestureTimestamp = inputTime;
            GestureCaptured?.Invoke(gesture, inputTime);

            // Track gesture coverage for real-device verification
            gesturesDetectedThisSession.Add(gesture);

            // Record history (ring buffer, newest first)
            GestureHistory.Insert(0, new GestureRecord
            {
                gesture = gesture,
                time = inputTime,
                confidence = LastConfidence
            });
            if (GestureHistory.Count > 8)
                GestureHistory.RemoveAt(GestureHistory.Count - 1);

            return true;
        }

        public struct GestureRecord
        {
            public MaestroZoo.GestureType gesture;
            public float time;
            public float confidence;
        }

        private bool IsAnyHandTracked()
        {
            return trackers[HandType.LeftHand].CurrentPosition.HasValue ||
                trackers[HandType.RightHand].CurrentPosition.HasValue;
        }

        private void ResetDistanceBaselines()
        {
            twoHandBaselineDistance = null;
            leftPinchBaseline = null;
            rightPinchBaseline = null;
        }

        // --- Gesture Calibration (exposed for UI) ---
        public bool IsCalibrating { get; private set; }
        public event Action<int> CalibrationStateChanged;
        public event Action CalibrationCompleted;

        public void BeginCalibration()
        {
            if (!Utils.IsAndroidPlatform())
            {
                Debug.Log("[RokidNative] Gesture calibration is only available on device.");
                return;
            }

            Rokid.UXR.Native.NativeInterface.NativeAPI.OnGesCalibStateChange += HandleCalibState;
            Rokid.UXR.Native.NativeInterface.NativeAPI.BeginGestureCalibrate();
            IsCalibrating = true;
            Debug.Log("[RokidNative] Gesture calibration started.");
        }

        public void StopCalibration()
        {
            Rokid.UXR.Native.NativeInterface.NativeAPI.StopGestureCalibrate();
            Rokid.UXR.Native.NativeInterface.NativeAPI.OnGesCalibStateChange -= HandleCalibState;
            IsCalibrating = false;
            Debug.Log("[RokidNative] Gesture calibration stopped.");
        }

        private void HandleCalibState(int state)
        {
            CalibrationStateChanged?.Invoke(state);
            Debug.Log($"[RokidNative] Calibration state: {state}");

            if (state == 0)
            {
                IsCalibrating = false;
                Rokid.UXR.Native.NativeInterface.NativeAPI.OnGesCalibStateChange -= HandleCalibState;
                CalibrationCompleted?.Invoke();
            }
        }

        private struct GestureEvent
        {
            public MaestroZoo.GestureType gesture;
            public float time;
        }

        private class HandTracker
        {
            public Vector3? CurrentPosition { get; private set; }

            private Vector3? firstPosition;
            private float firstTime;

            public void Update(Vector3 position, float time, float window)
            {
                CurrentPosition = position;

                if (!firstPosition.HasValue || time - firstTime > window)
                {
                    firstPosition = position;
                    firstTime = time;
                }
            }

            public void MarkLost()
            {
                CurrentPosition = null;
                firstPosition = null;
            }

            public void ResetWindow()
            {
                firstPosition = CurrentPosition;
                firstTime = Time.time;
            }

            public MaestroZoo.GestureType? Detect(float threshold, float dominance, float minConf, out float confidence)
            {
                confidence = 0f;

                if (!firstPosition.HasValue || !CurrentPosition.HasValue)
                {
                    return null;
                }

                Vector3 delta = CurrentPosition.Value - firstPosition.Value;
                float absX = Mathf.Abs(delta.x);
                float absY = Mathf.Abs(delta.y);

                if (absX < threshold && absY < threshold)
                {
                    return null;
                }

                float maxAxis = Mathf.Max(absX, absY);
                float minAxis = Mathf.Min(absX, absY);
                confidence = maxAxis > 0.001f ? 1f - (minAxis / maxAxis) : 0f;

                if (confidence < minConf)
                {
                    return null;
                }

                if (absY > absX * dominance)
                {
                    return delta.y > 0f ? MaestroZoo.GestureType.Up : MaestroZoo.GestureType.Down;
                }

                if (absX > absY * dominance)
                {
                    return delta.x > 0f ? MaestroZoo.GestureType.Right : MaestroZoo.GestureType.Left;
                }

                return null;
            }
        }
    }
}
