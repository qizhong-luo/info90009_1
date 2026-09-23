# 当前版本资产与依赖来源

没有调用 ImageGen、Unity AI 资产生成或付费 AI 接口。界面使用可编辑 uGUI；Mocha 使用下载的免费狗狗 2D 动画；声音使用以下免费授权文件。

| Unity 资产 | 作者与原始来源 | 许可证 | 处理 |
|---|---|---|---|
| `Assets/Sleepet/Audio/Rain.mp3` | Ogrebane，[Rain in the Gutter Loop](https://opengameart.org/content/rain-gutter-loop) | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | 原始 MP3，无生成式处理 |
| `Assets/Sleepet/Audio/Ocean.wav` | jasinski，qubodup 整理发布，[Beach Ocean Waves](https://opengameart.org/content/beach-ocean-waves) | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | 第一个 FLAC 文件转换为 44.1 kHz PCM16 WAV，4 秒片段循环 |
| `Assets/Sleepet/Art/Mocha/*.png`、`MochaUI.prefab` | LuizMelo，[Pet Dogs Pack](https://luizmelo.itch.io/pet-dogs-pack) / Golden Retriever | CC0，原许可证见同目录 License.txt | 原始 PNG 未改图；Unity 统一切片，Point 过滤；11 组动作，脚本连接点击、长按、休息、拖动。作者声明未使用生成式 AI |
| UI 卡片、按钮、控件 | Unity uGUI 内置皮肤与 LegacyRuntime.ttf | Unity 自带资源 | 静态 prefab 内的颜色、字体、圆角与布局 |
| `SampleHistory.json` | 本地测试数据 | 项目测试夹具 | 20 条明确标记 SAMPLE 的模拟记录，不是用户睡眠数据 |

来源核实与下载日期：2026-09-05。音频页面明确列出 CC0。原始下载地址：

- [Rain MP3](https://opengameart.org/sites/default/files/rain-gutter-loop_0.mp3)
- [Ocean FLAC](https://opengameart.org/sites/default/files/wave_01_cc0-18363__jasinski__alkaibeach.flac)

转换工具 SoundFile 仅安装在 `Validation/AudioTools`，不随游戏打包。替换声音时，将新的 AudioClip 拖入主 prefab 的 `SleepMediaController` 对应字段即可。

摄像头实现参考 Unity 官方 [WebCamTexture 文档](https://docs.unity.cn/ScriptReference/WebCamTexture.html)，处理帧到达、图像旋转和镜像；没有使用第三方 AR 资产或追踪插件。

