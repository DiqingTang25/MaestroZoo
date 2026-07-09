using UnityEngine;

namespace MaestroZoo
{
    /// <summary>
    /// [DEPRECATED] 此 Stub 已被 RokidNativeGestureInput 替代。
    /// 请使用 GestureInputDispatcher 自动选择输入源。
    /// 保留此类仅用于向后兼容旧的 scene 引用。
    /// </summary>
    [System.Obsolete("Use GestureInputDispatcher with RokidNativeGestureInput instead.")]
    public class RokidGestureInputStub : MonoBehaviour, IGestureInput
    {
        public bool TryConsumeGesture(out GestureType gesture, out float inputTime)
        {
            gesture = default;
            inputTime = default;
            return false;
        }
    }
}
