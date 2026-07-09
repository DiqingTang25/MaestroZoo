using System;
using UnityEngine;

namespace MaestroZoo
{
    public class ChartPlayer : MonoBehaviour
    {
        [Header("Chart")]
        public TextAsset chartJson;
        public AudioSource musicSource;

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
    }
}
