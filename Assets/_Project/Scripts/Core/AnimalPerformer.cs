using UnityEngine;

namespace MaestroZoo
{
    public class AnimalPerformer : MonoBehaviour
    {
        public string animalId;
        public string displayName;
        public Renderer bodyRenderer;
        public TextMesh label;

        [Header("Models")]
        [Tooltip("Idle model (always visible, subtle bounce)")]
        public GameObject idleModel;

        [Tooltip("Score animation model (briefly shown on hit)")]
        public GameObject scoreModel;

        [Tooltip("How long the score model stays visible (seconds)")]
        public float scoreModelDuration = 0.55f;

        private Color baseColor;
        private Vector3 baseScale;
        private Vector3 basePosition;
        private float excitement;
        private float shake;
        private float scoreModelTimer;

        private void Awake()
        {
            baseScale = transform.localScale;
            basePosition = transform.localPosition;

            if (bodyRenderer == null)
            {
                // Prefer idle model renderer, fallback to any child renderer
                if (idleModel != null)
                    bodyRenderer = idleModel.GetComponentInChildren<Renderer>();
                if (bodyRenderer == null)
                    bodyRenderer = GetComponentInChildren<Renderer>();
            }

            if (bodyRenderer != null)
            {
                baseColor = bodyRenderer.material.color;
            }

            // Start with idle model visible, score model hidden
            ShowIdleModel();
        }

        private void Update()
        {
            excitement = Mathf.MoveTowards(excitement, 0f, Time.deltaTime * 1.8f);
            shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime * 3f);

            float bounce = Mathf.Sin(Time.time * 10f) * 0.08f * excitement;
            float wobble = Mathf.Sin(Time.time * 24f) * 0.06f * shake;
            transform.localPosition = basePosition + new Vector3(wobble, Mathf.Abs(bounce), 0f);
            transform.localScale = baseScale * (1f + excitement * 0.18f);

            if (bodyRenderer != null)
            {
                Color target = Color.Lerp(baseColor, Color.white, excitement * 0.65f);
                bodyRenderer.material.color = target;
            }

            // Score model timer — switch back to idle when expired
            if (scoreModelTimer > 0f)
            {
                scoreModelTimer -= Time.deltaTime;
                if (scoreModelTimer <= 0f)
                {
                    ShowIdleModel();
                }
            }
        }

        public void React(JudgeResult result)
        {
            if (result == JudgeResult.Miss)
            {
                Miss();
            }
            else
            {
                Hit(result == JudgeResult.Perfect ? 1f : 0.65f);
            }
        }

        public void Hit(float amount)
        {
            excitement = Mathf.Clamp01(excitement + amount);
            ShowScoreModel();
        }

        public void Miss()
        {
            shake = 1f;
            excitement = 0.15f;
        }

        public void SetMood(float mood01)
        {
            if (bodyRenderer == null)
            {
                return;
            }

            Color calm = baseColor * 0.75f;
            calm.a = 1f;
            Color happy = Color.Lerp(baseColor, Color.white, 0.35f);
            bodyRenderer.material.color = Color.Lerp(calm, happy, mood01);
        }

        private void ShowScoreModel()
        {
            if (idleModel != null) idleModel.SetActive(false);
            if (scoreModel != null) scoreModel.SetActive(true);
            scoreModelTimer = scoreModelDuration;
        }

        private void ShowIdleModel()
        {
            scoreModelTimer = 0f;
            if (idleModel != null) idleModel.SetActive(true);
            if (scoreModel != null) scoreModel.SetActive(false);
        }
    }
}

