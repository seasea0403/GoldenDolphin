导出设置指南
你当前是 UGF Package单机）模式，正确导出流程是四步：

1. 构建资源（AssetBundle）

菜单 GameFramework → Resource Editor：确认资源收集无误后 Save Collection
菜单 GameFramework → Resource Builder：
InternalResourceVersion 每次构建 +1（当前 14，下次用 15）
平台建议只勾 Windows x64（现在 Platforms=129 同时构建 32/64 位，浪费时间）
输出目录 D:/Desktop/BUILD，保持 Package 输出勾选，Build Resources
2. 同步到 StreamingAssets

把构建输出的 Package 目录内容拷贝覆盖到 Assets/StreamingAssets/（你现在的 StreamingAssets 就是这么来的镜像）。覆盖前建议先清空 StreamingAssets 旧镜像，否则像 Entity.dat 这类已删除资源会残留、白白增大包体。
3. PlayerSettings（File → Build Settings → Player）

场景顺序已正确：Launcher → Menu → Main（Launcher 必须在 号位，UGF 从它启动）
Product Name 现在是"灵啵食堂"，而代码和简历都是"灵薄食堂"，建议统一，避免导出文件夹名对不上
Company Name 还是 DefaultCompany，Bundle Version 还是 1.0，发版时记得改
applicationIdentifier 是 com.DefaultCompany.2DProject，建议改成 com.你的名字.lingbocanteen
4. Build

目标 PC, Mac & Linux Standalone → Windows，架构 x86_64，Build 即可