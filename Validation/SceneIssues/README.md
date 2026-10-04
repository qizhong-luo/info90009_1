# Unity 场景修复验证

日期：2026-10-04。入口：`Assets/Sleepet/Scenes/Sleepet_Home.unity`。

## 已修复

1. 注册第 2 步：Breed / Colour 下拉列表恢复可读的选项文字、行高和勾选标记。
2. 主页宠物：点击可见宠物产生缩放与摇摆反馈，重复点击后仍会复位。
3. Change routine：先实际保存，再显示 “Your routine has been saved.” 弹窗。
4. Tomorrow’s Todo：日期按钮打开独立日期编辑弹窗，支持保存、取消及有效日期校验；新增多个事件进入上方可勾选列表，下方已选事件的 × 逐条删除。兼容旧 shortEvent 存档，晨间提醒及 AI 上下文读取新事件列表。
5. Chat：白色圆角多行输入框、内嵌圆形发送按钮；移除 Here with you 和 On your device 文案，保留必要的请求中/失败状态。
6. Weekly：横线按有记录日期实际显示的时长柱高度取平均；空数据隐藏横线。
7. Sleep / Morning：避免重复设置 AudioSource.clip 打断播放，活跃睡眠会话从 Silence 切换到声音后恢复播放；晨间通知圆框可点击并显示勾选。
8. Morning end：移除点击完成绑定；上滑超过 60 个界面单位且纵向距离大于横向距离后完成，短滑/横滑取消。

## 验证结果

- `../scene-issues-results.xml`：首轮 PlayMode 32 通过、0 失败、1 跳过。跳过项为需要外部接口配置的在线 AI 集成测试。
- `../scene-issues-final-results.xml`：最终 7 项专项 PlayMode 测试全部通过。
- 真实加载序列化场景后验证：下拉选择、事件增删/勾选/重新加载、无效日期与闰日、宠物复位、音频播放与音量/音源/静音切换、平均线随数据变化、聊天控件、通知点击命中、上滑处理器及完成持久化。
- `Screens/`：注册下拉框、日期弹窗、保存提示、事件列表、周报、聊天输入区和晨间勾选的渲染截图；已目视复查。

## 参考限制

指定 Figma 文件的 `Slide 16:9 - 1`（节点 `1:98`）返回的是配色素材，动画接口未返回动画轨道。因此主页点击反馈使用项目现有宠物图实现，并非声称逐帧复刻该 Figma 动画。

场景修改已保存为可在 Inspector 中编辑的控件。可通过菜单 `Sleepet > Fix Reported Scene Interactions` 重复应用本次控件修复。现有独立 Windows 安装包未重新打包。
