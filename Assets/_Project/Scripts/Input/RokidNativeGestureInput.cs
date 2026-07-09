using System;
using System.Collections.Generic;
using UnityEngine;
using Rokid.UXR.Interaction;

namespace MaestroZoo
{
    /// <summary>
    /// Rokid 原生手势输入适配器。
    /// 通过 GesEventInput.OnProcessGesData 获取手部骨骼数据，
    /// 检测挥动方向(Up/Down/Left/Right)和双手展开/合拢(Expand/Close)。
    /// 真机运行时自动激活，Editor 下退回 KeyboardGestureInput。
    /// </summary>
    public class RokidNativeGestureInput : MonoBehaviour, IGestureInput
    {
        [Header("Detection Thresholds")]
        [Tooltip("手在短时间内移动超过此距离(m)才算一次挥动")]
        public float moveThreshold = 0.12f;

        [Tooltip("检测窗口(s)，超过此时间重置追踪")]
        public float detectWindow = 0.4f;

        [Tooltip("挥动后冷却时间(s)，防止连续触发")]
        public float cooldown = 0.25f;

        [Tooltip("双手展开/合拢的距离变化阈值(m)")]
        public float expandContractThreshold = 0.15f;

        [Tooltip("确保主方向比次方向强多少倍才判定为有效挥动")]
        [Range(1f, 3f)]
        public float axisDominance = 1.25f;

        [Header("Pinch Detection")]
        [Tooltip("使用 pinch 距离变化来检测 Expand/Close (代替双手距离)")]
        public bool usePinchForExpandClose = true;

        [Tooltip("Pinch 距离变化阈值(m)")]
        public float pinchThreshold = 0.02f;

        // --- Public ---
        public bool IsTrackingAvailable { get; private set; }
        public event Action<GestureType, float> GestureCaptured;

        // --- IGestureInput ---
        public bool TryConsumeGesture(out GestureType gesture, out float inputTime)
        {
            if (bufferedGestures.Count > 0)
            {
                GestureEvent ev = bufferedGestures.Dequeue();
                gesture = ev.gesture;
                inputTime = ev.time;
                return true;
            }

            gesture = default;
            inputTime = default;
            return false;
        }

        // --- Internal State ---
        private readonly Queue<GestureEvent> bufferedGestures = new Queue<GestureEvent>();
        private readonly Dictionary<HandType, HandTracker> trackers = new()
        {
            { HandType.LeftHand, new HandTracker() },
            { HandType.RightHand, new HandTracker() }
        };

        private float lastGestureTime = -10f;
        private float? twoHandBaselineDistance;
        private float? leftPinchBaseline;
        private float? rightPinchBaseline;

        // --- Unity Lifecycle ---
        private void OnEnable()
        {
            GesEventInput.OnProcessGesData += HandleProcessGesData;
            GesEventInput.OnTrackedSuccess += HandleTrackedSuccess;
            GesEventInput.OnTrackedFailed += HandleTrackedFailed;
        }

        private void OnDisable()
        {
            GesEventInput.OnProcessGesData -= HandleProcessGesData;
            GesEventInput.OnTrackedSuccess -= HandleTrackedSuccess;
            GesEventInput.OnTrackedFailed -= HandleTrackedFailed;
        }

        private void Start()
        {
            if (GesEventInput.Instance == null)
            {
                Debug.Log("[RokidNative] GesEventInput not initialized — Editor 内请用 KeyboardGestureInput。");
            }
        }

        // --- Rokid Event Handlers ---
        private void HandleTrackedSuccess(HandType handType)
        {
            IsTrackingAvailable = true;
        }

        private void HandleTrackedFailed(HandType handType)
        {
            trackers[handType].MarkLost();
            twoHandBaselineDistance = null;

            if (!IsAnyHandTracked())
            {
                IsTrackingAvailable = false;
            }
        }

        private void HandleProcessGesData(HandType handType, GestureBean bean)
        {
            if (bean == null || bean.skeletons == null || bean.skeletons.Length < 22)
            {
                trackers[handType].MarkLost();
                return;
            }

            // 使用手掌中心位置(skeleton index 21 = PALM)
            Vector3 handCenter = bean.position;

            HandTracker tracker = trackers[handType];
            tracker.Update(handCenter, Time.time, detectWindow);

            // 检测 pinch-based Expand/Close
            if (usePinchForExpandClose)
            {
                UpdatePinchBaseline(handType, bean.pinchDistance);
            }

            // 尝试双手距离检测
            if (TryDetectTwoHandGesture())
            {
                return;
            }

            // 尝试单手掌势检测
            GestureType? detected = tracker.Detect(moveThreshold, axisDominance);
            if (detected.HasValue && TryBufferGesture(detected.Value))
            {
                tracker.ResetWindow();
            }
        }

        private void UpdatePinchBaseline(HandType handType, float pinchDistance)
        {
            if (pinchDistance <= 0f) return;

            if (handType == HandType.LeftHand)
            {
                if (!leftPinchBaseline.HasValue)
                {
                    leftPinchBaseline = pinchDistance;
                    return;
                }

                float delta = pinchDistance - leftPinchBaseline.Value;
                if (Mathf.Abs(delta) >= pinchThreshold)
                {
                    GestureType gesture = delta > 0f ? GestureType.Expand : GestureType.Close;
                    if (TryBufferGesture(gesture))
                    {
                        trackers[HandType.LeftHand].ResetWindow();
                    }
                    leftPinchBaseline = pinchDistance;
                }
            }
            else
            {
                if (!rightPinchBaseline.HasValue)
                {
                    rightPinchBaseline = pinchDistance;
                    return;
                }

                float delta = pinchDistance - rightPinchBaseline.Value;
                if (Mathf.Abs(delta) >= pinchThreshold)
                {
                    GestureType gesture = delta > 0f ? GestureType.Expand : GestureType.Close;
                    if (TryBufferGesture(gesture))
                    {
                        trackers[HandType.RightHand].ResetWindow();
                    }
                    rightPinchBaseline = pinchDistance;
                }
            }
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

            GestureType gesture = delta > 0f ? GestureType.Expand : GestureType.Close;
            if (!TryBufferGesture(gesture))
            {
                return false;
            }

            twoHandBaselineDistance = distance;
            left.ResetWindow();
            right.ResetWindow();
            return true;
        }

        private bool TryBufferGesture(GestureType gesture)
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
            GestureCaptured?.Invoke(gesture, inputTime);
            return true;
        }

        private bool IsAnyHandTracked()
        {
            return trackers[HandType.LeftHand].CurrentPosition.HasValue
                || trackers[HandType.RightHand].CurrentPosition.HasValue;
        }

        // --- Inner Types ---
        private struct GestureEvent
        {
            public GestureType gesture;
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

            public GestureType? Detect(float threshold, float dominance)
            {
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

                if (absY > absX * dominance)
                {
                    return delta.y > 0f ? GestureType.Up : GestureType.Down;
                }

                if (absX > absY * dominance)
                {
                    return delta.x > 0f ? GestureType.Right : GestureType.Left;
                }

                return null;
            }
        }
    }
}
