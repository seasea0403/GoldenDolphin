# 剧情系统实现指南

## 系统概览

完整的对话/剧情系统已实现，包括：
- 📝 对话管理器（PlotTriggerManager）
- 🎨 对话UI（DialogueFormLogic）
- 🎭 立绘显示/隐藏逻辑
- 👤 第一个客人剧情人物立绘绑定
- 💾 剧情完成状态存档

---

## 关键文件列表

### 新创建文件

| 文件路径 | 用途 |
|---------|------|
| `0_Scripts/DataTable/DialogueConfig.cs` | 剧情配置数据结构 |
| `0_Scripts/Manager/PlotTriggerManager.cs` | 剧情触发管理器（单例） |
| `0_Scripts/View/UI/DialogueFormLogic.cs` | 对话UI逻辑脚本 |

### 修改文件

| 文件路径 | 修改内容 |
|---------|---------|
| `0_Scripts/View/AreaSwitchManager.cs` | Start()中添加InitializePlotTriggerManager()初始化 |
| `0_Scripts/View/Customer/CustomerSlotManager.cs` | 添加剧情系统集成，第一个客人立绘绑定 |

### 既有文件（无需修改）

- `DataTables/Day.txt` - 已包含PlotId字段
- `UI/UIForms/DialogueForm.prefab` - 需要配置（见下文）
- `Configs/ExcelConfigs/Day*.csv` - 对话数据（CSV格式）

---

## 配置步骤

### 第1步：配置DialogueForm.prefab

**目标**：将DialogueFormLogic脚本和UI组件绑定到DialogueForm预制体

**操作步骤**：
1. 打开 `Assets/LingBoCanteen/UI/UIForms/DialogueForm.prefab`
2. 在预制体根节点添加 **DialogueFormLogic** 脚本
3. 在Inspector中配置以下字段：

```
DialogueFormLogic (Script)
├─ Speaker Name Text: 指向显示说话人名字的Text组件
├─ Dialogue Content Text: 指向显示对话内容的TextArea
├─ Character Portrait Image: 指向显示立绘的Image组件
├─ Portrait Canvas Group: 指向立绘的CanvasGroup（用于淡入淡出）
├─ Continue Button: 指向"下一步"按钮
└─ Portrait Fade Duration: 0.3 (立绘淡出/淡入时长)
```

**预制体结构参考**：
```
DialogueForm
├─ Panel (CanvasGroup)
│  ├─ Background
│  ├─ SpeakerNameText (Text)
│  ├─ DialogueContentText (TextArea)
│  ├─ PortraitImage (Image + CanvasGroup)
│  └─ ContinueButton (Button)
└─ (其他UI元素)
```

### 第2步：配置PlotIdMapping

**目标**：在PlotTriggerManager中配置每个PlotId对应的对话资源和立绘资源

**位置**：`0_Scripts/Manager/PlotTriggerManager.cs` 第8-14行

**当前配置**（已预设）：
```csharp
private static readonly Dictionary<int, (string dialogueAssetName, string portraitAssetName)> PlotIdMapping 
    = new Dictionary<int, (string, string)>()
{
    { 1, ("Day1", "Genius") },           // Day1 剧情
    { 2, ("Day4", "Genius") },           // Day4 剧情
    { 3, ("Day5", "Genius") },           // Day5 剧情
};
```

**如何添加新剧情**：
```csharp
{ PlotId, (DialogueAssetName, PortraitAssetName) }

示例：
{ 4, ("Day9", "Scholar") }  // Day9的剧情，使用Scholar立绘
```

- `PlotId`：Day.txt表中对应day行的PlotId值
- `DialogueAssetName`：CSV文件名（不含.csv后缀）
- `PortraitAssetName`：对话中使用的立绘资源名（对应DRGuest.AssetName）

### 第3步：生成DialogueAsset资源

**目标**：将CSV文件转换为DialogueAsset资源

**操作步骤**：
1. 确保 `Assets/LingBoCanteen/Configs/ExcelConfigs/` 中有对应的CSV文件
   - 例如：Day1.csv, Day4.csv 等
