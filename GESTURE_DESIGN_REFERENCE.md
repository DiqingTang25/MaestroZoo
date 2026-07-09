# VR Maestro 手势设计参考 — 给 Codex

> Claude 总结，用于谱面手势标注 + 手势识别调优

## VR Maestro (Meta Quest) 核心手势体系

VR Maestro 由 Double Jack 开发，是目前最成功的 VR 指挥游戏。玩家站在指挥台上，用手势控制管弦乐队。

### 手势 → 音乐动作映射

| VR Maestro 手势 | 音乐含义 | MaestroZoo 对应 |
|---|---|---|
| 右手指挥棒挥拍 | 保持节拍 (tempo) | **Up/Down** — 上下拍子 |
| 指挥棒敲击空气 | 强音重音 (accent/sforzando) | **Down** — 强调重拍 |
| 指向声部方向 | 提示声部进入 (cue section) | **Left/Right** — 左右指向 |
| 手掌上下抬压 | 渐强/渐弱 (crescendo/decrescendo) | **Expand/Close** — 张握控制音量 |
| 手掌向上托举 | 持续音/sostenuto | **Expand** (长按) |
| 握拳收拢 | 收束/终止 (cut-off) | **Close** — 终止乐句 |

### 关键设计理念

1. **左右手分工**: 右手指挥棒控制节拍，左手控制表情（力度、声部进入）
2. **视觉引导**: 屏幕上有彩色指示器告诉玩家：看哪个方向、做什么手势、何时做
3. **难度分层**: Easy 只需跟上拍子；Normal 加入声部指向；Hard 快速切换+组合手势
4. **反馈层次**: 演奏好 → 玫瑰花+欢呼；演奏差 → 番茄+嘘声；观众反应=评分可视化

### 给 Codex 的建议

#### 谱面手势标注规则（for 费加罗的婚礼）

根据 VR Maestro 的设计模式，建议谱面按以下规则标注：

```
Mozart - 费加罗的婚礼 (4/4拍, Allegro vivace)

强拍 (Downbeat)     → Down    — 指挥棒下击，每小节第1拍
弱拍 (Upbeat)       → Up      — 指挥棒上挑，第2-4拍
小提琴进入           → Left    — 指向左侧 (violin section)
木管/铜管进入        → Right   — 指向右侧 (winds/brass)
渐强 (crescendo)    → Expand  — 张力张开
渐弱/乐句结束        → Close   — 收束

小节示例:
Beat 1 (强):  Down
Beat 2 (弱):  Up  
Beat 3 (小提琴): Left
Beat 4 (全员): Expand  ← 渐强到下一小节
```

#### 手势识别调优建议

1. **Up/Down**: 关键区分度是 Y 轴位移量，建议降低 `axisDominance` (1.1-1.2) 允许轻微水平偏移
2. **Left/Right**: X 轴位移，注意区分是"指向"还是"挥拍"，可在 `detectWindow` 内限制最小位移
3. **Expand/Close**: 有两种实现方式：
   - 单手捏合距离 (pinch) — 更精确但需要手指追踪
   - 双手距离变化 — 更稳定但只能检测双手
4. **组合手势**: VR Maestro 的 Hard 模式有快速手势切换，建议 `cooldown` 设为 0.15-0.2s

#### 手势与动物的建议映射

当前 MaestroZoo 中 OrchestraController 的手势→动物映射：

| 手势 | 动物 | 乐器 | 角色 |
|---|---|---|---|
| Down | RabbitDrum | Drum_Set | 节奏核心 |
| Up | BirdFlute | Flute | 旋律高音 |
| Left | FoxViolin | Violin | 弦乐旋律 |
| Right | BearCello | Cello | 低音支撑 |
| Expand/Close | ElephantHorn | Piano | 和声/力度控制 |

建议优化：将 Expand 单独映射到 FoxViolin（渐强=弦乐进入感），Close 映射到 BirdFlute（终止=高音收束）。

---

## 参考资料

- VR Maestro 游戏视频: Meta Quest Store / YouTube 搜索 "Maestro VR gameplay"
- 手势追踪讨论: UploadVR Review — "among the tightest hand tracking on the platform"
- 古典指挥手势教程: 搜索 "conducting patterns 4/4 time" 了解标准指挥拍子
