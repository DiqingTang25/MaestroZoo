using UnityEngine;

namespace MaestroZoo
{
    public class AnimalPerformer : MonoBehaviour
    {
        public string animalId;
        public string displayName;
        public Renderer bodyRenderer;
        public TextMesh label;

        private Color baseColor;
        private Vector3 baseScale;
        private Vector3 basePosition;
        private float excitement;
        private float shake;

        private void Awake()
        {
            baseScale = transform.localScale;
            basePosition = transform.localPosition;

            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<Renderer>();
            }

            if (bodyRenderer != null)
            {
                baseColor = bodyRenderer.material.color;
            }
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
    }
}

