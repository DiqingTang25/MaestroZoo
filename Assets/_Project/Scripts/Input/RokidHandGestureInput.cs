using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

namespace MaestroZoo
{
    public class RokidHandGestureInput : MonoBehaviour, IGestureInput
    {
        [Header("XR")]
        [Tooltip("Optional XROrigin/rig transform used to convert XR hand poses into scene space.")]
        public Transform xrOrigin;

        [Header("Detection Thresholds")]
        [Tooltip("Minimum hand travel, in meters, required for a directional swipe.")]
        public float moveThreshold = 0.12f;

        [Tooltip("How long a gesture sample window stays open, in seconds.")]
        public float detectWindow = 0.4f;

        [Tooltip("Minimum time between accepted gestures, in seconds.")]
        public float cooldown = 0.25f;

        [Tooltip("Minimum two-hand distance change, in meters, for expand/close.")]
        public float expandContractThreshold = 0.15f;

        [Tooltip("Ignore movement that is not this much stronger on the winning axis.")]
        [Range(1f, 3f)]
        public float axisDominance = 1.25f;

        public bool IsTrackingAvailable => handSubsystem != null && handSubsystem.running;
        public event Action<GestureType, float> GestureCaptured;

        private readonly Queue<GestureEvent> bufferedGestures = new Queue<GestureEvent>();
        private readonly Dictionary<Handedness, HandTracker> trackers = new Dictionary<Handedness, HandTracker>
        {
            { Handedness.Left, new HandTracker() },
            { Handedness.Right, new HandTracker() }
        };

        private XRHandSubsystem handSubsystem;
        private float lastGestureTime = -10f;
        private float? twoHandBaselineDistance;

        public bool TryConsumeGesture(out GestureType gesture, out float inputTime)
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
            TryAttachSubsystem();
        }

        private void Start()
        {
            TryAttachSubsystem();

            if (handSubsystem == null)
            {
                Debug.Log("[RokidHand] No XRHandSubsystem found. Rokid native gesture input is still the primary source.");
            }
        }

        private void OnDisable()
        {
            DetachSubsystem();
        }

        private void TryAttachSubsystem()
        {
            XRHandSubsystem subsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRHandSubsystem>();
            if (subsystem == null || subsystem == handSubsystem)
            {
                return;
            }

            DetachSubsystem();
            handSubsystem = subsystem;
            handSubsystem.updatedHands += OnHandsUpdated;
        }

        private void DetachSubsystem()
        {
            if (handSubsystem != null)
            {
                handSubsystem.updatedHands -= OnHandsUpdated;
                handSubsystem = null;
            }
        }

        private void OnHandsUpdated(
            XRHandSubsystem subsystem,
            XRHandSubsystem.UpdateSuccessFlags successFlags,
            XRHandSubsystem.UpdateType updateType)
        {
            if (updateType != XRHandSubsystem.UpdateType.Dynamic)
            {
                return;
            }

            UpdateTracker(subsystem.leftHand, Handedness.Left);
            UpdateTracker(subsystem.rightHand, Handedness.Right);

            if (TryBufferTwoHandGesture())
            {
                return;
            }

            foreach (HandTracker tracker in trackers.Values)
            {
                GestureType? gesture = tracker.Detect(moveThreshold, axisDominance);
                if (gesture.HasValue && BufferGesture(gesture.Value))
                {
                    tracker.ResetWindow();
                    return;
                }
            }
        }

        private void UpdateTracker(XRHand hand, Handedness handedness)
        {
            if (!hand.isTracked || !TryGetJointPosition(hand, XRHandJointID.MiddleProximal, out Vector3 position))
            {
                trackers[handedness].MarkLost();
                twoHandBaselineDistance = null;
                return;
            }

            trackers[handedness].Update(position, Time.time, detectWindow);
        }

        private bool TryBufferTwoHandGesture()
        {
            HandTracker left = trackers[Handedness.Left];
            HandTracker right = trackers[Handedness.Right];
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
            if (!BufferGesture(gesture))
            {
                return false;
            }

            twoHandBaselineDistance = distance;
            left.ResetWindow();
            right.ResetWindow();
            return true;
        }

        private bool BufferGesture(GestureType gesture)
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

        private bool TryGetJointPosition(XRHand hand, XRHandJointID jointId, out Vector3 position)
        {
            XRHandJoint joint = hand.GetJoint(jointId);
            if (!joint.TryGetPose(out Pose pose))
            {
                position = default;
                return false;
            }

            position = xrOrigin != null ? xrOrigin.TransformPoint(pose.position) : pose.position;
            return true;
        }

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