2. 在Unity编辑器菜单：**Tools > 剧情 > 导入CSV生成SO**
3. 资源将自动生成到 `Assets/Resources/DialogueAssets/`

**CSV格式要求**：
```
ID,SpeakerName,SpeakingContent,CharImage
1,我,（开场白）,
2,"天才",（回应）,Genius
3,我,（继续）,
```

字段说明：
- `ID`：行号（自增）
- `SpeakerName`：说话人名称（使用"我"表示玩家/旁白）
- `SpeakingContent`：对话内容（支持\n换行）
- `CharImage`：立绘资源名（留空表示隐藏立绘）

### 第4步：配置Day.txt

**目标**：为需要剧情的天数设置PlotId

**操作步骤**：
1. 打开 `Assets/LingBoCanteen/DataTables/Day.txt`
2. 找到需要添加剧情的day行，修改PlotId列：

```
	1	2	"1001,1002,1003"	"2001,2002,2003"	1    ← PlotId=1
	4	7	"1004,1005,1006"	"2004,2005,2006"	2    ← PlotId=2
```

3. 若day行没有剧情，PlotId保留为0或空

---

## 工作流程

### 游戏启动流程

```
Main.unity 加载
  ↓
AreaSwitchManager.Start()
  ├─ 初始化PlotTriggerManager单例
  ├─ 获取当前天数
  └─ 调用 PlotTriggerManager.CheckAndPlayPlotForDay(day)
      ├─ 检查Day表的PlotId
      ├─ 若PlotId=0，直接进入正常流程
      ├─ 若PlotId>0且未完成过，播放对话
      │  ├─ 打开DialogueForm UI
      │  ├─ 显示DialogueAsset中的对话行
      │  ├─ 逐行显示（点击推进）
      │  ├─ 根据CharImage字段显示/隐藏立绘
      │  └─ 完成后触发OnPlotDialogueComplete事件
      └─ 进入CustomerSlotManager生成客人流程
         ├─ 等待PlotTriggerManager的完成事件
         ├─ 第一个客人使用剧情人物的立绘
         └─ 其余客人随机生成
```

### 对话显示逻辑

```
PlayDialogue()
  └─ 逐行显示(ShowNextLine)
      ├─ 设置说话人名字
      ├─ 设置对话内容
      ├─ 根据CharImage更新立绘：
      │  ├─ if CharImage == null || SpeakerName == "我"
      │  │     淡出立绘（隐藏）
      │  │  else
      │  │     淡入新立绘（显示）
      │  └─ 支持不同立绘间的平滑过渡
      └─ 等待用户点击"继续"按钮
         └─ OnContinueButtonClicked() → 显示下一行
```

### 立绘显示规则

| 说话人 | CharImage | 显示结果 |
|------|---------|--------|
| "我" | 任何值 | ❌ 隐藏 |
| "天才" | "Genius" | ✅ 显示Genius |
| "天才" | 空 | ❌ 隐藏 |
| 其他NPC | 资源名 | ✅ 显示对应立绘 |

---

## 数据存储

### 存档字段

运行时数据由GameEntry.DataNode管理：
- `Story.FinishedPlotIdList`：逗号分隔的已完成剧情ID列表
  - 存档格式：`"Plot_1,Plot_2,Plot_3"`
  - 读档时自动恢复，重复进入已完成的剧情不再显示

### 临时数据

- `PlotTriggerManager.m_TodayPlotId`：当天的PlotId
- `PlotTriggerManager.m_TodayPlotCharacterId`：当天的剧情立绘资源名
- `CustomerSlotManager.m_CanSpawnCustomers`：是否允许生成客人

---

## 扩展指南

### 添加新的剧情

**步骤**：
1. **编写CSV**：在 `Configs/ExcelConfigs/` 创建新CSV文件
   ```
   Day20_Plot.csv
   ID,SpeakerName,SpeakingContent,CharImage
   1,我,（开场）,
   2,"神秘人",（出现）,Mystery
   ...
   ```

2. **更新PlotIdMapping**：
   ```csharp
   { 10, ("Day20_Plot", "Mystery") }  // PlotId=10
   ```

