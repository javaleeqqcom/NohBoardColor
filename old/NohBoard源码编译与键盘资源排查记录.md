# NohBoard 源码编译与键盘模型缺失问题排查记录

> - 归档状态：**废稿，仅供历史参考**
> - 文档版本：Draft 0.4（随 NohBoardColor v1.4.4 复核，仍为归档废稿）
> - 原始记录日期：2026-08-03
> - 最后标注日期：2026-08-11
> - 说明：本文包含早期环境排查和特效设想，可能与当前工程状态不一致，不作为现行构建或开发依据。彩色按钮和色相轮盘不再列入当前实施计划。

## 1. 目标

为后续 DIY NohBoard 按键渐变、余晖高亮、彩虹循环等特效，先完成以下基础工作：

- Fork 并克隆 NohBoard 项目
- 配置 C# / .NET 编译环境
- 成功编译生成 `NohBoard.exe`
- 解决程序启动后没有键盘模型、`Load Keyboard` 列表为空的问题
- 形成可复用的排查流程

---

## 2. 工程目录

仓库根目录：

```text
D:\Users\java_lee\Documents\GitHub\NohBoardColor
```

主要结构：

```text
NohBoardColor
├─ .github
├─ keyboards
├─ NohBoard
│  ├─ clipper_library
│  ├─ Hooking
│  ├─ NohBoard
│  │  ├─ bin
│  │  ├─ Controls
│  │  ├─ Extra
│  │  ├─ Forms
│  │  ├─ Keyboard
│  │  ├─ Legacy
│  │  ├─ NohBoard.csproj
│  │  └─ Program.cs
│  └─ NohBoard.sln
├─ NohBoard.json
├─ README.md
└─ LICENSE
```

注意：

- 解决方案文件位于：

```text
NohBoard\NohBoard.sln
```

- 主程序项目位于：

```text
NohBoard\NohBoard\NohBoard.csproj
```

- 键盘布局资源位于仓库根目录：

```text
keyboards
```

---

## 3. 当前编译环境

执行：

```powershell
dotnet --info
dotnet --list-sdks
```

当前环境：

```text
.NET SDK 6.0.427
Windows x64
```

项目目标框架：

```text
clipper_library：netstandard2.1
Hooking：netstandard2.1
NohBoard：netcoreapp3.1
UseWindowsForms：true
```

虽然主项目目标框架为 `.NET Core 3.1`，但本机的 `.NET 6 SDK` 可以成功还原并编译该项目，因此暂时不需要额外安装 `.NET Core 3.1 SDK`。

---

## 4. 查找解决方案和项目文件

在仓库根目录执行：

```powershell
Get-ChildItem .\NohBoard -Recurse -Filter *.sln
Get-ChildItem .\NohBoard -Recurse -Filter *.csproj
```

查看目标框架：

```powershell
Select-String `
  -Path .\NohBoard\*.csproj, .\NohBoard\**\*.csproj `
  -Pattern "TargetFramework|TargetFrameworkVersion|UseWindowsForms|UseWPF"
```

---

## 5. 还原与编译

执行：

```powershell
cd D:\Users\java_lee\Documents\GitHub\NohBoardColor

dotnet restore .\NohBoard\NohBoard.sln
dotnet build .\NohBoard\NohBoard.sln -c Debug
```

成功输出：

```text
已成功生成。
0 个警告
0 个错误
```

生成位置：

```text
NohBoard\NohBoard\bin\Debug\netcoreapp3.1\win-x64
```

其中包括：

```text
NohBoard.exe
NohBoard.dll
NohBoard.Hooking.dll
clipper_library.dll
```

---

## 6. 问题现象：程序启动后没有键盘模型

直接启动：

```powershell
.\NohBoard\NohBoard\bin\Debug\netcoreapp3.1\win-x64\NohBoard.exe
```

程序可以正常运行，但出现以下问题：

- 主窗口只有纯色背景
- 没有任何键盘模型
- 打开 `Load Keyboard` 后，分类列表为空
- 无法选择键盘定义和键盘样式

这说明：

> 程序本体编译成功，但运行时没有读取到键盘资源。

---

## 7. 根因定位

检查仓库根目录和输出目录中的键盘资源数量：

```powershell
$src = ".\keyboards"
$dst = ".\NohBoard\NohBoard\bin\Debug\netcoreapp3.1\win-x64\keyboards"

"源码 keyboards 文件数："
(Get-ChildItem $src -Recurse -File).Count

"输出 keyboards 文件数："
(Get-ChildItem $dst -Recurse -File).Count
```

实际结果：

```text
源码 keyboards 文件数：209
输出 keyboards 文件数：0
```

说明：

- 输出目录中虽然存在 `keyboards` 文件夹
- 但该文件夹是空的
- `dotnet build` 只编译了程序，没有自动复制键盘资源
- 因此 NohBoard 无法加载任何键盘布局

---

## 8. 临时修复方法

先关闭正在运行的 NohBoard。

在仓库根目录执行：

```powershell
cd D:\Users\java_lee\Documents\GitHub\NohBoardColor

$src = ".\keyboards"
$dst = ".\NohBoard\NohBoard\bin\Debug\netcoreapp3.1\win-x64\keyboards"

Remove-Item $dst -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item $src $dst -Recurse -Force
```

再次检查：

