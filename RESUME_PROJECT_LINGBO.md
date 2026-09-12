## 项目经历

### 灵薄食堂 · Unity 叙事驱动模拟经营游戏 · 核心开发
*2024.xx – 2024.xx  |  团队项目（x人）*

**项目概述**：一款以"三界食堂"为题材的暗黑奇幻风 2D 模拟经营游戏。玩家经营一间连通人间、天堂、地狱的食堂，在 20 天剧情中经历采购→备菜→烹调→上菜→结算的日常循环。拥有完整的叙事分支系统，根据玩家 San 值（精神力）和区域选择触发 5 种不同结局。项目基于 Ellan Jiang 的 **GameFramework** 框架构建，全项目约 **86 个 C# 脚本、25,000+ 行代码**。

**核心职责与技术亮点**：

- **GameFramework 框架应用**：深度使用 GameFramework 框架搭建项目骨架。基于 Procedure 状态机驱动游戏流程（启动→菜单→游戏主循环），利用 DataTable 系统实现菜品/食材/NPC/音效等 10+ 张配置表的策划驱动数据管理，通过 DataNode 层级数据树管理全部运行时状态（Player/Day/Area/Business/Storage/Story），使用 Event 事件池系统实现模块间解耦通信。

- **日循环阶段状态机**：设计并实现了完整的每日流程调度系统（`EveningDayFlow`）。每天被划分为傍晚（采购）和白天（经营）两个时段，包含 DayStart → Market → Business → StoryCheck → Settlement 五个阶段。状态机通过 `GameEntry.Event` 广播阶段变更事件，各子系统（厨房/顾客/UI）根据阶段事件激活或休眠，支持 20 天完整叙事推进。

- **三道工序的烹饪流水线**：实现了一个多步骤食材加工管线。食材支持切（CuttingBoard）、榨汁（Juicer）、混合烹饪（Pot）三种加工方式，支持"半成品菜"（Intermediate Dish）作为中间产物参与后续烹饪——例如先制作番茄肉酱，再将其作为食材投入最终菜品。每个加工站提供独立的拖拽交互和输出槽管理，通过 `DishRecipeUtility` 统一解析配方匹配逻辑。

- **San 值驱动的分支叙事系统**：设计了一个与核心玩法深度绑定的叙事触发机制。顾客满意度影响 San 值（成功 +2、失败 -3），San 值跨越阈值（70/30）自动触发区域传送（天堂/地狱），San 值为 0 或 100 触发死亡结局。第 15 天前未换区则激活"疑心模式"，在第 20 天根据最终 San 值区间判定真结局/普通结局/隐藏结局共 5 种结局路线。

- **MVC 顾客系统**：采用严格的 Model-View-Controller 分离设计。`CustomerEntity` 作为纯数据实体（进入/等待/离开状态机），不持有任何 UI 引用；`CustomerBubbleView` 作为视图层独立渲染订单气泡、耐心度滑块和 Buff 图标；`CustomerSlotManager` 作为控制器协调顾客生成、UI 绑定和菜品匹配逻辑。实体数据通过 `ReferencePool` 对象池管理，优化 GC 压力。

- **三区域 CanvasGroup 切换优化**：实现了一个零实例化开销的区域切换系统（`AreaSwitchManager`）。点单/备菜/烹饪三个区域通过切换 `CanvasGroup` 的可交互属性和 `Collider` 的启用状态实现显隐切换，从不销毁/重建对象，所有子系统保持后台逻辑运行，避免了频繁的 Awake/Start 调用。

- **CSV 驱动的剧情导入管线**：开发了一个 Editor 工具（`DialogueImporter`），从策划配置的 TSV 表格自动生成 `DialogueAsset` ScriptableObject。支持多层嵌套对话结构（节点 → 多分支对话行 → 角色台词），包含立绘表情切换、打字机效果、背景切换等指令。实现策划零代码配置剧情。

- **跨场景音效管理**：基于 `MonoSingleton` 实现 BGM/SFX 双通道音效管理器。支持区域感知的背景音乐切换（人间/天堂/地狱 × 白天/傍晚共 6 种 BGM 组合），通过 DOTween 实现无缝交叉淡入淡出，音量设置持久化到 PlayerPrefs。

**技术栈**：Unity 2022 LTS、C#、GameFramework、DOTween、TextMeshPro、ScriptableObject、Editor Tools
