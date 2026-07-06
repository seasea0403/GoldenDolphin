# 剧情系统快速参考卡

## 🎬 系统架构

```
┌─────────────────────────────────────────────────┐
│           Main Scene Loaded                      │
│  AreaSwitchManager.Start()                       │
└────────────────┬──────────────────────────────────┘
                 │
        ┌────────▼────────┐
        │ PlotTriggerMgr  │
        │ (Initialized)   │
        └────────┬────────┘
                 │
    ┌────────────┴────────────┐
    │                         │
  ┌─▼──────────┐    ┌─────────▼────┐
  │ Has Plot?  │    │ No Plot       │
  │ (PlotId>0) │    │ → Direct Flow │
  └─┬──────────┘    └──────────────┘
    │
  ┌─▼──────────────┐
  │ Already Seen?  │
  └─┬──────────────┘
    │
  ┌─▼──────────────────────┐
  │ Load DialogueAsset      │
  │ Open DialogueForm UI    │
  │ Play Dialogue Lines     │
  │ (Click to Progress)     │
  └─┬──────────────────────┘
    │
  ┌─▼──────────────────────┐
  │ Mark Plot as Finished   │
  │ Fire Completion Event   │
  └─┬──────────────────────┘
    │
  ┌─▼──────────────────────┐
  │ CustomerSlotManager     │
  │ Enable Spawning         │
  │ 1st Customer:           │
  │ Use Plot Character      │
  └──────────────────────────┘
```

## 📋 配置清单

### 1️⃣ 数据配置
- [ ] CSV 文件准备 (Configs/ExcelConfigs/)
- [ ] PlotIdMapping 配置 (PlotTriggerManager.cs L8-14)
- [ ] Day.txt 配置 PlotId 字段

### 2️⃣ UI 配置（关键！）
- [ ] DialogueForm.prefab 添加 DialogueFormLogic 脚本
- [ ] 配置 Speaker Name Text
- [ ] 配置 Dialogue Content Text
- [ ] 配置 Character Portrait Image
- [ ] 配置 Portrait Canvas Group
- [ ] 配置 Continue Button

### 3️⃣ 资源生成
- [ ] Tools > 剧情 > 导入CSV生成SO

### 4️⃣ 测试验证
- [ ] Day 1 对话显示
- [ ] 立绘显示/隐藏
- [ ] 第一客人立绘
- [ ] 存档恢复

---

## 🔧 常用代码片段

### 查询当天剧情状态
```csharp
if (PlotTriggerManager.Instance != null)
{
    bool isPlaying = PlotTriggerManager.Instance.IsPlayingPlot();
    string portraitName = PlotTriggerManager.Instance.GetTodayPlotCharacterId();
    Debug.Log($"Playing: {isPlaying}, Portrait: {portraitName}");
}
```

### 添加新剧情（3步）
```csharp
// 1. PlotTriggerManager.cs
{ PlotId, ("DialogueAssetName", "PortraitAssetName") }

// 2. Day.txt
	DayNum	...	...	...	PlotId

// 3. 运行导入
// Tools > 剧情 > 导入CSV生成SO
```

### 重置已完成的剧情
```csharp
GameEntry.DataNode.SetData("Story.FinishedPlotIdList", (VarString)"");
```

### 手动触发对话
```csharp
DialogueAsset asset = Resources.Load<DialogueAsset>("DialogueAssets/Day1");
GameEntry.UI.OpenUIForm(UIFormId.DialogueForm);
// 通过 DialogueFormLogic.PlayDialogue() 开始播放
```

---

## 📁 文件位置速查

| 用途 | 路径 |
|-----|------|
| 对话脚本 | `0_Scripts/View/UI/DialogueFormLogic.cs` |
| 剧情管理 | `0_Scripts/Manager/PlotTriggerManager.cs` |
| UI配置 | `UI/UIForms/DialogueForm.prefab` |
| CSV数据 | `Configs/ExcelConfigs/*.csv` |
| 数据表 | `DataTables/Day.txt` |
| 生成资源 | `Resources/DialogueAssets/` |

---

## 🎯 工作流量化指标

| 指标 | 含义 |
|-----|------|
| `PlotId = 0` | 该天无剧情 |
| `PlotId > 0` | 该天有剧情 |
| `CharImage = ""` | 隐藏立绘 |
| `SpeakerName = "我"` | 玩家/旁白（强制隐藏立绘） |
| `m_CanSpawnCustomers = false` | 剧情播放中，客人未生成 |
| `Story.FinishedPlotIdList` | 存档已完成的剧情列表 |

---

## 🐛 调试技巧

### 快速测试对话
```csharp
// 在 Game 场景按 Play 后，Console 输入：
PlotTriggerManager.Instance?.CheckAndPlayPlotForDay(1);
```

### 查看剧情播放状态
```
Debug.Log: "[PlotTriggerManager] CheckAndPlayPlotForDay"
Debug.Log: "✅ 剧情 Plot_X 标记为已完成"
Debug.Log: "✅ 剧情播放完成，进入正常游戏流程"
Debug.Log: "✅ 第一个客人使用剧情人物 X 的立绘"
```

### 强制跳过对话
按 ESC（需要在 DialogueFormLogic 中添加）

---

## ⚡ 性能优化建议

1. **对话资源懒加载**
   - 已实现：Resources.Load() 按需加载
   
2. **立绘缓存**
   - 建议：首次显示后缓存 Sprite，避免重复加载

3. **UI 复用**
   - 已实现：DialogueForm 为单例 UI

4. **音频支持**（可选扩展）
   - 在 DialogueFormLogic 中添加：
   ```csharp
   SoundManager.Instance.PlayDialogueSound(currentLine.speakerName);
   ```

---

## 📝 使用示例

### 示例 1：Day 1 剧情

**CSV (Day1.csv)**：
```
ID,SpeakerName,SpeakingContent,CharImage
1,我,这里是哪？,
2,"天才",欢迎来到灵啵食堂！,Genius
3,我,我是怎么来这的？,
4,"天才",你是我们选定的打工人！,Genius
```

**PlotIdMapping**：
```csharp
{ 1, ("Day1", "Genius") }
```

**Day.txt**：
```
	1	2	"1001,1002,1003"	"2001,2002,2003"	1
```

**结果**：
- 进入 Day 1 → 自动播放对话
- "天才"说话时显示 Genius 立绘
- "我"说话时隐藏立绘
- 完成后第一客人使用 Genius 立绘

---

## 🎓 扩展方向

1. **选择支线对话**
   - 在 DialogueLine 中添加 ChoiceA/ChoiceB 字段
   
2. **变量支持**
   - 对话中支持 {PlayerName} 等变量替换

3. **条件触发**
   - 基于 SAN 值、剧情进度等条件显示不同对话

4. **音声配置**
   - 每条对话对应不同角色的语音

---

**更新时间**：2026-07-06 ✨
