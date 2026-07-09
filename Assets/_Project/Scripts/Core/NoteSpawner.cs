using System.Collections.Generic;
using System;
using UnityEngine;

namespace MaestroZoo
{
    public class NoteSpawner : MonoBehaviour
    {
        public ChartPlayer chartPlayer;
        public FlyingNote notePrefab;
        public Transform spawnRoot;

        [Header("Lane Layout")]
        public float laneSpacing = 1.2f;
        public float spawnZ = 8f;
        public float judgeZ = 0f;
        public bool useXForwardLayout;
        public float spawnX = -3.2f;
        public float judgeX = 0.35f;
        public float y = 1.2f;

        private readonly List<FlyingNote> activeNotes = new List<FlyingNote>();
        private int nextNoteIndex;
        private ChartPlayer subscribedPlayer;

        public IReadOnlyList<FlyingNote> ActiveNotes => activeNotes;
        public event Action<FlyingNote> NoteSpawned;

        private void Reset()
        {
            chartPlayer = GetComponent<ChartPlayer>();
        }

        private void OnEnable()
        {
            UpdateSubscription();
        }

        private void OnDisable()
        {
            if (subscribedPlayer != null)
            {
                subscribedPlayer.PlaybackStarted -= ResetSpawner;
                subscribedPlayer.PlaybackStopped -= ClearNotes;
                subscribedPlayer = null;
            }
        }

        private void Update()
        {
            UpdateSubscription();

            if (chartPlayer == null || chartPlayer.Chart == null || notePrefab == null)
            {
                return;
            }

            SpawnDueNotes();
            CleanupJudgedNotes();
        }

        private void UpdateSubscription()
        {
            if (subscribedPlayer == chartPlayer)
            {
                return;
            }

            if (subscribedPlayer != null)
            {
                subscribedPlayer.PlaybackStarted -= ResetSpawner;
                subscribedPlayer.PlaybackStopped -= ClearNotes;
            }

            subscribedPlayer = chartPlayer;

            if (subscribedPlayer != null)
            {
                subscribedPlayer.PlaybackStarted += ResetSpawner;
                subscribedPlayer.PlaybackStopped += ClearNotes;
            }
        }

        private void ResetSpawner()
        {
            nextNoteIndex = 0;
            ClearNotes();
        }

        public void ClearNotes()
        {
            foreach (FlyingNote note in activeNotes)
            {
                if (note != null)
                {
                    Destroy(note.gameObject);
                }
            }
            activeNotes.Clear();
        }

        private void SpawnDueNotes()
        {
            ChartData chart = chartPlayer.Chart;
            if (chart.notes == null)
            {
                return;
            }

            while (nextNoteIndex < chart.notes.Length)
            {
                ChartNote note = chart.notes[nextNoteIndex];
                float spawnTime = note.time - chart.leadTime;
                if (chartPlayer.CompensatedSongTime < spawnTime)
                {
                    break;
                }

                Spawn(note, chart.leadTime);
                nextNoteIndex++;
            }
        }

        private void Spawn(ChartNote note, float leadTime)
        {
            Vector3 spawn = useXForwardLayout
                ? new Vector3(spawnX, y, note.lane * laneSpacing)
                : new Vector3(note.lane * laneSpacing, y, spawnZ);
            Vector3 target = useXForwardLayout
                ? new Vector3(judgeX, y, note.lane * laneSpacing)
                : new Vector3(note.lane * laneSpacing, y, judgeZ);

            Transform parent = spawnRoot != null ? spawnRoot : transform;
            FlyingNote instance = Instantiate(notePrefab, parent);
            instance.gameObject.SetActive(true);
            instance.Initialize(note, chartPlayer, spawn, target, leadTime);
            activeNotes.Add(instance);
            NoteSpawned?.Invoke(instance);
        }

        private void CleanupJudgedNotes()
        {
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                FlyingNote note = activeNotes[i];
                if (note == null)
                {
                    activeNotes.RemoveAt(i);
                    continue;
                }

                if (note.Judged && chartPlayer.CompensatedSongTime > note.Note.time + 0.6f)
                {
                    activeNotes.RemoveAt(i);
                    Destroy(note.gameObject);
                }
            }
        }
    }
}
