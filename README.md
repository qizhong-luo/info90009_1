# Sleepet Unity：独立页面 Scene

使用 Unity 6000.3.21f1 打开项目，从 `Assets/Sleepet/Scenes/Sleepet_Home.unity` 运行。iPhone 默认方向为竖屏，Figma 参考画板为 402×874。

## 可直接编辑的页面

| Scene | 内容 |
|---|---|
| `Sleepet_Home.unity` | 首页、Mocha、明日计划和 AR 入口 |
| `Sleepet_Sleep.unity` | 睡眠监测、闹钟和滑动结束 |
| `Sleepet_Me.unity` | 设置、时间与宠物编辑弹层 |
| `Sleepet_Routine.unity` | 明日计划及短事件编辑 |
| `Sleepet_Daily.unity` | 日报与外部健康 App 入口 |
| `Sleepet_Weekly.unity` | 周报 |
| `Sleepet_DayDetail.unity` | 周报选定日期的历史日报 |
| `Sleepet_AR.unity` | 摄像头／测试背景、放置与离线对话 |

Build Settings 只包含以上 8 个 Scene。每个 Scene 只有一个主页面；弹层是所属页面的编辑状态。文字、图片、按钮和位置都保存在对应 Scene 的 uGUI 对象中，可在 Scene 视图或 Inspector 直接修改。运行时不生成页面，也没有会覆盖 Scene 的 UI 生成脚本。

`Assets/Sleepet/Prefabs/SleepetApp.prefab` 现在仅是不可渲染的功能内核。`SleepetSceneSession` 在切换 Scene 时保留内核、`SleepetDemo.Store` 和睡眠会话；`SceneTransitionCount` 记录切换次数，`StateRevision` 记录已提交的状态更改。页面重新进入时从同一个数据对象和本地存档刷新。原初版 `PhoneFrame` 已删除。

底部月亮优先进入周报；周报的七个日期柱状按钮进入独立历史日报 Scene，右上角 `TEST +` 写入明确标记的固定样本数据（包括三段 Sleep Stage 时长）。详见 [HIGH_FI_IMPLEMENTATION.md](HIGH_FI_IMPLEMENTATION.md)。最新测试结果位于 `Validation/four-fixes-final.xml`（13/13 通过），页面截图位于 `Validation/SceneScreens/`。外部健康 App、摄像头权限、屏幕安全区和 iOS 构建仍需在真机验收。
