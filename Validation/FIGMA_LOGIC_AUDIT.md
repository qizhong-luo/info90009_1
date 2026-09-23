# Figma 逻辑回归核对 · 2026-09-23

## 核对依据与优先级

读取了 [Setting 95:316](https://www.figma.com/design/wL0dEDwxixjr25vdROZULV/Capstone-high-fi?node-id=95-316) 及本范围各画板的文字和 prototype reactions。读取结果保存在 `FigmaLogicConnections.json`。时间／宠物弹层以 Setting 当前连线指向的 `95:624`／`95:983` 为依据；不再沿用旧弹层节点。

设置主页按用户最新指定的 [Sleepet 网页](https://qinghao-f.github.io/Sleepet/) 布局制作，头像与名字编辑是追加交互。用户明确覆盖原型的要求优先：不画系统状态栏、月亮优先打开周报、七个日期分别进入独立历史日报、增加样本周按钮、Health 只预留外部接口、排除注册与 Wake-up/Morning 执行页。

## 页面逐项核对

| 页面 | Figma 路径／操作 | 本次检查和修复结果 |
|---|---|---|
| Home `1:38` | Me→Setting；月亮→Weekly；Ready→Sleep；AR→查找；Tomorrow→计划 | 保持对应独立 Scene 跳转；新资料名字同步首页问候；宠物物种／外观／姿势读取统一偏好。 |
| Sleep `1:20` | 显示 Alarm、音乐、行为；滑动结束 | Alarm 已修正为 wakeTime。时间修改使用同一弹层；结束保存会话并进入 Daily。原型终点 `32:65` 属于排除的唤醒流程，因此按现有批准范围落到 Daily。 |
| Setting `95:316` | 时间摘要→`95:624`；Change Pet→`95:983`；月亮→Weekly；Home→Home | 摘要点击和圆盘手柄点击打开时间弹层；手柄拖动直接保存；头像／姓名可点击；声音／模式左右切换、音量即时保存；支持区可滚动。 |
| Time `95:624` | BEDTIME、WAKE UP，AM/PM；WIND-DOWN 分钟／秒；Cancel／Save | 实际数字输入与 AM/PM 按钮；检查小时 1–12、分秒 0–59、wind-down 分钟 0–180；取消不保存；保存后 Settings 和 Sleep 同步。 |
| Pet `95:983` | 物种缩略图；名称；Appearance；Movements/Poses；Preview；Save | 三种预设；黑白犬另有金色外观和躺卧姿势。当前无额外资源的组合显示明确提示。Preview 只预览当前草稿，不再切换物种；保存后 Home／Me／AR 一致。加号说明当前预设范围。 |
| Tomorrow's Todo `8:28` | 保存→Home | 已修复保存后停留原页的偏差；成功写入后返回 Home，重新进入恢复计划。 |
| Daily `53:324` | 无 Watch 样式；Health；底部导航 | 保持时长卡、环形阶段图和图例布局；真实应用会话没有传感器阶段数据时显示不可用说明。无顶部返回；月亮回 Weekly。 |
| Weekly `53:378` | 作息目标次数、平均 bedtime、七天图表、平均睡眠；日期→Daily | 补齐原来缺少的两个目标计数、平均 bedtime 和时间段柱图。七日任意日期→DayDetail 独立 Scene。`TEST +` 幂等加入固定阶段样本；真实记录优先。 |
| DayDetail | 用户追加的独立历史日报 | 按所选日期读取，空状态明确；测试阶段分钟总和与时长一致；月亮→Weekly。 |
| AR `48:373`→`48:382` | 查找→放置；放置返回查找；查找返回Home | 修复原来放置后直接退出 AR 的路径。保存宠物的外观／姿势一致；测试背景明确标注；对话 `48:402` 使用离线演示。 |

## 统计的含义

- 七天按截至今天的日历日期组织，记录以开始时间的本地日期索引，周报与日期详情采用同一规则。
- 平均 bedtime 跨午夜计算，例如 23:50 与 00:10 平均为 00:00。
- 图表纵轴遵循设计的 23:00、00:00、08:00、09:00 四个刻度，午夜到 08:00 为压缩区间；柱表示记录的开始／结束时间，日期是按钮。
- 固定样本可计算“按目标入睡／起床”数量，并明确标注 TEST DATA。应用活动会话不能证明实际睡着／醒来，因此无样本时目标次数显示“—”；存在真实会话时平均项使用 session 文案。
- 用户可修改时间目标；当前周的样本达标计数按当前目标计算，尚未建立“每日历史目标版本”模型。

## 场景与可编辑性

Build Settings 仅包含 Home、Sleep、Me、Routine、Daily、Weekly、AR、DayDetail 八个 Scene。每个 Scene 一个主页面，弹层是所在页面的局部状态；没有将多个主页面堆进同一个 Scene。

UI 对象、点击绑定和资源引用序列化保存在 Scene，可以直接调整 RectTransform、颜色、字体及控件。圆盘是可配置的原生 uGUI Graphic；运行时更新弧线顶点，不创建页面。持久编辑菜单位于 Editor 文件夹，运行时不执行。跨场景数据使用 `SleepetSceneSession`，带自动场景切换计数与数据修订计数。

## 仍须真机或外部实现验收

- Health 已有调用接口和失败结果；尚无已验证的 Apple Health 外部 URL，不宣称跳转成功。iOS 左上角返回由系统提供。
- AR 为摄像头上的二维宠物叠加；真实平面识别和空间锚定未接入 ARKit。
- 对话为离线演示，未接云模型；系统后台闹钟、通知、HealthKit 睡眠数据未接入。
- 402×874 本地截图与交互回归不能替代 iPhone 的安全区域、系统键盘、触摸、权限和跨 App 返回验收，也不等同逐像素 1:1 已通过。

完整运行结果和截图索引见 `../HIGH_FI_IMPLEMENTATION.md` 的本地验证段落。
