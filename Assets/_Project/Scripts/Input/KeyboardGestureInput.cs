using System.Collections.Generic;
using System;
using UnityEngine;

namespace MaestroZoo
{
    public class KeyboardGestureInput : MonoBehaviour, IGestureInput
    {
        private readonly Queue<GestureEvent> bufferedGestures = new Queue<GestureEvent>();

        public event Action<GestureType, float> GestureCaptured;

        private void Update()
        {
            EnqueueIfPressed(KeyCode.W, GestureType.Up);
            EnqueueIfPressed(KeyCode.S, GestureType.Down);
            EnqueueIfPressed(KeyCode.A, GestureType.Left);
            EnqueueIfPressed(KeyCode.D, GestureType.Right);
            EnqueueIfPressed(KeyCode.Q, GestureType.Expand);
            EnqueueIfPressed(KeyCode.E, GestureType.Close);
        }

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

        private void EnqueueIfPressed(KeyCode key, GestureType gesture)
        {
            if (Input.GetKeyDown(key))
            {
                float inputTime = Time.time;
                bufferedGestures.Enqueue(new GestureEvent
                {
                    gesture = gesture,
                    time = inputTime
                });
                GestureCaptured?.Invoke(gesture, inputTime);
            }
        }

        private struct GestureEvent
        {
            public GestureType gesture;
            public float time;
        }
    }
}