```powershell
(Get-ChildItem $dst -Recurse -File).Count
```

正确结果：

```text
209
```

检查目录结构：

```powershell
Get-ChildItem $dst -Recurse |
    Select-Object -First 30 FullName
```

正确结构应类似：

```text
win-x64
├─ NohBoard.exe
└─ keyboards
   ├─ BurningFish
   ├─ GamesLegacy
   ├─ global
   ├─ HaleyHalcyon
   ├─ joao7yt
   ├─ Normal
   ├─ quake
   ├─ TheCore
   └─ wheels
```

不能出现：

```text
keyboards
└─ keyboards
   └─ Normal
```

---

## 9. 正确启动方式

为了避免旧项目按当前工作目录查找资源，建议先切换到 EXE 所在目录再启动：

```powershell
cd .\NohBoard\NohBoard\bin\Debug\netcoreapp3.1\win-x64
.\NohBoard.exe
```

修复后：

- 键盘模型正常显示
- `Load Keyboard` 中可以看到分类
- 按键可以正常高亮
- 编译环境和运行环境均已验证成功

---

## 10. 永久修复建议

为了避免每次重新编译后都要手动复制 `keyboards`，应修改：

```text
NohBoard\NohBoard\NohBoard.csproj
```

在 `</Project>` 前加入：

```xml
<ItemGroup>
  <Content Include="..\..\keyboards\**\*">
    <Link>keyboards\%(RecursiveDir)%(Filename)%(Extension)</Link>
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

然后执行：

```powershell
cd D:\Users\java_lee\Documents\GitHub\NohBoardColor

dotnet clean .\NohBoard\NohBoard.sln
dotnet build .\NohBoard\NohBoard.sln -c Debug
```

验证：

```powershell
(Get-ChildItem `
  .\NohBoard\NohBoard\bin\Debug\netcoreapp3.1\win-x64\keyboards `
  -Recurse -File).Count
```

只要输出不再为 `0`，说明以后每次编译都会自动复制键盘资源。

---

## 11. 配置文件注意事项

仓库根目录还存在：

```text
NohBoard.json
```

如果后续出现以下问题：

- 每次启动都忘记上次选择的键盘
- 窗口位置、大小、样式不能保留
- 相关设置无法持久化

可以把配置文件复制到 EXE 旁边：

```powershell
Copy-Item `
  .\NohBoard.json `
  .\NohBoard\NohBoard\bin\Debug\netcoreapp3.1\win-x64\NohBoard.json `
  -Force
```

后续也可以考虑把该文件一并加入 `.csproj` 的自动复制配置。

---

## 12. 后续 DIY 渐变按钮特效的准备

目前基础环境已经完成：

- C# 项目可正常编译
- 原版程序可正常运行
- 键盘布局可正常加载
- 按键高亮功能正常
- 可以开始修改按键绘制逻辑

下一阶段建议按以下顺序进行：

### 12.1 创建功能分支

```powershell
git switch -c feature-key-fade
```

### 12.2 搜索按键状态代码

```powershell
Get-ChildItem .\NohBoard -Recurse -Filter *.cs |
    Select-String -Pattern "KeyDown|KeyUp|Pressed|IsPressed"
```

### 12.3 搜索绘制代码

```powershell
Get-ChildItem .\NohBoard -Recurse -Filter *.cs |
    Select-String -Pattern "Draw|Paint|FillRectangle|FillPath|Brush|Color"
```

### 12.4 预期实现结构

```text
按键按下
→ 记录当前按键状态
→ 记录按下或松开时间
→ 松开后不立即恢复普通颜色
→ 根据经过时间计算高亮强度
→ 每一帧重绘
→ 高亮逐渐变暗
→ 到达设定时间后恢复普通颜色
```

### 12.5 建议优先实现

第一版先只做：

```text
按下时全亮
→ 松开后保持 200 ms
→ 之后在 800 ms 内逐渐变暗
→ 最终恢复普通颜色
```

确认稳定后，再增加：

- 可调整余晖时长
- 线性渐变
- 缓入缓出渐变
- 赤橙黄绿青蓝紫循环
- 字母键、数字键、功能键使用不同颜色
- 在设置窗口中增加开关和参数

---

## 13. 本次经验总结

本次问题不是：

- C# 编译失败
- .NET SDK 不兼容
- WinForms 环境缺失
- 程序源码错误
- 键盘布局文件损坏

真正原因是：

> `dotnet build` 成功生成了程序，但没有把仓库根目录中的 `keyboards` 资源复制到 EXE 输出目录。

排查此类问题时，不应只确认目录是否存在，还必须检查目录内实际文件数量：

```powershell
(Get-ChildItem <目录> -Recurse -File).Count
```

本次关键对比：

```text
源码目录：209 个文件
输出目录：0 个文件
```

完成复制后：

```text
输出目录：209 个文件
```

程序随即恢复正常。

---

## 14. 一键修复命令

在仓库根目录执行：

```powershell
$src = ".\keyboards"
$dst = ".\NohBoard\NohBoard\bin\Debug\netcoreapp3.1\win-x64\keyboards"

Remove-Item $dst -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item $src $dst -Recurse -Force

cd .\NohBoard\NohBoard\bin\Debug\netcoreapp3.1\win-x64
.\NohBoard.exe
```
