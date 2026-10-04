# 注册与晨间流程迁移

后续字体、动态效果和底部对齐修复见 [WEB_UI_PARITY.md](WEB_UI_PARITY.md)。下面的 23 项测试和旧场景不变记录是首次迁移时的验证快照。

## 使用入口

- 默认启动场景仍为 `Assets/Sleepet/Scenes/Sleepet_Home.unity`。
- 主页右上方 **Register** 小按钮进入注册演示。
- 从主页开始睡眠，滑动结束后进入起床反馈；不会再次创建睡眠记录。
- 注册流程：Splash → Account → ChooseCompanion → PersonalisePet → CreatingPet → MeetPet → Home。
- 晨间流程：WakeFeedback → MorningRating → MorningDrink → MorningStretch → MorningTodos → MorningEnd → Home。
- 已保存的 Routine 未勾选喝水或拉伸时，跳过对应步骤。早餐作为待办提醒显示。
- 起床页的睡眠数据按钮读取刚结束的会话，并以弹层显示。支持完成或跳过晨间步骤，上滑或点击结束页返回主页。

## 独立可编辑 Scene

新建 12 个场景，均位于 `Assets/Sleepet/Scenes`：

| 注册（6） | 晨间（6） |
| --- | --- |
| Sleepet_OnboardingSplash | Sleepet_WakeFeedback |
| Sleepet_Account | Sleepet_MorningRating |
| Sleepet_ChooseCompanion | Sleepet_MorningDrink |
| Sleepet_PersonalisePet | Sleepet_MorningStretch |
| Sleepet_CreatingPet | Sleepet_MorningTodos |
| Sleepet_MeetPet | Sleepet_MorningEnd |

每个场景都有 `Editable … Canvas / Page · 402 x 874`。文字、按钮、输入框、下拉菜单、图片、视频和效果层均已序列化，可直接在 Hierarchy / Inspector 修改，不依靠运行时创建整套 UI。

`SleepetFlow` 负责现有对象的绑定、动画与交互。`stretchSeconds` 默认为真实 300 秒，测试演示可以在 Inspector 显式缩短。各场景的 Home 按钮可退出流程。

已有 Home、Sleep、Me、Routine、Daily、Weekly、AR、DayDetail 保留。只有 Home 的场景文件增加 Register 按钮；其他七个场景文件未修改。原界面的运行时代码仅补充导航与资料数据绑定等必要整合。

`Sleepet / Create Missing Onboarding And Morning Scenes` 仅补建缺失场景，不会覆盖手工修改过的新场景。`SleepetFlowAuthoring.RebuildNewScenes` 是开发时显式重建全部新场景的入口，不要在手工调整后调用。

## 数据一致性

- 沿用 `SleepetSceneSession` 和 `SleepetDemo.Store`，跨场景保持同一后端对象。
- 注册资料先保存在会话草稿中，仅在 Meet 页确认后提交。取消不会覆盖原有宠物设置。
- Email、宠物名称、种类、品种、颜色、可选照片路径及完成标记扩展原 `settings.json`，不建立第二套 Preferences。
- 密码只参与本地输入检查，不保存、不上传、不写入日志。注册和登录均为本地原型，不提供真实身份认证。
- 主页、个人页与新场景读取相同宠物名称和已有宠物预设；个人页 Email 绑定保存后的地址，未重排界面。
- 原声音／音量设置保存改为复制完整 Preferences 后更新，避免丢失新增字段。
- 晨间计划读取现有 `tomorrow-plan.json`；进入晨间流程时保留计划快照。未引入浏览器 localStorage 或并行计划文件。
- 晨间评分、喝水／拉伸完成或跳过状态、稍后通知选项、完成时间保存在对应 `SleepSummary` 中，通过 `sessionId` 更新。
- 写入采用原 Store 的临时文件和原子替换机制。失败时显示错误并留在当前页面，不声称保存成功。
- 调试更改睡眠结果时按 ID 更新当前记录，保留新增的晨间反馈。

## 素材与功能边界

- 源前端只读，未修改。导入素材位于 `Assets/Sleepet/Art/Flow`。
- 背景与主要插画沿用源素材；UI 使用原生 Unity Canvas 控件。动画节奏、玻璃效果和过渡是 Unity 实现，并非浏览器逐像素渲染。
- 原喝水 VP9 WebM 不被当前 Unity 导入器支持，已转为 48 张透明 PNG 帧。其余视频已转换为 H.264 baseline。转换工具只在项目 Validation 内，游戏运行不依赖 FFmpeg。
- 默认 Mocha 保留源宠物动画。猫、其他宠物和另一种狗外观使用已有预设形象；没有为它们伪造 Mocha 的动作视频。
- 品种、颜色和照片作为个性化资料保存；当前没有根据照片生成新宠物模型的服务。
- 照片选择支持 Unity Editor 与 Windows Standalone。其他平台仍可完成流程，但尚未接入系统相册选择器。
- `Send me notifications later` 保存用户选择，沿用原项目尚未提供系统后台通知的边界。
- 起床反馈读取本地会话结果。原项目为活动／静止检测演示，不将其声明为传感器测量的睡眠质量。

## 验证资料

2026-10-04：Unity PlayMode 最终回归 **23 / 23 通过**（新流程 8 项、原场景 13 项、基础功能 2 项）。已检查十二个场景的实际 Unity 渲染图。测试均使用隔离数据目录。

- 新流程测试：`Assets/Sleepet/Tests/PlayMode/FlowTests.cs`。
- 场景与原业务回归：`SceneArchitectureTests.cs`、`P0Tests.cs`。
- 结果报告：`Validation/Flow/final-results.xml`。
- 十二个页面的 Unity 渲染预览：`Validation/Flow/Screens/`。
- 原场景 SHA-256：`Validation/Flow/original-scenes.json`，用于核对已有页面未被重做。

所有自动测试都使用 Validation 下的隔离存档，不改用户正常的 SleepetData。
