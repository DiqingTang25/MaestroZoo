using UnityEngine;

namespace MaestroZoo
{
    /// <summary>
    /// 设备延迟预设 — 常见 XR 设备的音频延迟参考值。
    /// Device-specific audio latency presets for quick calibration.
    /// </summary>
    [CreateAssetMenu(menuName = "MaestroZoo/Latency Preset", fileName = "Latency_")]
    public class LatencyPreset : ScriptableObject
    {
        [Tooltip("Human-readable name (e.g. 'Rokid Wired', 'Quest 3 Bluetooth').")]
        public string deviceName = "Unknown";

        [Tooltip("Typical audio output latency in seconds.")]
        [Range(0f, 0.3f)]
        public float latencySeconds = 0.05f;

        [Tooltip("Brief description of this preset.")]
        [TextArea(2, 4)]
        public string description;

        public void ApplyTo(ChartPlayer chartPlayer)
        {
            if (chartPlayer != null)
                chartPlayer.SetLatencyOffset(latencySeconds);
        }

        // --- Factory ---
        public static LatencyPreset Create(string name, float latency, string desc)
        {
            var p = CreateInstance<LatencyPreset>();
            p.deviceName = name;
            p.latencySeconds = latency;
            p.description = desc;
            return p;
        }
    }
}
