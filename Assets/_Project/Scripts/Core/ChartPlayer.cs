using System;
using UnityEngine;

namespace MaestroZoo
{
    public class ChartPlayer : MonoBehaviour
    {
        [Header("Chart")]
        public TextAsset chartJson;
        public AudioSource musicSource;

        [Header("Fallback Audio")]
        public bool generatePlaceholderAudio = true;
        public float placeholderVolume = 0.18f;
        public int placeholderSampleRate = 24000;

        public ChartData Chart { get; private set; }
        public float SongTime { get; private set; }
        public bool IsPlaying { get; private set; }
        public float ChartEndTime { get; private set; }

        public event Action<ChartData> ChartLoaded;
        public event Action PlaybackStarted;
        public event Action PlaybackStopped;
        public event Action PlaybackEnded;

        private float startDspTime;
        private bool endRaised;

        private void Awake()
        {
            if (chartJson != null)
            {
                LoadChart();
            }
        }

        private void Update()
        {
            if (!IsPlaying)
            {
                return;
            }

            SongTime = (float)(AudioSettings.dspTime - startDspTime);

            if (!endRaised && Chart != null && SongTime >= ChartEndTime + 1f)
            {
                endRaised = true;
                IsPlaying = false;
                PlaybackEnded?.Invoke();
            }
        }

        [ContextMenu("Load Chart")]
        public void LoadChart()
        {
            if (chartJson == null)
            {
                Debug.LogWarning("ChartPlayer needs a chart JSON file.");
                return;
            }

            Chart = JsonUtility.FromJson<ChartData>(chartJson.text);
            ChartEndTime = Chart != null ? Chart.GetEndTime() : 0f;
            ChartLoaded?.Invoke(Chart);
        }

        public void LoadChart(TextAsset nextChart)
        {
            chartJson = nextChart;
            LoadChart();
        }

        [ContextMenu("Start Song")]
        public void StartSong()
        {
            if (Chart == null)
            {
                LoadChart();
            }

            if (Chart == null)
            {
                return;
            }

            startDspTime = (float)AudioSettings.dspTime;
            SongTime = 0f;
            IsPlaying = true;
            endRaised = false;

            if (musicSource != null && musicSource.clip != null)
            {
                musicSource.Stop();
                musicSource.Play();
            }
            else if (generatePlaceholderAudio && musicSource != null)
            {
                musicSource.Stop();
                musicSource.clip = CreatePlaceholderClip();
                musicSource.Play();
            }

            PlaybackStarted?.Invoke();
        }

        public void StartSong(TextAsset nextChart)
        {
            LoadChart(nextChart);
            StartSong();
        }

        public void StopSong()
        {
            IsPlaying = false;
            SongTime = 0f;
            endRaised = false;

            if (musicSource != null)
            {
                musicSource.Stop();
            }

            PlaybackStopped?.Invoke();
        }

        private AudioClip CreatePlaceholderClip()
        {
            float duration = Mathf.Max(ChartEndTime + 1f, 4f);
            int sampleCount = Mathf.CeilToInt(duration * placeholderSampleRate);
            float[] samples = new float[sampleCount];
            float beatInterval = Chart != null && Chart.bpm > 0 ? 60f / Chart.bpm : 0.5f;
            float strongBeatInterval = beatInterval * 4f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / placeholderSampleRate;
                float beatPhase = Mathf.Repeat(t, beatInterval);
                float strongPhase = Mathf.Repeat(t, strongBeatInterval);
                float envelope = Mathf.Exp(-beatPhase * 28f);
                bool strongBeat = strongPhase < beatInterval * 0.35f;
                float frequency = strongBeat ? 880f : 660f;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * placeholderVolume;
            }

            AudioClip clip = AudioClip.Create("Generated Placeholder Beat", sampleCount, 1, placeholderSampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
