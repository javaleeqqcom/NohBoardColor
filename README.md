# NohBoardColor

> 当前版本：**v1.4.0** · 更新日期：**2026-08-09** · 基于 [ThoNohT/NohBoard](https://github.com/ThoNohT/NohBoard) 的教学用途分支

NohBoardColor 是面向儿童编程和键位学习场景的 NohBoard 分支。当前重点是让刚刚操作过的按键更容易被找到，同时尽量保持界面简单、顺序清晰。

## v1.4.0：按键渐暗

本版本在 `Settings` → `Input` 中增加了 `Fade keypresses`：

- `Show keypresses for at least` 必须大于 `0 ms`，否则渐暗选项会禁用。
- 按住键盘键或鼠标键时保持完整高亮。
- 松开后在设定时间内逐渐淡回普通状态，不再到时间后突然熄灭。
- 未勾选时保留 NohBoard 原有行为。
- 设置会随其他全局设置一同保存。

## 项目方向

本项目暂不开发彩色按钮或色相轮盘。实际讨论认为，小朋友难以仅凭颜色轮换准确判断按键先后顺序；简单渐暗更直观，也更适合当前教学目标。此前的彩色方案保存在 [`old`](old) 目录中，仅供历史参考。

下一阶段计划研究窗口置顶、半透明键盘、激活按键恢复不透明，以及键盘窗口与编程 IDE 的位置切换。所有内容须先完成人工评审再实施，详见 [后续改进计划](ROADMAP.md)。

## 上游项目简介

NohBoard is a keyboard visualization program. I know certain applications already exist that do just this, display your keyboard on-screen. And even more probably. However, so far I have found none that were both free and easy to use. That's where this program came in, I made it to be free and easy to use, without any fancy graphics, and easily capturable (possibly with chroma key). Furthermore, it's very customizable.

## Rewrite

An initial version was made in C++, this originated from the desire to make something with graphics, and what I knew was [OBS](http://github.com/jp9000/OBS), now replaced by [OBS Studio](http://github.com/jp9000/obs-studio). That's why I started in the same spirit, using C++, and rendering with DirectX. However, having spent most of my time on C# during at least the last decade or so, I decided that I would be much more able to create awesome things in this language. That's when I re-started. Rather than using DirectX, I switched to GDI+, as we're Windows only (I'm sorry, but I just really don't use any other OS, and so far it is still the go-to OS for gaming). No really fancy graphics are required, no 3D is required. This also makes it easier to capture, as a simple window capture in OBS will do the trick now, rather than having to fiddle with game capture which might not work due to a game typically being run at the same time as NohBoard.

## Contributors

**Maintainer / original author**
- Eric "ThoNohT" Bataille (e.c.p.bataille@gmail.com) - Original author

**Contributors**
- Marius "Buttercak3" Becker - Various bugfixes
- Ivan "YaLTeR" Molodetskikh - Added the scroll counter *(NohBoard classic)*
- Michal Mitter - Added button outline *(NohBoard classic)*

**Keyboard layouts**
- BaronBargy
- Burning Fish
- Cloudwolf
- Daigtas
- Floatingthru
- HAJohnny
- Helixia
- joao7yt
- kernel1337
- Krazy
- layarion
- MCCrafterTV
- MtB1980
- TicTacFoe
- ToxicMirror
- WayZHC
- wingsltd
- zolia
- SirDifferential
- flyingmongoose
- JapanYoshi
- dchitra

If you want to contribute, either with code, with keyboard definitions or keyboard styles, feel free to fork this repository and provide your changes via a pull request, or other means of submitting your changes back to me.

## Changelog

See this fork's [CHANGELOG](CHANGELOG.md) for NohBoardColor updates. For changes inherited from the original project, see the upstream [Releases](https://github.com/ThoNohT/NohBoard/releases) page.

## Full Documentation

See the [Wiki](https://github.com/ThoNohT/NohBoard/wiki) for full documentation.

## Donations

Donations are neither required nor requested. They are, however, always appreciated, and due to some demand, there now is the possibility to [donate](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=FFB9XFRWE5EK2).
Note that donations are to be made purely for appreciation of performed work, and not as a means of prioritizing or requesting future work. They will not in any way impact the speed or order in which features are implemented.

## License

NohBoard is licensed under the GPL version 2. The license agreement is attached in this repository and can be found [here](https://github.com/ThoNohT/NohBoard/blob/master/LICENSE).
