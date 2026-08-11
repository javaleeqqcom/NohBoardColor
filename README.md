# NohBoardColor

> 当前版本：**v1.4.4** · 更新日期：**2026-08-11** · 基于 [ThoNohT/NohBoard](https://github.com/ThoNohT/NohBoard) 的教学用途分支

NohBoardColor 是面向儿童编程和键位学习场景的 NohBoard 分支。当前重点是让刚刚操作过的按键更容易被找到，同时尽量保持界面简单、顺序清晰。

## v1.4.4：窗口辅助与教学键帽显示

- 鼠标移入主窗口后，右上角显示图钉按钮，可直接切换窗口置顶。
- 右键菜单也可切换置顶；全局快捷键为 `Ctrl+Alt+T`。
- `Ctrl+Alt+O` 可快速开关空闲透明与标题栏自动隐藏。
- `Settings` → `General` 可将键位文字放大到 100%–300%，默认使用更适合儿童辨认的 130%。
- `Settings` → `Dual-state key labels` 可选择实体键帽式双字符显示，或兼容 NohBoard 原版的居中单字符显示；默认采用实体键帽模式。
- `Settings` → `Inactive Window Appearance` 可启用空闲透明，并分别设置键盘背景与键帽的 20%–100% 透明度；默认值分别为 40% 与 75%。
- 鼠标离开窗口后变为设定透明度并隐藏标题栏；鼠标移回后恢复完整显示。
- 键位文字始终保持 100% 不透明；高亮渐暗只作用于键帽高亮，不再让文字随之消失。
- 按键激活时可选择仅让被按下的键恢复不透明，或让全部键帽临时恢复不透明。
- 透明状态下的按下键固定使用白色键帽与黑色文字；根据背景自动选择高对比颜色留待后续设计。
- 实体键帽模式下，空闲的非字母双状态键同时显示左下普通字符和右上 S 字符；键被按下后只显示按下瞬间对应的一个字符，但位置不变。A–Z 字母始终居中。
- 原版模式始终只显示一个居中字符，并随 Shift 切换普通/S 状态。
- 编辑键盘布局和打开菜单/对话框时保持完整不透明和标题栏，避免影响操作。

这些功能已编入 v1.4.4。Windows x64 绿色版无需管理员权限或额外安装 .NET，完整解压后运行 `NohBoard.exe` 即可；详见 [`PORTABLE_README.txt`](PORTABLE_README.txt)。

## 绿色版使用

- 必须完整解压后运行，不要直接在压缩包内启动。
- `NohBoard.exe` 与 `keyboards` 文件夹需要保持在同一目录。
- 设置保存在程序旁的 `NohBoard.json`；删除该文件可恢复默认设置。
- 绿色包是 Windows x64 自包含版本，不需要管理员权限或额外安装 .NET Runtime。

### 本轮修复方案

- `Settings` 的外观分组已正式纳入窗体 Designer，与原有控件一起进行字体和 DPI 自动缩放，避免新增区域覆盖原分组或底部按钮被裁剪。
- 空闲显示改为三层绘制：主窗绘制键盘背景、半透明覆盖窗绘制键帽、最上层覆盖窗绘制 100% 不透明文字；各层均不拦截鼠标或 IDE 输入。
- 高亮渐暗内部也拆分为键帽与文字绘制，渐暗系数仅应用于键帽背景和边框，文字全程保持清晰。
- 显示/隐藏标题栏时保持键盘客户区的屏幕坐标不变，不再因窗口边框尺寸变化而向左上跳动。
- 透明文字层固定使用黑色单色像素，避免文字抗锯齿与洋红透明键混色；自动高对比文字颜色留待后续评审。
- 透明键帽先绘制完整普通底色，再叠加白色高亮，避免渐暗像素与洋红透明键混色；按下状态统一为白底黑字。
- 新增全局键位字体倍率。倍率只改变文字和 S 状态角标，不改变键帽尺寸与键盘布局。
- Shift 使用物理按下状态而非保持时间：Shift 松开后以 0 ms 退出，其间新按下的键独立锁存 S 状态。
- 实体键帽模式在空闲时以右上 S 字符和左下较小普通字符同时表达键帽含义；激活后只保留实际输入状态的字符，且沿用其原位置和字号。
- 每次按键会记录按下瞬间是否激活 Shift，保持/渐暗期间不再因后来按下或松开 Shift 而改变字符。
- 原版模式保留单一居中文字，并兼容此前的 Shift 状态逻辑；普通窗口和透明窗口使用相同排版规则。
- 最小化期间暂停透明层和窗口边框切换，保留 Windows 原生最小化句柄，因此可从任务栏正常恢复。
- 旧开发版的单一 `InactiveOpacityPercent` 配置继续作为背景透明度读取；新增键帽透明度无需迁移旧配置。

后续可在现有分层结构上增加真正的 Alpha 通道主题，使 Alpha 不透明度遵循“文字 > 键帽 > 键盘背景”，并允许背景使用适合儿童的卡通图案；该主题设计仍需人工评审后再实现。

## v1.4.0：按键渐暗

本版本在 `Settings` → `Input` 中增加了 `Fade keypresses`：

- `Show keypresses for at least` 必须大于 `0 ms`，否则渐暗选项会禁用。
- 按住键盘键或鼠标键时保持完整高亮。
- 松开后在设定时间内逐渐淡回普通状态，不再到时间后突然熄灭。
- 未勾选时保留 NohBoard 原有行为。
- 设置会随其他全局设置一同保存。

## 项目方向

本项目暂不开发彩色按钮或色相轮盘。实际讨论认为，小朋友难以仅凭颜色轮换准确判断按键先后顺序；简单渐暗更直观，也更适合当前教学目标。此前的彩色方案保存在 [`old`](old) 目录中，仅供历史参考。

窗口置顶、半透明键盘和激活按键不透明已进入人工验收；IDE 与键盘位置切换仍处于规划阶段，详见 [后续改进计划](ROADMAP.md)。

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
