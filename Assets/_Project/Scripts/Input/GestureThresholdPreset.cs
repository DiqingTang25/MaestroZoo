using UnityEngine;

namespace MaestroZoo
{
    /// <summary>
    /// 手势阈值预设。比赛现场 Inspector 一键切换，无需重新编译。
    /// 创建: Assets > Create > MaestroZoo > Gesture Threshold Preset
    /// </summary>
    [CreateAssetMenu(fileName = "GesturePreset_", menuName = "MaestroZoo/Gesture Threshold Preset", order = 100)]
    public class GestureThresholdPreset : ScriptableObject
    {
        [Header("Swipe Detection")]
        [Tooltip("手移动超过此距离(m)判定为有效挥动。越小越灵敏。")]
        [Range(0.04f, 0.30f)]
        public float moveThreshold = 0.12f;

        [Tooltip("手势采样窗口(s)。太短来不及挥动，太长合并多个手势。")]
        [Range(0.15f, 0.80f)]
        public float detectWindow = 0.40f;

        [Tooltip("手势冷却(s)，防止连续误触发。")]
        [Range(0.10f, 0.60f)]
        public float cooldown = 0.25f;

        [Tooltip("主方向必须比次方向强多少倍。越大越区分上下/左右。")]
        [Range(1.0f, 2.5f)]
        public float axisDominance = 1.25f;

        [Header("Expand / Close")]
        [Tooltip("双手间距变化超过此距离(m)判定为 Expand/Close。")]
        [Range(0.05f, 0.30f)]
        public float expandContractThreshold = 0.15f;

        [Tooltip("Pinch 单帧变化超过此距离(m)判定为 Expand/Close。")]
        [Range(0.005f, 0.05f)]
        public float pinchThreshold = 0.02f;

        [Header("Confidence")]
        [Tooltip("手势方向位移必须超过此倍率的次方向位移才接受。低于 = 丢弃。")]
        [Range(0.5f, 2.0f)]
        public float minConfidence = 0.8f;

        // --- Factory Presets ---
        public static GestureThresholdPreset Competition => FromValues(
            "Competition", 0.12f, 0.40f, 0.25f, 1.25f, 0.15f, 0.02f, 0.8f);

        public static GestureThresholdPreset Sensitive => FromValues(
            "Sensitive", 0.08f, 0.30f, 0.18f, 1.10f, 0.10f, 0.012f, 0.6f);

        public static GestureThresholdPreset Stable => FromValues(
            "Stable", 0.18f, 0.50f, 0.35f, 1.50f, 0.20f, 0.03f, 1.0f);

        private static GestureThresholdPreset FromValues(
            string name, float move, float window, float cd,
            float dominance, float expand, float pinch, float confidence)
        {
            var p = CreateInstance<GestureThresholdPreset>();
            p.name = name;
            p.moveThreshold = move;
            p.detectWindow = window;
            p.cooldown = cd;
            p.axisDominance = dominance;
            p.expandContractThreshold = expand;
            p.pinchThreshold = pinch;
            p.minConfidence = confidence;
            return p;
        }

        public void ApplyTo(RokidNativeGestureInput input)
        {
            if (input == null) return;
            input.moveThreshold = moveThreshold;
            input.detectWindow = detectWindow;
            input.cooldown = cooldown;
            input.expandContractThreshold = expandContractThreshold;
            input.pinchThreshold = pinchThreshold;
            input.axisDominance = axisDominance;
            input.minConfidence = minConfidence;
            Debug.Log($"[GesturePreset] Applied '{name}' to {input.gameObject.name}");
        }
    }
}
