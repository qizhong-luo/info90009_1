# Sleepet：Figma → Unity 竖屏实施与验收

参考设计：[Capstone high fi](https://www.figma.com/design/wL0dEDwxixjr25vdROZULV/Capstone-high-fi?node-id=0-1)。Unity 版本：6000.3.21f1。参考画板：iPhone 竖屏 402×874。从 `Assets/Sleepet/Scenes/Sleepet_Home.unity` 运行。Home、Sleep、Me、Routine、Daily、Weekly、AR、DayDetail 分别保存在 `Assets/Sleepet/Scenes/Sleepet_<页面>.unity`；每个 Scene 只有一个可见主页面，可在 Scene 视图或 Inspector 直接修改 uGUI。`Assets/Sleepet/Prefabs/SleepetApp.prefab` 只保留不可渲染的功能内核；原初版 `PhoneFrame` 与整页 UI 生成脚本已删除。

`SleepetSceneSession` 使用 `DontDestroyOnLoad` 在 Scene 切换时保持同一个 `SleepetDemo.Store`、当前睡眠会话、所选报告日期和相机状态；`SceneTransitionCount` 自动累计切换次数，`StateRevision` 在已保存的设置、计划、会话和固定样本变化后递增。场景载入时页面从该对象和本地文件刷新。Build Settings 只列出以下 8 个主页面 Scene，弹层仅作为所在页面的编辑状态。

## 页面与设计映射

| Unity 页面／状态 | Figma 节点 | 可见操作的结果 | 截图 |
|---|---|---|---|
| Home | `1:38` | Mocha 点击反馈；明日计划、开始睡眠、AR、底部导航分别进入对应页面。 | `Validation/SceneScreens/Home.png` |
| Sleep monitor | `1:20` | 首页按钮开始会话；音乐和播放策略切换并保存；闹钟打开时间弹层；滑动结束进入日报；返回首页保留会话。 | `Sleep.png` |
| Me / Setting | `95:316`，主页面按用户后续指定的 Sleepet 网页 | 原生可编辑 uGUI；拖动圆盘保存时间；头像／名字点击打开预设头像和 1–10 位英文字母编辑器；声音、模式左右切换，音量即时保存。支持区滚动可达，入口均有具体结果。 | `Me.png`、`SettingCustomized.png`、`SettingSupport.png` |
| 时间与宠物弹层 | `95:624`、`95:983` | 时间使用小时／分钟与 AM/PM，wind-down 使用分钟／秒；取消不保存，非法输入保留弹层并提示。宠物有三个物种预设、可用外观与姿势、预览及保存；未提供的资源有明确说明。 | `SettingTimePicker.png`、`SettingPetPicker.png` |
| 用户资料弹层 | 用户追加要求 | 4 个头像预设；名字只接收 A–Z/a–z，最多 10 个字母；取消丢弃草稿；保存到本地并同步首页问候。 | `SettingProfileEditor.png` |
| Tomorrow's Todo | `8:28` | 清单切换、日期切换、短事件增删；保存成功返回首页，重新打开恢复本地计划。 | `Routine.png` |
| Daily Report | `53:324`（Sleepet Daily Report without Watch）；`180:471`（文字与正常会话参考） | 睡眠会话结束后进入；无左上角返回按钮，日期条仅展示日期，底部月亮入口进入周报。固定测试记录按 Figma 展示时长卡、睡眠阶段环图与 Deep/Light/REM 图例；没有传感器阶段数据的真实记录给出明确说明。底部 Watch 卡调用外部健康 App 跳转接口。 | `Daily.png`、`DayDetailWithTestData.png` |
| Weekly Report | `53:378` | 底部月亮优先进入周报；近七天作息目标计数、平均入睡时间、时间段柱图及平均时长；七个日期按钮各自进入历史日报。`TEST +` 写入固定七天样本，可重复点击且不复制。无传感器记录以 session 标明，睡眠达标次数仅由样本计算并标注。 | `Weekly.png`、`WeeklyWithTestData.png` |
| DayDetail / 历史日报 | `53:324` | 独立 Scene；展示周报中选定日期的记录或空状态；有固定测试阶段数据时使用同一睡眠阶段布局，底部月亮返回周报。 | `DayDetail.png`、`DayDetailWithTestData.png` |
| AR 查找／放置／对话 | `48:373`、`48:382`、`48:402` | 摄像头或明确标记的测试背景、放置／拖动宠物、离线对话；放置状态返回查找状态，再返回原页面。宠物读取同一份已保存物种、外观和姿势。 | `AR.png`、`ARWithCustomizedPet.png` |

注册 `01–05` 与原计划列出的 8 张 Wake-up/Morning Routine 执行页没有纳入；`8:28` 是睡前计划设置，已纳入。

## 数据与外部功能边界

- 会话开始和结束、设置、宠物名字、明日计划及实际完成的会话历史保存在项目原有本地数据机制中。周报可通过右上角按钮明确加入一组 `sample=true` 的固定测试记录；重复点击会替换该组样本，不会覆盖真实记录。七晚样本各有固定的 Deep、Light、REM 分钟数，总和等于当晚时长；历史日报显示对应日期的阶段数据和 `FIXED TEST DATA` 标记。真实会话没有传感器数据时仍显示不可用说明，不会把样本当作真实测量。
- Home 和 Me 不再绘制固定的 iPhone 时间、信号、电量；顶部系统状态由 iOS 负责。底部导航栏使用设计中的半透明深蓝底与白色 14% 选中层，三个图标资源使用透明底。AR 放置的宠物直接读取已保存的宠物选项资源，编辑器中的 AR Scene 默认图也改为彩色宠物；宠物名字同步到放置提示。
- 最新视觉修正已写入各自 Scene 的 uGUI 对象。可以在 Unity Inspector 逐个调整。`Sleepet/Apply Audited Figma Interactions` 是保留在项目中的场景维护菜单，会重新应用本轮参考布局；日常手动改 UI 无需执行它，避免覆盖自己调整的位置。`SleepetSettingsSceneAuthoring`、`SleepetLogicSceneAuthoring` 都位于 Editor 文件夹，不进入运行时；运行时只更新已存在的文字、图形和状态。
- 健康卡片通过 `IExternalHealthAppLauncher` 调用外部 App 地址。当前 `HealthAppUrl` 为空：点击后有明确提示，不会假装已打开 Apple 健康。需要在 iPhone 上确认一个受支持的目标 URL 或增加原生适配器，再填入此字段。iOS 左上角返回上一 App 属于系统界面，由系统决定显示；Unity 中不画假按钮。
- AR 当前是摄像头画面上的 2D 宠物叠加，测试背景会明确提示；没有 ARKit 平面检测和空间锚定。对话默认离线演示，未接入云模型。系统通知、HealthKit 数据、真机摄像头权限和真实 AR 需另行真机验收。

## 本地验证与真机验收

PlayMode 测试检查 Build Settings 严格为 8 个 Scene、每个 Scene 只有一个主页面、跨 Scene 的同一数据对象、切换计数、设置与会话持久化、月亮入口、七个日期选择、测试样本幂等写入及真实记录优先、AR 宠物选项一致性、Home/Me 状态栏移除、底部导航透明度、阶段样本数值、设置头像裁切、Mode 行点击区域与外部健康入口结果，以及 402×874 页面截图。最终结果文件为 `Validation/figma-final.xml`（13/13 通过，2026-09-23 20:48 本地时间）；`Validation/SceneScreens/` 包含空状态、`WeeklyWithTestData.png`、`DayDetailWithTestData.png` 与 `ARWithCustomizedPet.png`。桌面横屏 Game View 将竖屏应用居中显示；iPhone 竖屏以屏幕宽度为缩放基准。

iPhone 真机逐页验收仍需要：同尺寸竖屏视觉对照、刘海／安全区域、触摸拖动和滚动、离开和返回 App、健康 App 跳转失败与成功路径、摄像头权限、不同机型缩放。当前 Windows 环境未安装 Unity iOS Build Support，无法在本机生成 iOS 包或完成上述真机验收。逐像素 1:1 视觉一致仍需按真机截图调整字体、图标、卡片细节与安全区。
