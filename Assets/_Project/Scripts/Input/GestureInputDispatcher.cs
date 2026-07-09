using UnityEngine;

namespace MaestroZoo
{
    /// <summary>
    /// Auto-selects the best available gesture input source each frame.
    /// Priority: Rokid native (GesEventInput) > XRHandSubsystem.
    /// Attach to the same GameObject as JudgeManager and assign inputBehaviour.
    /// </summary>
    public class GestureInputDispatcher : MonoBehaviour, IGestureInput
    {
        [Header("Input sources (checked in priority order)")]
        [Tooltip("Rokid native hand tracking via GesEventInput (production path on Rokid devices).")]
        public RokidNativeGestureInput nativeInput;

        [Tooltip("Unity XRHandSubsystem hand tracking (fallback for other XR devices).")]
        public RokidHandGestureInput handInput;

        public string ActiveSourceName { get; private set; } = "None";

        public bool TryConsumeGesture(out GestureType gesture, out float inputTime)
        {
            // Priority 1: Rokid native (GesEventInput pipeline)
            if (TrySource(nativeInput, "RokidNative", out gesture, out inputTime))
                return true;

            // Priority 2: Unity XRHandSubsystem
            if (TrySource(handInput, "XRHand", out gesture, out inputTime))
                return true;

            gesture = default;
            inputTime = default;
            return false;
        }

        private bool TrySource(IGestureInput source, string label, out GestureType gesture, out float inputTime)
        {
            if (source == null)
            {
                gesture = default;
                inputTime = default;
                return false;
            }

            bool eligible = source switch
            {
                RokidNativeGestureInput n => n.isActiveAndEnabled && n.IsTrackingAvailable,
                RokidHandGestureInput h   => h.isActiveAndEnabled && h.IsTrackingAvailable,
                _                         => true
            };

            if (eligible && source.TryConsumeGesture(out gesture, out inputTime))
            {
                ActiveSourceName = label;
                return true;
            }

            gesture = default;
            inputTime = default;
            return false;
        }
    }
}
