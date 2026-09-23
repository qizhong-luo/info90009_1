# Sleepet Figma → Unity 转换范围与执行方案

来源：[Capstone high fi · Page 1](https://www.figma.com/design/wL0dEDwxixjr25vdROZULV/Capstone-high-fi?node-id=0-1)。核对日期：2026-09-23。本文保留最初的范围规划；目前的 Unity 实施状态、逐页截图与真机待验项见 [HIGH_FI_IMPLEMENTATION.md](HIGH_FI_IMPLEMENTATION.md)。

## 1. 范围口径

Figma 文件只有一个画布页 `Page 1`。画布上同时放有完整手机界面、同一界面的不同状态、弹层、导航组件、素材板和零散图层；因此不宜把每个顶层 frame 都做成独立 Unity 页面。

按“注册 01–05”排除 5 张：`iPhone 17 · 01 Account`、`02 Choose`、`03 Personalise`、`04 Creating`、`05 Meet Mocha`。

按“Wake-up page 及 Morning routine 的八个界面”排除下列 8 张：

| 分类 | Figma 名称 | Node ID |
|---|---|---|
| Wake-up × 3 | `Wake-up page-rough nights`、`Wake-up page-medium nights`、`Wake-up page-great nights` | `32:76`、`32:65`、`32:53` |
| Morning routine × 5 | `Morning routine rating`、`Morning routine drinking`、`Morning routine stretching`、`Morning routine to-dos`、`Morning routine end` | `34:88`、`20:359`、`20:381`、`20:416`、`20:402` |

**待你核对的范围假设：** `morning routine setting page`（`8:28`）不在上述八张中，故暂时**纳入**转换。它是“Tomorrow’s Todo...”的设置界面，功能与五张晨间执行页不同。如果你所说的八张也包含这张，则应将它从下表移出，并指出八张中哪一张应纳入。

## 2. 拟转换的页面与状态

| Unity 页面／状态（建议名） | Figma 原始 frame（Node ID） | 核心交互与转换说明 |
|---|---|---|
| Home 首页 | `Homepage` (`1:38`) | 底部 Home/Sleep/Me 导航；点击 Mocha 反馈；进入睡眠监测、AR 陪伴；“Tomorrow’s morning routine”通往设置页。 |
| Sleep 睡眠监测 | `Sleep monitor page` (`1:20`) | 显示当前时间、闹钟、音乐和播放策略；选择／查看音乐；滑动结束睡眠。结束后的目标页需要明确：被排除的 Wake-up 页不制作，建议转至 Daily Report 或 Home，并保留温和结果提示。 |
| Tomorrow’s Todo 设置 | `morning routine setting page` (`8:28`) | 勾选 gentle start 事项、点选推荐事项、增删短事件、选择日期、保存；只做**睡前计划编辑**，不进入被排除的五张晨间执行页。 |
| Me / Setting 主页面 | `iPhone 17 - 51` (`38:180`)、`Setting` (`95:316`)、`iPhone 17 - 52` (`38:304`)、`Frame 22` 内的 `iPhone 17 - 53` (`175:610` / `175:608`) | 视作同一主页面的草稿／状态，不制作 4 份。包括用户资料、My Plan 时间盘、My Sleepet、声音、音量、模式，以及 Privacy & data、Accessibility、Connected Devices、Notifications、Help & Q&A 入口。具体弹层以以下两行处理。 |
| My Plan 时间选择弹层 | `iPhone 17 - 33` (`44:1507`) 中的 `Keyboard picker` | 时间输入、取消／保存，更新睡觉与起床计划；应保持跨午夜时间计算正确。 |
| My Sleepet 编辑弹层 | `iPhone 17 - 34` (`46:2023`) 中的 `Keyboard picker`；另有独立 `Keyboard picker` (`95:624`、`95:983`) | 头像候选、名称、外观、动作／姿势、预览／保存；这些是弹层状态，不是新的底部导航页。外观和动作须以实际可用素材为准。 |
| Sleep Daily Report | `Sleepet Daily Report` (`42:197`)、`Sleepet Daily Report normal` (`180:471`) | 日期、睡眠时长、目标、时间线、阶段组成、Mocha 文案、Watch 信息；两张按数据／状态合并为一个模板。报告内容可滚动，Day/Week 切换可进入周报。 |
| Daily Report 无手表状态 | `Sleepet Daily Report without Watch` (`53:324`) | 与日报共用模板；未连接设备时显示连接入口，不显示伪造的心率或手表数据。 |
| Sleep Weekly Report | `Sleepet Weekly Report` (`53:378`) | 七日柱状图、平均入睡时间、作息达成、共同模式；Day/Week 切换与日历周期选择。无记录日应为空缺，而非自动填入睡眠数据。 |
| 外部健康 App 入口 | `health` (`108:445`) | **不制作 Unity 内的 Health / Watch 详情页。** 日报／周报上的健康入口仅调用统一的外部 App 跳转接口。iOS 实现尝试打开目标健康 App；失败时显示可理解的提示。用户从外部 App 返回时恢复原 Unity 报告和滚动位置。左上角返回上一 App 的入口由 iOS 系统决定和显示，Unity 不绘制假的系统返回按钮。 |
| AR 查找平面 | `iPhone 17 - 42` (`48:373`) | 摄像头／模拟背景、扫描提示、返回、Place Mocha。现有 Windows 摄像头实现可复用，但它没有真实平面识别。 |
| AR 放置与聊天 | `iPhone 17 - 43` (`48:382`) | 放置 Mocha、输入／发送消息、AI 模型下拉、返回；模型选择只在确实接入对应服务时可用，否则作为演示选项并明确标示。 |
| AR 陪伴对话状态 | `iPhone 17 - 44` (`48:402`) | 对话气泡与宠物反馈，作为上一页的状态或子视图，不另建完整流程。 |

### 不作为独立页面的画布内容

`iPhone 17 - 27` (`1:92`) 只有图片层；`Slide 16:9 - 1/2` 是素材／Moodboard；`Home-navigation bar`、`Sleep-navigation bar`、`Group 4/5`、独立 `Keyboard picker`、`Goal Failed`、零散图层和背景属于组件、状态或设计注释。它们应被复用或参考，不单独加入导航。

## 3. Unity 执行方式

1. **先建立映射表。** 对上表各 frame 留存截图、尺寸（主要手机 frame 为 402×874）、颜色／字体／图标、交互热点、入口与返回路径。对重复 Me 画面选一张基准图，其他只提取差异状态；核对 `health` 视觉及控件。
2. **在现有工程上增量制作。** 项目为 Unity `6000.3.21f1`，当前场景 `Assets/Sleepet/Scenes/SleepetP0.unity`、主 prefab `Assets/Sleepet/Prefabs/SleepetApp.prefab` 已有 Home/Sleep/Me、Mocha、设置持久化、会话历史和 Windows 摄像头演示。保留这些数据与行为，扩展可编辑的 uGUI prefab／panel、复用状态栏、底部导航、按钮、卡片、弹层、图表组件；不要以整页图片充当可交互 UI。
3. **把页面导航和数据分开。** 建立统一页面路由及返回栈；偏好／计划／宠物外观本地保存，睡眠会话驱动日报和周报。无数据、无设备、摄像头失败、未接入 AI 都要有可交互的明确状态。导航中的每个入口都应有去向；Figma 未提供的 Privacy、Help 等详情页只做清楚标注的说明弹层，待补设计后再扩充。健康入口建议为 `IExternalHealthAppLauncher.Open()` 一类接口：Unity UI 只处理点击、失败提示、离开／返回时的页面状态；具体打开动作由 iOS 原生适配层实现，Windows 演示版显示“仅在 iPhone 可用”。
4. **分阶段落地。** A：Home、Sleep、Me 视觉与导航；B：计划时间弹层、宠物编辑、Tomorrow’s Todo；C：日报、无手表状态、周报及外部健康 App 入口；D：AR 查找／放置／对话；E：全流程联测、真机适配。桌面先沿用现有摄像头叠加演示；若目标为手机真实 AR，再单独加 AR Foundation、平台配置与设备测试，这部分不能由 Figma 画面自动生成。
5. **逐页验收。** 对照 Figma 检查竖屏布局、滚动和弹层遮挡；检查所有可见按钮、滑杆、选择器、返回键、保存／取消、空状态；重启后检查保存数据；用实际会话验证日报／周报；在 iPhone 真机上检查外部 App 打开、失败提示和返回后页面恢复；分别检查 Windows 摄像头和目标手机设备。避免把模拟夜晚、睡眠阶段、Watch 心率或 AI 回复当作实测结果。

### 外部健康 App 跳转的实现边界

接口和 iOS 原生桥接可以实现。苹果公开文档提供 `UIApplication.open` 来打开**有受支持 URL 的外部 App**；但目前未查到苹果公开承诺的“直接打开 Apple 健康 App”URL。故不应把未公开的 `health://` 等 scheme 当作必然可用的交付保证。实施时可在目标 iOS 版本的真机上验证候选方式；若无法可靠打开 Apple 健康，则入口显示“请从主屏幕打开健康 App”并维持当前报告页。用户提到的左上角“返回 Sleepet”属于 iOS 的系统界面，是否出现以及具体样式由系统决定。此跳转本身不需要 HealthKit 数据读写权限；若以后要在 Unity 内读取健康数据，则是另一项功能。

## 4. 实施前需要确认的两点

1. `morning routine setting page` 是否在转换范围内（本方案暂列“是”）。
2. 交付目标是 **Windows 可交互演示**，还是 **iOS/Android 竖屏应用并实现真实 AR／设备连接／在线 AI**。两者的 UI 页面清单接近，但底层工作量和可验收功能不同。当前方案以现有 Windows 演示为第一阶段，并把真实设备能力列为后续独立阶段。
