using UnityEngine;

namespace MaestroZoo
{
    public class FlyingNote : MonoBehaviour
    {
        public ChartNote Note { get; private set; }
        public bool Judged { get; private set; }
        public JudgeResult? Result { get; private set; }

        private ChartPlayer chartPlayer;
        private Vector3 spawnPosition;
        private Vector3 targetPosition;
        private float spawnSongTime;
        private float hitSongTime;
        private Vector3 baseScale;
        private Renderer[] renderers;

        public void Initialize(
            ChartNote note,
            ChartPlayer player,
            Vector3 spawn,
            Vector3 target,
            float leadTime)
        {
            Note = note;
            chartPlayer = player;
            spawnPosition = spawn;
            targetPosition = target;
            hitSongTime = note.time;
            spawnSongTime = hitSongTime - leadTime;
            transform.position = spawnPosition;
            baseScale = transform.localScale;
            renderers = GetComponentsInChildren<Renderer>();
            gameObject.name = $"Note_{note.gesture}_{note.time:0.00}";

            GestureNoteVisual visual = GetComponent<GestureNoteVisual>();
            if (visual != null)
            {
                visual.Apply(note.GestureType);
            }
        }

        private void Update()
        {
            if (chartPlayer == null)
            {
                return;
            }

            float duration = Mathf.Max(0.01f, hitSongTime - spawnSongTime);
            float t = Mathf.InverseLerp(spawnSongTime, hitSongTime, chartPlayer.SongTime);
            transform.position = Vector3.LerpUnclamped(spawnPosition, targetPosition, t);
            float pulse = 1f + Mathf.Sin(Time.time * 7f) * 0.04f;
            transform.localScale = baseScale * pulse;

            if (!Judged && chartPlayer.SongTime > hitSongTime + 0.35f)
            {
                MarkJudged(JudgeResult.Miss);
            }

            if (Judged)
            {
                float judgedAge = Mathf.Clamp01((chartPlayer.SongTime - hitSongTime) / 0.6f);
                transform.localScale = Vector3.Lerp(baseScale * 1.25f, Vector3.zero, judgedAge);
            }
        }

        public void MarkJudged()
        {
            Judged = true;
        }

        public void MarkJudged(JudgeResult result)
        {
            Judged = true;
            Result = result;
            TintAfterJudgement(result);
        }

        private void TintAfterJudgement(JudgeResult result)
        {
            if (renderers == null)
            {
                return;
            }

            Color color = result == JudgeResult.Miss ? new Color(0.8f, 0.12f, 0.12f) : new Color(1f, 1f, 1f);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].material.color = color;
                }
            }
        }
    }
}
