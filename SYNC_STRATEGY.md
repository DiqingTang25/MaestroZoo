# Claude Code ↔ Codex 同步协作策略

## 共享基础设施

### MCP Unity Bridge
- **WebSocket 端口**: 8090
- **配置文件**: `ProjectSettings/McpUnitySettings.json`
- Claude Code 和 Codex 都可以通过 MCP Unity 同时连接到运行中的 Unity Editor
- 这是两者之间**唯一的 Unity 交互通道**

### Git 版本控制
- 已初始化 git 仓库
- `.gitignore` 已配置（忽略 Library/、Temp/、Build/ 等）
- **修改任何 C# 文件前，先 `git pull`；修改后立即 `git commit`**

## 并行工作方式

### 方案 A：按子系统分工（推荐）
```
Claude Code 负责:                    Codex 负责:
├── Input/ (手势识别)                 ├── UI/ (HUD, 菜单)
├── Core/ (判定逻辑, 音符生成)        ├── Editor/ (场景构建, 工具)
├── Resources/Charts/ (谱面数据)      ├── Models/ (3D 模型导入)
└── OrchestraController              └── GameHud, HudConnector
```

### 方案 B：按分支分工
```bash
# Claude Code 工作区
git checkout -b feat/gesture-recognition
# ... 修改 Input/ 相关文件 ...
git add -A && git commit -m "feat(gesture): 完成手势识别"
git checkout main && git merge feat/gesture-recognition

# Codex 工作区  
git checkout -b feat/ui-polish
# ... 修改 UI/ 相关文件 ...
git add -A && git commit -m "feat(ui): 完善 UI"
git checkout main && git merge feat/ui-polish
```

## 文件冲突预防

1. **每次修改前拉取**: `git pull --rebase`
2. **小步提交**: 每个功能点完成就 commit，不要攒大批量
3. **不同文件不同人**: 严格遵守子系统分工
4. **沟通**: 修改 scene 文件前通知对方（.unity 文件是二进制，无法合并）

## MCP Unity 共享规则

- Unity Editor 只有一个实例运行
- 两个 AI 工具可同时连接同一个 WebSocket 服务器
- 避免两个工具同时执行会修改场景的 MCP 命令

## 快速命令

```bash
# Claude Code
cd D:/UnityProjects/Zoo
git pull --rebase
# ... 工作 ...
git add -A && git commit -m "描述修改"
git push

# 查看对方的最新修改
git log --oneline -10
```
