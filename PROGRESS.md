# MaestroZoo Progress

Last updated: 2026-07-09 (Claude sync)

## Active Split

- Claude Code: Rokid input, gesture calibration, recognition thresholds, core judgement, model import.
- Codex: UI, scene wiring, Build Settings, demo readiness, resource gap checks.

## Recent Changes

| Who | What |
|-----|------|
| Claude | ✅ 导入动物模型: 熊/狐狸/猫/象/鸟 (idle+score FBX) → `Assets/Models/Animals/` |
| Claude | ✅ ChartValidator + Validate All Charts 菜单 |
| Claude | ✅ GestureThresholdPreset (比赛/灵敏/稳定) |
| Claude | ✅ GestureHistory 环形缓冲 + 调试面板显示 |
| Claude | ✅ JudgeManager: 只在 chartPlayer.IsPlaying 时判定 |
| Codex | ✅ 移除 KeyboardGameplay，场景/代码清理 |
| Codex | ✅ Main.unity → Build Settings |
| Codex | ✅ RokidDebugPanel + GestureFeedbackDisplay 接线 |
| Codex | ✅ ChartPlayer 占位节拍器 |
| Codex | ✅ Unity 批量编译通过 |

## Handoff Notes

- Main input: `RokidNativeGestureInput` → `GesEventInput.OnProcessGesData`
- Fallback: `RokidHandGestureInput` → `XRHandSubsystem`
- No keyboard gameplay for competition.
- `Main.unity` — Codex 独占，修改前协调。
- GitHub: `git push -u origin master:main`（终端手动执行，此处网络超时）

## ⚠️ 模型缺口
- 小鸟得分.blend → 需导出 FBX
- 小兔(兔子鼓手) → 缺失
- ElephantHorn 乐器模型 → 缺失

## Codex 下一步
1. 动物模型挂接到 SceneBuilder（替换 Primitive Capsule/Sphere）
2. 小鸟.blend → FBX
3. 结算界面 (Results mode)
4. 真实音频文件替换占位节拍器
