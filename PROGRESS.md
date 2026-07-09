# MaestroZoo 比赛版 — 协作进度

> 2026-07-09 · Claude + Codex 同步开发

## Commit 前缀约定
- `input:` — Claude Code（手势识别、输入系统、真机调试）
- `ui:` — Codex（UI、HUD、反馈显示）
- `scene:` — Codex（场景、SceneBuilder、接线）
- `build:` — Codex（Build Settings、打包、资源）
- `chore:` — 任一方（git、进度、文档、配置）

## 当前状态

| 时间 | 谁 | 做了什么 | 是否阻塞 | 需要谁接手 |
|------|-----|---------|---------|-----------|
| 2026-07-09 | Claude | ✅ API验证通过 + 调试属性 + RokidDebugPanel + 校准 | - | Codex 接线 DebugPanel |
| 2026-07-09 | Claude | ✅ 创建 PROGRESS.md | - | - |
| 2026-07-09 | Codex | ✅ 移除 KeyboardGestureInput 并清理 Dispatcher/Director | - | - |
| 2026-07-09 | Codex | ✅ 重构 RokidNativeGestureInput | - | - |

## 文件所有权
### Claude 的文件（Input/Core 手势相关）
- `Assets/_Project/Scripts/Input/RokidNativeGestureInput.cs` ✅ 已完成
- `Assets/_Project/Scripts/Input/RokidHandGestureInput.cs`
- `Assets/_Project/Scripts/Input/GestureInputDispatcher.cs` ⚠️ Codex 移除了键盘
- `Assets/_Project/Scripts/Input/IGestureInput.cs`
- `Assets/_Project/Scripts/Input/RokidGestureInputStub.cs`
- `Assets/_Project/Scripts/Input/RokidDebugPanel.cs` ✅ 新增，需 Codex 挂到场景
- `Assets/_Project/Scripts/Core/JudgeManager.cs`
- `Assets/_Project/Scripts/Core/GestureType.cs`

### Codex 的文件（UI/场景/构建）
- `Assets/_Project/Scenes/Main.unity` ⚠️ 互斥
- `Assets/_Project/Scripts/Editor/MaestroSceneBuilder.cs`
- `Assets/_Project/Scripts/UI/GameHud.cs`
- `Assets/_Project/Scripts/UI/HudConnector.cs`
- `Assets/_Project/Scripts/UI/GestureFeedbackDisplay.cs`
- `Assets/_Project/Scripts/Core/MaestroGameDirector.cs`
- `ProjectSettings/EditorBuildSettings.asset`

### 共享（需要协调）
- `Assets/_Project/Scripts/Core/MaestroGameDirector.cs` — Controller 层
- `ProjectSettings/EditorBuildSettings.asset` — ⚠️ 二进制，一次一人

## 阻塞项
- [ ] 无音频文件 → Codex 负责占位资源
- [ ] 无 ElephantHorn 模型
- [x] ~~真机未验证手势链路~~ → Claude API 验证通过
- [ ] **Codex 需在 Main.unity/MaestroSceneBuilder 中挂接 RokidDebugPanel**

## 下一步
- Claude: 已完成 Input 侧工作，等 Codex 反馈
- Codex: 在 MaestroSceneBuilder 中 AddComponent<RokidDebugPanel> 并连线
