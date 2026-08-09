# 更新说明

本项目从 `v1.3.0` 起继承上游 NohBoard，并使用语义化版本号记录此分支的变化。

## v1.4.0 - 2026-08-09

### 新增

- 在 `Settings` → `Input` 增加 `Fade keypresses` 复选框。
- 键盘键和鼠标键松开后，可在 `Show keypresses for at least` 设置的时长内线性淡回普通状态。
- 支持纯色按键样式和带背景图片的按键样式。
- 新增 `FadeKeyPresses` 全局配置并持久化保存；旧配置文件无需迁移，默认保持关闭。

### 行为与兼容性

- 渐暗仅在保持时间大于 `0 ms` 时可用。
- 按键按住期间保持完整高亮，松开后才开始渐暗。
- 关闭渐暗时，保持上游版本到达保持时间后立即取消高亮的行为。
- 组合键、Caps Lock/Shift 显示逻辑以及原有键盘布局格式保持兼容。

### 项目决策

- 暂不实现彩色按钮、彩虹循环或色相轮盘。
- 原因是颜色轮换不利于儿童准确分辨按键先后顺序；当前采用更简单、稳定的渐暗反馈。
- 相关早期设想已移入 `old` 目录，并明确标记为废稿。

### 验证

- 已完成简单人工功能测试。
- `dotnet build NohBoard\NohBoard.sln --configuration Debug --no-restore`：0 个警告，0 个错误。

## 上游更新

此前版本的更新记录请参阅 [ThoNohT/NohBoard Releases](https://github.com/ThoNohT/NohBoard/releases)。
