# MaestroZoo 真机部署指南

> 目标设备: Rokid AR 眼镜 + Android 手机

---

## 一、Unity Editor 操作（一次性）

### 1. 打开项目
```
Unity Hub → 打开项目 → D:\UnityProjects\Zoo
Unity 版本: 2022.3.62f3c1
```

### 2. 运行菜单（按顺序）
```
菜单栏 → Maestro Zoo:

① Create Difficulty Profiles    → 生成 Easy/Normal/Hard 难度.asset
② Create Latency Presets        → 生成 7 个设备延迟.asset
③ Build Production Scene        → 构建完整 Main.unity 场景
```

### 3. 切换平台
```
File → Build Settings → Android → Switch Platform
```

### 4. 构建 APK
```
菜单栏 → Maestro Zoo → Build Android APK
输出: Builds/Android/MaestroZoo-Rokid-Demo.apk
```

> 如果报签名错误：Project Settings → Player → Android → Publishing Settings → 勾选 "Custom Keystore" → 创建新的 debug keystore

---

## 二、安装到手机

```bash
# 1. 手机 USB 连接，开启开发者模式+USB调试
adb devices                          # 确认设备已连接

# 2. 安装 APK
adb install Builds/Android/MaestroZoo-Rokid-Demo.apk

# 3. 手机连接 Rokid 眼镜，启动 App
```

---

## 三、真机第一次启动流程

### Step 1: 看调试面板
启动后屏幕左上角会出现 **ROKID DEBUG PANEL**（OnGUI 实现的，不需要 Canvas）：

```
=== ROKID DEBUG PANEL ===
Active Source:      RokidNative / XRHand / ?
GesEventInput:      INITIALIZED / NOT FOUND  ← 最关键
Device Readiness:   Ready / Error_xxx
  Message:          ...
Gesture Coverage:   ALL 6/6 或 Detected [2/6]: Down, Up, ...
Last Gesture:       ...
```

**如果显示 `GesEventInput: NOT FOUND`**：
- Rokid SDK 未正常初始化
- 检查眼镜连接，重启 App
- 确认手机已安装 Rokid 配套 App

**如果显示 `Device Readiness: Error_NoHandTracking`**：
- 手进入摄像头视野
- 光线不要太暗

### Step 2: 验证手势
对着 Rokid 眼镜做 6 种手势，看面板上 `Gesture Coverage` 是否从 [0/6] 逐渐变成 [6/6]：
- **Down**: 手向下挥
- **Up**: 手向上挥
- **Left**: 手向左挥
- **Right**: 手向右挥
- **Expand**: 双手张开 / 捏合距离变大
- **Close**: 双手收拢 / 捏合距离变小

### Step 3: 手势阈值调优
如果手势识别不准（太灵敏/太迟钝），两种方式调整：

**方式A：预设切换（快速）**
在 `RokidNativeGestureInput` 组件上拖入 `GestureThresholdPreset`：
- `GesturePreset_Competition` — 比赛默认
- `GesturePreset_Sensitive` — 灵敏（手势轻的人）
- `GesturePreset_Stable` — 稳定（手势重的人）

**方式B：个性化校准（精准）**
在游戏启动画面点 "手势校准" 按钮（需要 Codex 做 UI），或代码调用：
```csharp
calibrationCoordinator.StartGestureCalibration();
// 跟随提示: 举手 → 上 → 下 → 左 → 右
// 自动计算个性化 moveThreshold
```

### Step 4: 音频延迟校准
```csharp
calibrationCoordinator.StartAudioLatencyCalibration();
// 听节拍器，在重拍（强音）时做 Down 手势
// 8拍后自动计算延迟偏移
// 结果保存到 PlayerPrefs
```

Rokid 典型延迟参考：
| 输出方式 | 延迟 | 预设 |
|----------|------|------|
| 有线耳机 | ~30ms | Rokid Wired |
| 眼镜自带喇叭 | ~50ms | Rokid Speaker |
| 蓝牙耳机(aptX) | ~120ms | Bluetooth HQ |
| 蓝牙耳机(普通) | ~180ms | Bluetooth Std |

---

## 四、快速功能验证清单

| 模式 | 操作 | 预期 |
|------|------|------|
| 自动开始 | 启动App | 自动播放费加罗的婚礼 (BPM=144) |
| Challenge | 正常玩 | 音符飞来 → 做手势 → Perfect/Good/Miss → 得分 |
| Challenge结束 | 播完 | 结算界面显示分数、连击、精度 |
| FreeStage | 手动切模式 | 节拍器响 → 任意手势 → 动物反应 + 即兴得分 |
| Tutorial | 手动启动教程 | 猫头鹰文字引导 → 6步手势教学 |
| 暂停 | 暂停 | SongTime冻结 → 继续时恢复 |
| Fever | 连续Perfect | Mood→100 触发Fever → 灯光变亮 |
| 校准 | 跑校准流程 | 自动保存到 PlayerPrefs |

---

## 五、调试手段

### OnGUI 调试面板（设备上直接看）
`RokidDebugPanel` — 左上角实时显示：
- 手势源状态
- 手部追踪 (左/右 位置)
- 最近 5 个手势历史
- Device Readiness
- 校准状态
- 音频延迟

### ADB Logcat（电脑上看）
```bash
adb logcat -s Unity | grep -E "\[RokidNative\]|\[Judge\]|\[OwlTeacher\]|\[ChartPlayer\]|\[FreeStage\]|\[CalibCoord\]"
```

### 修改参数实时看效果
在 Unity Editor 的 Inspector 中改值 → 重新 Build → 安装测试

---

## 六、常见问题

| 问题 | 可能原因 | 解决 |
|------|----------|------|
| GesEventInput NOT FOUND | Rokid SDK未初始化 | 检查眼镜连接、重启App |
| 手势识别全是Miss | moveThreshold太高 | 切换到Sensitive预设 |
| 手势识别乱触发 | moveThreshold太低/cooldown太短 | 切换到Stable预设 |
| 音频不同步 | 延迟未校准 | 跑音频延迟校准 |
| FPS 低 / 卡顿 | 场景太复杂 | 降低光照质量/阴影 |
| APK 太大 | 未裁剪 | Player Settings → Strip Engine Code |

---

## 七、需要的 Codex UI 接线

Codex 还需要在场景里做这些 UI 才能完整跑通：
- [ ] 教程界面：创建 `tutorialInstructionText` / `tutorialFeedbackText` Text 对象 → 拖入 GameHud
- [ ] 开始/暂停/继续 按钮 → 调用 `chartPlayer.PauseSong()` / `ResumeSong()`
- [ ] 难度选择 → 切换 `judgeManager.difficultyProfile`
- [ ] 校准按钮 → 调用 `calibrationCoordinator.StartAudioLatencyCalibration()` / `StartGestureCalibration()`
