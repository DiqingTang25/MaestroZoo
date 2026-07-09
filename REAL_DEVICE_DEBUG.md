# MaestroZoo 真机调试小白指南

> 零基础也能看懂。每一步都写清楚了。

---

## 准备

你需要：
- Rokid AR 眼镜
- Android 手机（连眼镜用）
- 一根 USB 数据线（手机连电脑）
- 电脑（Windows，你现在这台）

---

## 第一步：手机开启开发者模式

1. 手机打开 **设置**
2. 找到 **关于手机** → 连续点击 **版本号** 7 次
3. 提示"已进入开发者模式"
4. 返回设置 → **开发者选项** → 打开 **USB 调试**

> 不同手机品牌位置略有不同，百度搜"XX手机 开发者模式"就有。

---

## 第二步：装 ADB 工具

**如果电脑已经能跑 `adb` 命令**（在终端输入 `adb` 有输出），跳过这一步。

**如果没有的话安装：**
1. 去 https://developer.android.com/tools/releases/platform-tools 下载
2. 解压到 `D:\platform-tools`
3. 把 `D:\platform-tools` 加到系统 PATH

---

## 第三步：确认手机连上

打开终端，输入：

```bash
adb devices
```

应该看到：

```
List of devices attached
ABC123456789    device
```

如果显示 `unauthorized`：手机屏幕上会弹出"允许 USB 调试"，点确定。

如果什么都不显示：
- 换一根数据线（很多线只能充电不能传数据）
- 换一个 USB 口
- 检查手机上 USB 调试开关

---

## 第四步：装 APK 到手机

APK 在 `D:\UnityProjects\Zoo\Builds\Android\MaestroZoo-Rokid-Demo.apk`

```bash
adb install -r D:\UnityProjects\Zoo\Builds\Android\MaestroZoo-Rokid-Demo.apk
```

看到 `Success` 就是装好了。

---

## 第五步：运行 App + 看日志

### 看实时日志

```bash
adb logcat -s Unity
```

这会打印 App 的所有 Unity 日志。**保持这个终端开着**，你能看到游戏运行时的所有输出。

### 只看关键信息

```bash
adb logcat -s Unity | grep -E "RokidNative|Judge|ChartPlayer|OwlTeacher|FreeStage|CalibCoord|Gesture"
```

### 启动 App

手机连上 Rokid 眼镜 → 在手机上打开 MaestroZoo App → 戴上眼镜。

---

## 第六步：看懂调试面板

戴上 Rokid 眼镜后，屏幕**左上角**会出现一个半透明黑底的调试面板（不需要额外 UI，代码里用 OnGUI 画的）：

```
=== ROKID DEBUG PANEL ===
Active Source:      RokidNative          ← 手势来源
GesEventInput:      INITIALIZED          ← 最关键！必须是 INITIALIZED
Tracking Available: YES                  ← 手被追踪到了吗
Left Hand:          TRACKED              ← 左手状态
Right Hand:         TRACKED              ← 右手状态
Device Readiness:   Ready                ← 整体就绪状态
Gesture Coverage:   ALL 6/6              ← 6种手势都要触发过
Audio Latency:      50ms ✓               ← 当前延迟补偿值
Calib Mode:         IDLE                 ← 校准状态
```

### 每个字段的意思

| 显示 | 正常值 | 不正常 | 怎么办 |
|------|--------|--------|--------|
| Active Source | RokidNative | ? / XRHand | Rokid SDK 没初始化 |
| GesEventInput | INITIALIZED | NOT FOUND | 检查眼镜连接，重启 App |
| Left Hand | TRACKED | LOST | 左手放进摄像头范围 |
| Right Hand | TRACKED | LOST | 右手放进摄像头范围 |
| Device Readiness | Ready | Error_xxx | 看下一行的 Message |
| Gesture Coverage | 6/6 | < 6 | 把所有手势都做一遍 |

---

## 第七步：验证 6 种手势

对着眼镜做这 6 个动作，看面板上 `Last Gesture` 和 `Gesture Coverage` 有没有变化：

| 动作 | 怎么做 | 触发什么 |
|------|--------|----------|
| **Down** | 手从上往下快速挥 | `Down @ xx.xxs` |
| **Up** | 手从下往上快速挥 | `Up @ xx.xxs` |
| **Left** | 手从右往左挥 | `Left @ xx.xxs` |
| **Right** | 手从左往右挥 | `Right @ xx.xxs` |
| **Expand** | 双手张开（或捏合手指张开） | `Expand @ xx.xxs` |
| **Close** | 双手收拢（或捏合手指收拢） | `Close @ xx.xxs` |

