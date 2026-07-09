using UnityEngine;

namespace MaestroZoo
{
    [RequireComponent(typeof(AudioSource))]
    public class GameSfxPlayer : MonoBehaviour
    {
        public JudgeManager judgeManager;
        public GestureInputDispatcher gestureInput;

        [Range(0f, 1f)] public float volume = 0.24f;

        private AudioSource audioSource;
        private AudioClip perfectClip;
        private AudioClip goodClip;
        private AudioClip missClip;
        private AudioClip gestureClip;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            perfectClip = CreateTone("Perfect SFX", 1046f, 0.09f, 0.18f);
            goodClip = CreateTone("Good SFX", 784f, 0.08f, 0.14f);
            missClip = CreateTone("Miss SFX", 196f, 0.12f, 0.18f);
            gestureClip = CreateTone("Gesture SFX", 660f, 0.045f, 0.08f);
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (judgeManager != null)
            {
                judgeManager.NoteJudged -= OnNoteJudged;
                judgeManager.NoteJudged += OnNoteJudged;
                judgeManager.WrongGesture -= OnWrongGesture;
                judgeManager.WrongGesture += OnWrongGesture;
            }

            if (gestureInput != null)
            {
                if (gestureInput.nativeInput != null)
                {
                    gestureInput.nativeInput.GestureCaptured -= OnGestureCaptured;
                    gestureInput.nativeInput.GestureCaptured += OnGestureCaptured;
                }

                if (gestureInput.handInput != null)
                {
                    gestureInput.handInput.GestureCaptured -= OnGestureCaptured;
                    gestureInput.handInput.GestureCaptured += OnGestureCaptured;
                }
            }
        }

        private void Unsubscribe()
        {
            if (judgeManager != null)
            {
                judgeManager.NoteJudged -= OnNoteJudged;
                judgeManager.WrongGesture -= OnWrongGesture;
            }

            if (gestureInput != null)
            {
                if (gestureInput.nativeInput != null)
                    gestureInput.nativeInput.GestureCaptured -= OnGestureCaptured;
                if (gestureInput.handInput != null)
                    gestureInput.handInput.GestureCaptured -= OnGestureCaptured;
            }
        }

        private void OnNoteJudged(FlyingNote note, JudgeResult result)
        {
            if (result == JudgeResult.Perfect)
            {
                Play(perfectClip, 1f);
            }
            else if (result == JudgeResult.Good)
            {
                Play(goodClip, 0.85f);
            }
            else
            {
                Play(missClip, 0.9f);
            }
        }

        private void OnWrongGesture(GestureType gesture)
        {
            Play(missClip, 0.65f);
        }

        private void OnGestureCaptured(GestureType gesture, float time)
        {
            Play(gestureClip, 0.35f);
        }

        private void Play(AudioClip clip, float gain)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip, volume * gain);
            }
        }

        private static AudioClip CreateTone(string name, float frequency, float duration, float clipVolume)
        {
            const int sampleRate = 24000;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float fade = 1f - Mathf.Clamp01(t / duration);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * fade * clipVolume;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
