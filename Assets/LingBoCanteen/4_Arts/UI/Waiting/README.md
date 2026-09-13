# 加载页面搭建说明

## 素材清单（loading_assets/）
| 文件 | 用途 |
|---|---|
| char_chop_up.png   | 人物举刀（常态/行走） |
| char_chop_down.png | 人物落刀（切菜瞬间） |
| food_stages/food_00~12.png | 遮挡式中间状态：底层是切好的图，上层完整黄瓜从切口处逐渐缩短（索引=已切刀数） |
| food_cut.png       | 100% 时的最终切好的食物 |
| food_whole.png     | 完整食物（备用） |
| bar_bg.png / bar_fill.png | 进度条背景 / 填充 |
| demo_loading.gif   | 进度驱动效果的完整演示 |

## 场景结构（Canvas，Scaler 1920x1080）
```
Canvas
└── LoadingView
    ├── Bg          Image 铺满，纯色 #FCE6AD（或你的背景图）
    ├── Food        Image，锚点居中偏下，source 用 food_00
    ├── Character   Image，放在 Food 的【下方】（同级排前面，脚被食物挡住）
    ├── BarBg       Image，食物下方
    └── BarFill     Image，挂 BarBg 同级，Type=Filled/Horizontal，fillAmount=0
```
注意：BarFill 的 Image Type 必须设为 Filled（Fill Method: Horizontal），
脚本直接改 fillAmount；不用 Filled 的话改成 RectTransform 改宽度也行。

## 运行逻辑
1. LoadingView.SetProgress(p) 是唯一入口，由加载逻辑喂进度
2. p 线性映射人物 x 位置：左端 → 右端
3. 每跨过 1/12 触发一次切菜：落刀帧 0.15s → 举刀帧，完整黄瓜的"盖子"从切口处缩短一截，露出下一段切好的黄瓜
3.5 人物位置始终站在切口（完整黄瓜的左边缘）上，切完一刀盖子和人物一起右移
4. p=1 时食物换成 food_cut，停留片刻后切场景
5. 非切菜时人物有 sin 上下浮动模拟行走（素材是单帧，用浮动代偿）

