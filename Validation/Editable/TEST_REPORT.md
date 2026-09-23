# 可编辑版验证记录

日期：2026-09-05。Unity 6000.3.21f1 / Windows。

## 实际结果

**Play Mode：13 / 13 通过；0 失败；0 Inconclusive。** 本轮测试执行约 9.47 秒，不含 Editor 启动和编译。

原始结果：`../editable-results.xml`；执行日志：`../editable-playmode.log`。

“View my records”直接定位到 Me 记录区的最后一项调整，已额外运行完整会话定向回归：**1 / 1 通过**，结果为 `../editable-journey-results.xml`。

真实电脑摄像头检查：**Live，连续收到 3 帧**，随后关闭并释放设备。仅保存状态与帧计数在 `camera-hardware.txt`，未保存或录制摄像头图像。

最终 Windows x64 构建成功：`Builds/Editable/Sleepet.exe`。批处理启动检查已加载场景、写出 `APP_STARTED` JSON，未发现 C# 异常；检查后已结束进程。独立程序执行了启动检查，完整交互回归及真实摄像头检查在 Unity Play Mode 中完成。日志为 `../editable-build.log`、`../editable-player.log`。

## 需求与证据

| 需求 | 验证方式与结果 |
|---|---|
| 适度美化 | 已查看运行截图；奶油色/绿色 Home、深色 Sleep、卡片式 Me，文字与按钮可读，弹窗正常遮挡背景 |
| 静态可编辑资产 | prefab 在 Play 前已包含三页、弹窗、控件、宠物、AudioClip 引用；关键按钮持久化事件存在 |
| 后续微调可保留 | 测试修改宠物 RectTransform 后切换三页，位置与原始对象实例均保留；未销毁或重建页面 |
| Home / Sleep / Me | 实际点击导航；切页不会结束睡眠会话；底部不再有 Result 页签 |
| 大量睡眠记录 | 23 条本地记录跨 6 页验证；另有 20 条独立 SAMPLE 记录跨 5 页；翻页边界和来源切换通过 |
| 设置持久化 | 保存后从磁盘重建 Store，核对提醒时间、开关、声音、音量、睡眠行为与陪伴模式 |
| 提醒时间 | 非法时间拒绝保存；按日去重、重启后去重、下一日提醒和提醒弹窗通过 |
| 声音 | 实际导入 Rain/Ocean AudioClip，试听、离页停止、渐弱中点、取消渐弱、停止、继续、Silence 均通过 |
| Me 控件可操作 | 真实 UI 射线命中 Dropdown 后选择 Ocean，再保存；鼠标滚轮事件使 Me 内容滚动 |
| 摄像头取流 | 本机 WebCamTexture 实际收到画面帧；不是把测试背景当作硬件成功 |
| 放置和拖动 | 注入测试画面验证放置，拖动到极端坐标时宠物四角仍留在画面范围内，关闭释放源 |
| 摄像头失败处理 | 无设备、无帧超时、明确的 Test background 备用路径通过 |
| 原 P0 回归 | 宠物点击、Mock 回复及接口失败回退、无操作状态机、长按与短按/移出取消、两种温和反馈、日志及历史保存通过 |

## 运行截图

- `01-home.png`：Home。
- `02-chat.png`：聊天弹窗。
- `03-me-settings.png`：Me 的设置。
- `04-sleep-ready.png`、`05-likely-asleep.png`：睡眠准备与模拟入睡。
- `06-calm-result.png`、`07-difficult-result.png`：两种非惩罚性结果。
- `08-real-history.png`、`10-sample-history.png`：真实演示会话与独立示例记录。
- `09-reminder.png`：提醒弹窗。
- `11-camera-test-background.png`：明确标记的无摄像头测试背景，非真实摄像头截图。
- `example-session.json`：完整流程实际写出的日志样例。

## 本次修正并复测

- 不再在 Play 时创建 UI，改用编辑器保存的 prefab 与持久化事件。
- Me 增加可见滚动条和合理的滚轮速度。
- 拖动坐标从父物体左上角原点换算为宠物中心锚点，防止拖出画面。
- 摄像头初次收到图像时立即显示并计数，避免首帧已经到达但计数仍为 0。
- 结果弹窗中的 View my records 会自动滚动到 Me 的历史记录区域。
- 静态结构测试保留对所有已排布对象实例的检查，仅允许 Unity InputField 自身懒创建的光标对象。

## 范围

电脑摄像头提供画面叠加放置，**不包含真实平面追踪或空间锚定**。本轮按要求不使用手机 AR 插件。

提醒仅在程序打开时触发，**不包含关闭程序后的系统通知**。睡眠记录是演示会话；不声称测得真实睡眠时长或睡眠质量。

音频行为经过播放状态和音量断言，但不代表已做长期睡眠听感研究。测试结果是功能验证，不是用户研究结论。
