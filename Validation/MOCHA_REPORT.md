# Mocha 免费 2D 狗狗替换验证

日期：2026-09-05 / Unity 6000.3.21f1。

- 素材：LuizMelo / Pet Dogs Pack / Golden Retriever，作者提供的零元下载，CC0；作者页面声明 No generative AI was used。
- 原始 ZIP、许可证、其他犬种已存入 SourceAssets/PetDogs；Unity 使用 Assets/Sleepet/Art/Mocha 的 11 张原始 PNG。
- 导入成功：统一帧裁切 (28,39,45,23)，逐帧保留共同脚底基准；未对 PNG 做生成或改图。
- MochaUI prefab 已替换，保留现有四处实例、按钮事件和页面结构。
- PlayMode：14 / 14 Passed，0 Failed，0 Inconclusive。原始结果见 mocha-results.xml，日志见 mocha-tests.log。
- 新回归验证实际待机换帧、按钮射线点击反馈、按住 0.35 秒后舔舐及松手不覆盖、趴下/睡眠/唤醒伸展、拖动步行动画及结束反馈。
- 既有导航、睡眠完整流程、偏好保存、历史分页、摄像头和拖动边界回归全部通过。
- 已人工查看 Unity 渲染截图：Editable/01-home.png、Editable/14-mocha-rest.png；另有 12-mocha-tap.png、13-mocha-stroke.png。

触摸逻辑由 Unity PointerEventData 共用鼠标/触屏输入；本轮通过事件系统模拟测试，没有在实体手机上验证。叫声只播放动作，不添加音效；睡眠原素材单帧，呼吸由缩放实现。

Windows 演示构建成功：Builds/Editable/Sleepet.exe。构建日志 mocha-build.log 明确记录 Build Finished, Result: Success。