3. **更新Day.txt**：
   ```
   	20	8	...	...	10   ← Day20使用PlotId=10
   ```

4. **生成资源**：
   - 运行 **Tools > 剧情 > 导入CSV生成SO**

5. **测试**：
   - 进入第20天，对话应自动播放

### 自定义对话文本样式

修改 `DialogueFormLogic.cs` 中的显示逻辑：

```csharp
// 修改字体、大小、颜色等
private void ShowNextLine()
{
    DialogueLine currentLine = m_CurrentDialogueAsset.lines[m_CurrentLineIndex];
    
    m_SpeakerNameText.text = currentLine.speakerName;
    m_SpeakerNameText.fontSize = 30;  // 自定义
    m_SpeakerNameText.color = Color.white;  // 自定义
    
    m_DialogueContentText.text = currentLine.speakingContent;
    m_DialogueContentText.fontSize = 24;  // 自定义
    
    UpdatePortrait(currentLine.charImage);
    m_CurrentLineIndex++;
    m_IsWaitingForInput = true;
}
```

### 自定义立绘动画

修改 `DialogueFormLogic.cs` 中的FadePortrait方法：

```csharp
private IEnumerator FadePortrait(Sprite newPortrait)
{
    // 示例：添加缩放动画
    float startScale = m_CharacterPortraitImage.transform.localScale.x;
    float elapsed = 0f;
    
    while (elapsed < m_PortraitFadeDuration)
    {
        elapsed += Time.deltaTime;
        float progress = elapsed / m_PortraitFadeDuration;
        
        // 淡出
        m_PortraitCanvasGroup.alpha = 1f - progress;
        // 缩小
        m_CharacterPortraitImage.transform.localScale = 
            Vector3.one * Mathf.Lerp(startScale, 0.8f, progress);
        
        yield return null;
    }
    
    // 切换图片并淡入...
}
```

---

## 调试技巧

### 查看当天剧情状态

在Console中添加命令：
```csharp
PlotTriggerManager.Instance.IsPlayingPlot()  // 是否正在播放
PlotTriggerManager.Instance.GetTodayPlotCharacterId()  // 当天立绘
```

### 跳过对话

修改DialogueFormLogic添加快捷键：
```csharp
private void Update()
{
    // ESC 快速完成对话
    if (Input.GetKeyDown(KeyCode.Escape))
    {
        if (m_IsWaitingForInput)
        {
            ShowNextLine();
        }
    }
}
```

### 重置已完成的剧情

```csharp
// 在代码中调用：
GameEntry.DataNode.SetData("Story.FinishedPlotIdList", (VarString)"");

// 这样重新进入会再次显示对话
```

---

## 常见问题

### Q: 对话UI没有显示？
**A**: 检查以下项：
1. DialogueForm.prefab是否正确配置了DialogueFormLogic脚本
2. 各UI组件引用是否正确指派
3. Day.txt中的PlotId是否正确设置
4. DialogueAsset资源是否正确生成

### Q: 立绘没有显示？
**A**: 检查以下项：
1. CSV中CharImage列是否正确填写（对应DRGuest.AssetName）
2. 立绘资源是否正确导入
3. 说话人是否为"我"（如是，立绘会被隐藏）

### Q: 第一个客人没有使用剧情立绘？
**A**: 检查以下项：
1. PlotTriggerManager中的PlotIdMapping是否正确配置
2. 立绘资源名是否与CustomerSpawnData.AssetName匹配
3. 第一个客人是否在剧情播放完成后才生成

### Q: 剧情重复播放？
**A**: 检查以下项：
1. 存档是否正确保存了"Story.FinishedPlotIdList"
2. Day.txt中PlotId是否正确映射
3. 是否调用了PlotTriggerManager.CheckAndPlayPlotForDay()

---

## 总结

系统已完整实现，现需操作：
1. ✅ 编码完成
2. ⏳ **配置DialogueForm.prefab UI**（关键步骤）
3. ⏳ 通过 **Tools > 剧情 > 导入CSV生成SO** 生成资源
4. ⏳ 测试：进入Day1，验证对话是否正确显示

祝贺！剧情系统已就绪！🎉
