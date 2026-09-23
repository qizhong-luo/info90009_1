# P0 验证记录

日期：2026-09-05。环境：Windows，Unity 6000.3.21f1，项目原有 2D URP 模板。

## 已执行

- Unity 自带 Roslyn 编译器：P0 C# 代码检查通过。
- Unity Editor 批处理：项目编译及 `SleepetP0` 场景、prefab、配置创建通过。
- Unity Play Mode：**9 / 9 通过，0 失败**。机器可读结果为 `playmode-results.xml`；本轮测试耗时约 12 秒，不包含 Editor 启动和编译。
- 已查看真实 uGUI 渲染的 7 张 480 × 900 截图，确认首页、聊天、睡眠设置、可能睡着、两种晨间结果和调试控件在该尺寸下可读、无控件遮挡。
- Windows x64 Development Build 构建成功，输出 `Builds/P0/SleepetP0.exe`。沿用模板已有包，没有为本次验证进行包裁剪或发布优化。
- Windows 程序批处理启动检查通过：场景成功初始化，在程序旁写出包含 `APP_STARTED` 的独立 JSON；未出现 C# 异常。检查后已结束测试进程。独立程序没有进行完整的人工交互回归，完整流程覆盖来自上面的 Play Mode 测试。

## 测试覆盖

| 测试 | 验证内容 |
|---|---|
| CompleteJourney_ChatHoldResultAndSavedLog | 从放置、宠物点击、聊天、睡眠到长按结束及结果；短按不能结束；JSON 事件顺序、UTC 时间、会话 ID、隐去聊天原文、新会话独立性 |
| Media_AllThreeBehavioursAndWakeRecovery | 停止、渐弱中点音量、渐弱结束、持续播放、交互取消渐弱、主动恢复停止的音频 |
| CompanionModes_QuietAndOnlyOneGentlePrompt | Quiet 无提示，Occasional / Proactive 各自最多提示一次，进入休息动画 |
| RealFrameTime_TransitionsAndControlActivityReset | 使用 0.3 + 0.3 秒测试配置，通过真实帧 Update 自然转移；控件操作使状态和计时重置 |
| DebugControls_ReachStatesAndBothResults | 调试 UI 可达三种睡眠状态及两种晨间结果 |
| Chat_EmptyDuplicateCloseAndFailureHandling | 空输入不发送、防重复、加载期间关闭后保留回复、接口故障退回 Mock |
| Hold_CancelsOnPointerExit | 长按中移出按钮取消结束操作 |
| Detector_ThresholdBoundariesResetAndEndedImmunity | 默认 30 + 60 秒阈值边界、交互重置、结束后计时不再转移 |
| MockAI_CommonInputsAreShortAndSupportive | 常见中英文输入获得简短、无惩罚的预置回复 |

UI 测试先等待新控件进入渲染帧，再通过 EventSystem 射线检查控件是否可命中，随后分发点击或指针事件。首轮同一帧连续点击新生成控件导致测试失败，调整测试帧序后全量通过。

## 检查材料

- `01-home.png` 至 `07-test-controls.png`：Unity 内实际运行的 UI 渲染。
- `example-session.json`：完整流程测试实际写出的会话记录，包括难眠结果的调试切换。
- `playmode-results.xml`：Unity Test Framework 原始结果。
- `prepare.log`、`playmode.log`、`build.log`：本地执行日志，不纳入版本控制。

## 验证边界

这是 P0 编辑器原型的功能验证，不是用户研究结论。尚未验证手机触控、移动端软键盘、真实 AR、真实 AI、整夜后台运行或真实睡眠识别；这些均不属于本次 P0 范围。

音频已通过 AudioSource 播放状态、音量与停止状态断言；没有进行真人听感评估。使用的测试音不能代表最终睡眠环境音的体验。

Play Mode 输入验证主要使用 UI 事件分发和文本赋值，不等同于人工逐键输入或触屏设备测试。
