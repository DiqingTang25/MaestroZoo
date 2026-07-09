# MaestroZoo Progress

Last updated: 2026-07-09 (Claude sync, commit 18371b0)

## Active Split

- Claude Code: Input/Core, chart system, gesture calibration, latency, animal models.
- Codex: Scene wiring, UI, audio, Build Settings, chart authoring, VFX.

## Recent Changes

| Who | What | Commit |
|-----|------|--------|
| Claude | ✅ BPM变速 + 暂停/继续 + 长按手势 | 18371b0 |
| Claude | ✅ 小鸟得分 .blend → FBX (Blender 5.1) | 8773356 |
| Claude | ✅ 校准管线端到端 CalibrationCoordinator | b84be29 |
| Claude | ✅ 个性化手势校准 PersonalGestureCalibrator | 1f4fdda |
| Claude | ✅ 音频延迟补偿 CompensatedSongTime | 2ac1e2a |
| Claude | ✅ 动物模型双模 idle/score 接入 | 6970cf1 |
| Claude | ✅ 猫头鹰老师教程系统 OwlTutorialController | b468ab1 |
| Claude | ✅ 难度分级 Easy/Normal/Hard + 延迟预设库 | d4e233a |
| Claude | ✅ FreeStage 增强 (节拍器+手势提示+即兴评分) | 0090fa9 |
| Codex | ✅ figaro 谱面 + ChartDebugPanel | 5428aa9 |

## 新增 API 速查 (给 Codex)

### 猫头鹰老师教程
```csharp
// 公共 API:
owlTutorial.StartTutorial();    // 开始完整教程
owlTutorial.SkipCurrentStep();  // 跳过当前步
owlTutorial.RetryStep();        // 重试当前步
owlTutorial.CancelTutorial();   // 取消

// 事件 (给 UI 订阅):
owlTutorial.StepChanged       // 步骤切换 → 更新标题
owlTutorial.InstructionChanged // 指令文本 → GameHud.ShowTutorialInstruction()
owlTutorial.FeedbackChanged   // 即时反馈 → GameHud.ShowTutorialFeedback()
owlTutorial.TutorialCompleted // 教程完成

// GameHud 新增:
gameHud.ShowTutorialInstruction("text");
gameHud.ShowTutorialFeedback("Perfect!");
gameHud.HideTutorial();
```

### 教程步骤流程
```
Idle → Intro(2.5s) → Down(2notes) → Up(2notes) → Left(2notes) 
     → Right(2notes) → Expand(1note) → Close(1note) → Complete
```
每步自动生成迷你谱面(仅含该手势的2个音符, BPM=96, leadTime=2s)。

### BPM 变速
```json
// Chart JSON 新增可选字段:
{
  "bpm": 144,
  "tempoChanges": [
    { "time": 0, "bpm": 120 },
    { "time": 30.5, "bpm": 144 },
    { "time": 75.0, "bpm": 108 }
  ]
}
```
- `ChartPlayer.CurrentBpm` → 实时 BPM（自动插值）
- `ChartValidator.ValidateTempoChanges()` 已做校验

### 暂停/继续
```csharp
chartPlayer.PauseSong();    // 暂停（冻结 SongTime，记录累计偏移）
chartPlayer.ResumeSong();   // 继续
// 状态: chartPlayer.IsPaused
// 事件: PlaybackPaused, PlaybackResumed
// JudgeManager/NoteSpawner 已感知暂停状态
```

### 长按手势 (sostenuto / hold)
```json
// ChartNote 新增可选字段:
{ "time": 12.0, "gesture": "Expand", "duration": 2.5, "lane": 0, "animal": "FullOrchestra" }
```
- `duration > 0` → 长按模式，不需要重复手势
- JudgeManager 自动跟踪 hold → duration 满后 Perfect
- `ChartNote.IsSustained` → 判断是否为长按音符
- 事件: `SustainedHoldStarted` / `SustainedHoldReleased`

### 当前 BPM 显示
- `ChartPlayer.CurrentBpm` 可在 HUD 显示
- 用于节拍器/占位音频的 BPM 自适应

## ⚠️ 剩余缺口

| 类别 | 缺口 | 负责 |
|------|------|------|
| 模型 | 兔子 (小兔鼓手) 缺失 | 用户建模 |
| 模型 | ElephantHorn 乐器缺失 | 用户建模 |
| 模型 | 小鸟待机重命名 (去掉"副本") | Codex |
| VFX | Note 命中粒子 / Fever 光效 / 动物反馈 | 用户 |
| 谱面 | figaro 变速段 + 长按段标注 | Codex |
| UI | GameHud 教程文本创建 (tutorialInstructionText/FeedbackText) | Codex |
| 真机 | Rokid 设备验证 | 待设备 |
