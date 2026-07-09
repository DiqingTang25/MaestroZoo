# MaestroZoo — 缺失内容清单

> 最后更新: 2026-07-09 | 更新者: Claude Code

---

## 🔴 关键缺失 (阻塞功能)

### 1. 音频文件 — `Assets/_Project/Resources/Audio/`
**状态:** 目录为空，只有 `.meta`

游戏需要以下音频资源:
| 文件名 | 用途 | 谱面 |
|--------|------|------|
| `tutorial_bgm.wav/mp3` | 教程 BGM | tutorial_basic.json |
| `forest_challenge_bgm.wav/mp3` | 森林挑战 BGM | forest_challenge_full.json |
| `party_quick_bgm.wav/mp3` | 派对快速 BGM | party_quick.json |
| `forest_opening_bgm.wav/mp3` | 森林开场 BGM | forest_opening_sample.json |

**建议:** 导入临时音频文件用于测试，或使用 Unity 的 AudioClip.Create 生成测试音。

### 2. EditorBuildSettings — 场景未注册
**文件:** `ProjectSettings/EditorBuildSettings.asset`
**状态:** `m_Scenes: []` — 空列表

**修复:** 将 `Assets/_Project/Scenes/Main.unity` 添加到 Build Settings。

### 3. ElephantHorn 模型缺失
`MaestroSceneBuilder.cs` 引用了 `"ElephantHorn"` 动物，但:
- `Assets/Models/Instruments/` 中没有对应的乐器模型
- 代码中 `inst[4] = null` (第120行) — 明确跳过了模型加载

### 4. 手势识别元文件缺失
新增的 `RokidNativeGestureInput.cs` 和 `GestureFeedbackDisplay.cs` 缺少 `.meta` 文件:
- `Assets/_Project/Scripts/Input/RokidNativeGestureInput.cs.meta`
- `Assets/_Project/Scripts/UI/GestureFeedbackDisplay.cs.meta`

**注意:** Unity 会在打开项目时自动生成 .meta 文件。如果项目未在当前 Unity 中打开，需要在 Unity 中打开一次以生成。

---

## 🟡 重要缺失 (影响体验)

### 5. Prefabs 目录为空
**路径:** `Assets/_Project/Prefabs/`
**状态:** 目录为空，只有 `.meta`

建议创建的 Prefabs:
- `FlyingNote.prefab` — 飞行音符预制体 (目前由 MaestroSceneBuilder 程序化创建)
- `Animal_RabbitDrum.prefab` ~ `Animal_ElephantHorn.prefab` — 动物角色预制体
- `GestureFeedbackUI.prefab` — 手势反馈 UI

### 6. 没有音符/SFX 音效
游戏缺少:
- Perfect/Good/Miss 判定音效 (打击音效)
- 手势识别确认音效
- Combo 变化音效
- UI 按钮音效

### 7. Rokid 真机测试验证
以下组件未在 Rokid 真机上验证:
- `RokidNativeGestureInput` — 依赖 `GesEventInput.OnProcessGesData`
- `RokidHandGestureInput` — 依赖 `XRHandSubsystem`
- 手势阈值调优 (`moveThreshold`, `detectWindow`, `cooldown`)

---

## 🟢 改进项 (非阻塞)

### 8. 没有单元测试
建议:
- JudgeManager 判定逻辑测试
- ChartData JSON 反序列化测试
- NoteSpawner 生成逻辑测试
- HandTracker 手势检测算法测试

### 9. 没有 README
项目缺少根目录 README.md，应包含:
- 项目简介 (MaestroZoo 是什么)
- 运行要求 (Unity 2022.3+, Node.js 18+, Rokid AR 眼镜)
- 开发环境搭建步骤
- 如何添加新谱面
- 如何添加新动物角色

### 10. 缺少手势校准系统
Rokid SDK 提供了 `BeginGestureCalibrate()`, `StopGestureCalibrate()` — 游戏中未使用。
建议在设置/开场添加手势校准步骤。

### 11. 缺少音乐同步系统
目前 `ChartPlayer` 使用 `AudioSettings.dspTime` 计时 — 这是正确的，但:
- 没有延迟补偿 (latency compensation)
- 没有 music offset 配置
- 没有节拍器/click track

### 12. Models/Notes 和 Models/Props 目录为空
**路径:** `Assets/Models/Notes/`, `Assets/Models/Props/`
**状态:** 目录为空

原本可能计划放音符模型和道具模型，尚未添加。

### 13. Animals 使用 Primitive 形状
`MaestroSceneBuilder.BuildAnimal()` 使用 Capsule + Sphere 等 Primitive 创建动物 — 需要真实的卡通动物模型替换。

### 14. `AnimalFeedbackController` 引用 Animator 但场景中没有
代码引用了 `rabbitDrum`, `foxViolin`, `bearCello`, `birdFlute` 的 Animator，但 `MaestroSceneBuilder` 创建动物时没有添加 Animator。

---

## 📊 完成度估算

| 子系统 | 完成度 | 备注 |
|--------|--------|------|
| 核心架构 | 85% | Director/ChartPlayer/NoteSpawner/JudgeManager 完成 |
| 手势识别 | 80% | 代码完成，真机未验证，阈值未调优 |
| 输入分发 | 90% | GestureInputDispatcher 三级优先级完成 |
| 判定系统 | 85% | Perfect/Good/Miss 逻辑完成，无边缘情况测试 |
| 动物反馈 | 60% | Primitive 几何体，无 Animator，无真实模型 |
| UI/HUD | 75% | 基本 HUD 完成，无菜单系统，无设置界面 |
| 谱面数据 | 70% | 4 首谱面，无音频文件 |
| 场景 | 70% | 程序化构建完成，模型引用不完整 |
| 音频 | 5% | 只有 AudioSource 组件，无音频文件 |
| 资源/模型 | 40% | 乐器模型有 6 个，缺动物模型和场景道具 |
| 测试 | 0% | 无任何测试 |
| 真机验证 | 0% | 无 Rokid 真机测试数据 |
