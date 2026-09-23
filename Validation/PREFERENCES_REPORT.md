# Preferences 下拉选项显示修复

根因：DefaultControls 的选项行高为 20px，Item Label 实际仅 17px；统一界面样式把字体提高到 18 号后，默认垂直截断导致文字整行不渲染。原文字已是深色，问题主要是文字容器高度。

修复：Sound、When asleep、Companion 三个菜单统一为 44px 选项行、36px 文字容器，并采用深色文字、浅色列表背景和浅绿色选中状态。修复已应用到 SleepetApp.prefab，也接入首次创建界面的流程。

回归增加实际展开三个菜单，验证每项文本高度、字形顶点生成、颜色对比、点击选择，并检查偏好保存和 Me 页滚动。修复前截图：preferences-before.png；修复后截图：Editable/preferences-Sound-open.png、Editable/preferences-Behaviour-open.png、Editable/preferences-Companion-open.png。

定向 PlayMode 回归通过：1/1，覆盖三个菜单全部选项的字形可见性、选择、保存与滚动。结果：preferences-results.xml。三个展开截图均已人工查看，文字完整显示。

Windows 演示重新构建成功：Builds/Editable/Sleepet.exe；日志 preferences-build.log。