### 如果某个手势不触发

在手势历史里看，`History` 行显示最近 5 个被识别的手势：
```
History: Up @ 12.34s (conf:0.85)
```

`conf` 是置信度，低于 0.8 会被丢弃。如果某个手势 `conf` 一直很低，说明 `axisDominance` 或 `moveThreshold` 需要调。

---

## 第八步：调手势阈值

手势不灵敏/太灵敏时，修改 `RokidNativeGestureInput` 的参数：

| 参数 | 太小会怎样 | 太大会怎样 | 建议范围 |
|------|-----------|-----------|----------|
| `moveThreshold` | 手不动都触发（误触发多） | 挥很大力才触发 | 0.08 - 0.18 |
| `cooldown` | 一个动作触发多次 | 快速连续动作被吞掉 | 0.18 - 0.35 |
| `axisDominance` | 上下左右分不清 | 必须很直才能识别 | 1.10 - 1.50 |
| `minConfidence` | 抖动也会通过 | 正常动作被丢弃 | 0.6 - 0.9 |

### 快速切换预设

代码里自带 3 个预设，在 Unity Inspector 里拖入即可：

- **Sensitive（灵敏）**：手势轻的人用
  - moveThreshold=0.08, cooldown=0.18, axisDominance=1.10
- **Competition（比赛）**：默认
  - moveThreshold=0.12, cooldown=0.25, axisDominance=1.25
- **Stable（稳定）**：手势重/容易误触发的人用
  - moveThreshold=0.18, cooldown=0.35, axisDominance=1.50

---

## 第九步：校准音频延迟

音频不同步（音符飞来但音乐没跟上）：

1. 在调试面板确认 `Audio Latency` 值
2. 如果显示 `0ms (default)`，说明还没校准
3. 跑校准：`calibrationCoordinator.StartAudioLatencyCalibration()`
4. 会播放 8 拍节拍器，在**重拍**时做 Down 手势
5. 自动计算延迟，保存到 PlayerPrefs

### 手动设置

如果知道设备的典型延迟，直接设：

```csharp
chartPlayer.SetLatencyOffset(0.05f);  // 50ms
```

典型值：
- 有线耳机：30ms
- 眼镜喇叭：50ms
- 蓝牙 aptX：120ms
- 普通蓝牙：180ms

---

## 第十步：常用 ADB 命令速查

```bash
# 安装 APK
adb install -r 路径/xxx.apk

# 卸载
adb uninstall com.diqingtang.maestrozoo

# 看所有应用日志
adb logcat -s Unity

# 只看错误
adb logcat -s Unity | grep -i error

# 清空日志缓存
adb logcat -c

# 重启 App
adb shell am force-stop com.diqingtang.maestrozoo
adb shell am start -n com.diqingtang.maestrozoo/com.unity3d.player.UnityPlayerActivity

# 截手机屏幕
adb exec-out screencap -p > screen.png

# 录屏（Ctrl+C 停止）
adb shell screenrecord /sdcard/demo.mp4
adb pull /sdcard/demo.mp4 .
```

---

## 常见问题排查

### 1. App 启动闪退

```bash
adb logcat -s Unity | grep -i "crash\|exception\|error"
```

把输出发给我。

### 2. GesEventInput: NOT FOUND

Rokid SDK 没初始化。可能原因：
- 手机没装 Rokid 配套 App
- 眼镜没连上
- 手机系统版本太低（需要 Android 10+）

### 3. 手势识别非常不准

先确认 `Device Readiness: Ready` + `Left/Right Hand: TRACKED`。

然后看 `Gesture History`，里面每条有一个 `conf` 值：
- conf > 0.9：很好
- conf 0.7-0.9：还行
- conf < 0.7：手势太模糊

conf 一直低 → 切换到 `Sensitive` 预设。

### 4. 音频延迟导致体验差

- 优先用**有线耳机**（延迟最低 ~30ms）
- 其次用眼镜喇叭（~50ms）
- 避免蓝牙（延迟 120-200ms，除非支持 aptX Low Latency）

### 5. 手机发热/卡顿

- 降低画质：Project Settings → Quality → 选 Low
- 关阴影：Directional Light → Shadow Type → No Shadows
- Strip Engine Code：Build Settings → Player Settings → Strip Engine Code = ON

---

## 一句话总结

```
装 ADB → 连手机 → adb install APK → 戴眼镜 → 看左上角面板 → 做 6 个手势 → 看 conf 值 → 调阈值
```

有问题把 `adb logcat -s Unity` 的输出发给我。
