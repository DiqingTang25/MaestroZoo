# MaestroZoo Progress

Last updated: 2026-07-09 (Claude sync, commit 1f4fdda)

## Active Split

- Claude Code: Input/Core, chart validation, animal model import, gesture calibration, latency compensation.
- Codex: Scene wiring, UI, audio, Build Settings, SFX.

## Recent Changes

| Who | What | Commit |
|-----|------|--------|
| Claude | ✅ 个性化手势校准 PersonalGestureCalibrator (4步状态机) | 1f4fdda |
| Claude | ✅ 手势设计参考文档 GESTURE_DESIGN_REFERENCE.md | 1f4fdda |
| Claude | ✅ 音频延迟补偿 CompensatedSongTime + 自动校准 | 2ac1e2a |
| Claude | ✅ AnimalPerformer 双模型 idle/score 切换 | 6970cf1 |
| Claude | ✅ RokidNativeGestureInput DeviceReadiness 诊断 | 6970cf1 |
| Claude | ✅ ChartValidator 重构 + Validate All Charts | 6970cf1 |
| Codex | ✅ Android 打包设置 | fb89285 |
| Codex | ✅ Demo BGM 音频资源导入 | 59c991e |
| Codex | ✅ 结算界面 Results Panel + 程序化 SFX | 18fcf4b |

## Handoff Notes

- Main input: `RokidNativeGestureInput` → `GesEventInput.OnProcessGesData`
- Fallback: `RokidHandGestureInput` → `XRHandSubsystem`
- No keyboard gameplay for competition.
- `Main.unity` — Codex 独占，修改前协调。
- GitHub: `git push -u origin master:main`（终端手动执行）

### 延迟补偿使用方式
- `ChartPlayer.latencyOffset` — 可在 Inspector 手动设置，或运行时调用 `SetLatencyOffset(0.05f)`
- 自动校准: `ChartPlayer.StartLatencyCalibration()` → 用户拍手 → `RegisterCalibrationTap()` → 自动计算
- 所有消费者 (JudgeManager/NoteSpawner/FlyingNote/HudConnector) 已统一使用 `CompensatedSongTime`

### 个性化手势校准使用方式
- `PersonalGestureCalibrator.Begin()` → 引导用户举手→4方向挥手→自动计算并应用 `moveThreshold`
- `CalibrationResult` 可 JSON 序列化跨会话保存
- 参考 `GestureThresholdPreset` 可以保存校准结果为预设

## ⚠️ 模型缺口 (Claude 负责跟踪)
- **小鸟得分 .blend → FBX**: Blender 未安装在本机。需在安装 Blender 的机器上 Unity 打开项目自动导入。
- **小兔 (兔子鼓手) 模型缺失**: 当前用小猫模型占位 RabbitDrum。
- **小鸟待机**: 文件名为 `小鸟待机 - 副本.fbx`，建议 Unity 中重命名。
- **ElephantHorn 乐器模型**: 缺失，当前用 Grand_Piano.glb 占位。

## Animator Controller 说明
- 动物模型是静态姿态，得分效果通过 AnimalPerformer 模型切换实现（idle ↔ score，0.55s）
- **不需要** Animator Controller

## Codex 下一步
1. ~~音频资源~~ ✅ Done (59c991e)
2. ~~结算界面~~ ✅ Done (18fcf4b)
3. ~~打包设置~~ ✅ Done (fb89285)
4. 调试面板位置/大小调整 + 开关
5. 谱面手势标注 (参考 GESTURE_DESIGN_REFERENCE.md)
6. 小鸟 .blend → 安装 Blender 的机器上 FBX 转换
