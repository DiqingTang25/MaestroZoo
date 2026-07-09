# MaestroZoo Progress

Last updated: 2026-07-09 (Claude sync, commit 6970cf1)

## Active Split

- Claude Code: Input/Core, chart validation, animal model import, gesture device readiness.
- Codex: Scene wiring, UI, audio, Build Settings, demo readiness.

## Recent Changes

| Who | What |
|-----|------|
| Claude | ✅ AnimalPerformer 双模型支持: idle + score 切换 |
| Claude | ✅ MaestroSceneBuilder: 加载 idle+score 模型, 修复映射文档 |
| Claude | ✅ RokidNativeGestureInput: DeviceReadiness 诊断 + 手势覆盖追踪 |
| Claude | ✅ ChartValidator: 重构 + Validate All Charts 菜单 |
| Claude | ✅ 之前: 动物模型导入, GestureThresholdPreset, GestureHistory |
| Codex | ✅ 移除 KeyboardGameplay，场景/代码清理 |
| Codex | ✅ Main.unity → Build Settings |
| Codex | ✅ RokidDebugPanel + GestureFeedbackDisplay 接线 |
| Codex | ✅ ChartPlayer 占位节拍器 |

## Handoff Notes

- Main input: `RokidNativeGestureInput` → `GesEventInput.OnProcessGesData`
- Fallback: `RokidHandGestureInput` → `XRHandSubsystem`
- No keyboard gameplay for competition.
- `Main.unity` — Codex 独占，修改前协调。
- GitHub: `git push -u origin master:main`（终端手动执行）

## ⚠️ 模型缺口 (Claude 负责跟踪)
- **小鸟得分 .blend → FBX**: Blender 未安装在本机，无法命令行转换。需在安装了 Blender 的机器上用 Unity 打开项目，Unity 会自动导入。或者手动用 Blender 导出 FBX。
- **小兔 (兔子鼓手) 模型缺失**: 当前用 小猫待机/得分 模型占位 RabbitDrum。需要获取兔子模型。
- **小鸟待机**: 文件名为 `小鸟待机 - 副本.fbx`，建议在 Unity 中重命名为 `小鸟待机.fbx`。
- **ElephantHorn 乐器模型**: 缺失，当前用 Grand_Piano.glb 占位。

## Animator Controller 说明
- 动物模型是**静态姿态**（idle pose + score pose），不是带骨骼动画的 rigged model。
- 得分动画效果通过 AnimalPerformer 的**模型切换**实现：命中时隐藏 idleModel，显示 scoreModel，0.55 秒后自动切换回 idle。
- **不需要** Animator Controller。如果未来有骨骼动画模型，AnimalPerformer 已预留 `idleModel`/`scoreModel` GameObject 引用字段，可直接拖入。

## Codex 下一步
1. 动物模型挂接到 SceneBuilder 场景 → 运行 `Maestro Zoo/Build Production Scene` 即可
2. 小鸟 .blend → 在安装了 Blender 的机器上打开 Unity 项目
3. 结算界面 (Results mode)
4. 真实音频文件 → Assets/_Project/Resources/Audio/
5. 打包检查 → Android/Rokid 平台设置
