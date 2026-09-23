# Sleepet Unity P0 验证原型

> 上一版运行说明归档。当前静态界面、Home / Sleep / Me 与电脑摄像头版本请阅读项目根目录 `README.md`。

仅实现 `Sleepet_Unity_Validation_Demo_v2.md` 的 P0，基于 Unity 6000.3.21f1 的原有 2D 模板。仅修改本 Unity 项目文件夹内的内容。

## 运行

1. 使用 Unity 6000.3.21f1 打开当前项目。
2. 打开 `Assets/Sleepet/Scenes/SleepetP0.unity`，点击 Play。
3. 在 Home 点击 **Place Mocha (fallback)**，点击宠物，再点击 **Talk with Mocha**，输入并发送一句话。
4. 进入 **Sleep Mode**，选择音频行为和陪伴模式，点击 **Start Sleep**。
5. 停止输入 30 秒进入 Low Attention；再等待 60 秒进入 Likely Asleep。鼠标移动不算交互，点击、触摸或按键会重置计时。
6. 长按 **Hold to end sleep** 1.5 秒查看晨间反馈。松开、移出控件或失去焦点会取消长按。
7. **Start another test session** 开始独立的新记录。

界面采用英文以对应低保真原型；Mock 可识别常见中英文输入。固定 480 × 900 逻辑画布按窗口比例缩放，适合竖屏测试。无需联网、账号、下载素材或配置 AI 密钥。

已生成 Windows 本地测试程序：`Builds/P0/SleepetP0.exe`。需保留同目录下的 Data、DLL 等文件；也可使用 `-screen-fullscreen 0 -screen-width 480 -screen-height 900` 参数以竖屏窗口启动。

## 快速演示

右下角 **Test controls** 打开默认隐藏的调试面板：

- 开始睡眠后，可直接设为 Awake、Low Attention、Likely Asleep。
- **Set calm night result / Set difficult night result** 指定模拟结果；结束后也可切换两种反馈供对比。
- 所有强制状态和结果切换均记录为调试事件，避免与自然无操作转移混淆。

`Assets/Sleepet/Config/P0Config.asset` 可在 Inspector 调整：

| 配置 | 默认值 | 含义 |
|---|---:|---|
| lowAttentionDelay | 30 秒 | 从最后一次输入到低注意力 |
| likelyAsleepDelay | 60 秒 | 进入低注意力后的额外等待时间 |
| fadeDuration | 8 秒 | 音频渐弱时长 |
| mockReplyDelay | 0.4 秒 | 用于显示 Mock 加载状态 |
| endHoldDuration | 1.5 秒 | 主动结束所需长按时间 |

修改配置后重新进入 Play Mode 生效。以上时间仅用于加速演示，不代表真实入睡判断。

## 已实现的交互

- 一个 Mocha 占位 prefab：呼吸缩放待机、点击正向反应、闭眼休息。
- 固定 2D 放置点。日志标明 fallback，不伪造 AR 扫描或平面检测。
- Mock 聊天：单行输入、发送、加载、最近 4 条消息、关闭；通过 `ICompanionAI` 分离回复与 UI。没有真实 API 调用。
- 三种音频行为：停止、8 秒渐弱、持续播放。仅控制本 Unity 程序的低音量测试音。
- 三种陪伴模式：Quiet 无提示；Occasional 在低注意力时出现一次安静陪伴提示；Proactive 出现一次轻柔呼吸选择。每种提示每会话最多一次。
- 用户操作回到 Awake；正在渐弱的音频恢复原音量；已经停止的音频需点击 **I'm awake / resume audio** 主动恢复。
- 两种晨间反馈均无惩罚。平静结果使用一次现有正向反应；困难结果只改变文字，不损害宠物。
- 每个事件即时保存 JSON；测试或程序中断前已写入的事件仍可检查。

## 日志

Editor：项目根目录 `SessionLogs/sleepet-<sessionId>.json`。

Windows 构建：可执行文件旁的 `SessionLogs` 目录。

每条事件包含 UTC 时间、sessionId、eventType、value、context。记录放置、点击、聊天操作、设置、睡眠状态、音频变化和结果；聊天仅记录字符数，不保存用户原文。结果页显示本次时长和有效设置，不显示虚构的睡眠分期或睡眠质量分数。

若日志无法写入，页脚显示失败状态；记录仍保留在内存中供后续保存尝试。调试会话和人工测试会话使用相同格式，可通过 DEBUG 事件区分。

## PDF 对应范围

| PDF 页 | 本次抽象实现 |
|---|---|
| 第 1 页 Sleep | 时间、宠物休息、三种音频行为、三种陪伴模式、长按结束 |
| 第 2 页 Home / AR / AI | Home、固定点放置替代 AR、宠物点击、短文本 Mock 聊天、无惩罚反馈 |
| 第 3 页 Me / Information | 陪伴偏好并入睡眠设置；结果页显示当前会话信息 |

未加入闹钟、社交、个人资料、穿戴设备、周统计、睡眠分期、商店或养宠负担。AR 为 P1；真实 AI、语音和传感器为 P2。

## 代码与验证

- `Assets/Sleepet/Runtime`：P0 业务与基础 uGUI。
- `Assets/Sleepet/Editor/P0ProjectSetup.cs`：创建初始场景/prefab，或构建 Windows 版本；已有场景和 prefab 不会被该菜单覆盖。
- `Assets/Sleepet/Tests/PlayMode/P0Tests.cs`：Unity Play Mode 测试，使用 UI 射线检测和事件分发检查可点击性。
- Unity 菜单 **Window > General > Test Runner > PlayMode**，运行 `Sleepet.PlayModeTests`。
- 批处理：关闭此项目的 Editor 后，在项目目录运行 `powershell -ExecutionPolicy Bypass -File Validation/Run-P0Tests.ps1`。
- `Validation/TEST_REPORT.md` 记录实际执行结果与验证边界。

原有 `SampleScene.unity` 保留。`SleepetP0` 已列为首个构建场景。可通过 **Sleepet > Build Windows P0** 创建只包含 P0 场景的本地 Windows 构建。
