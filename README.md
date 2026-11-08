# NotepadPro

一个为 Windows 打造的轻量、干净、无广告的源代码编辑器。多标签编辑、语法高亮、
查找替换、编码与行尾转换——全部核心功能，本地运行，不联网。

**全部代码为原创实现**。编辑内核基于开源的
[Scintilla](https://www.scintilla.org/) 编辑组件（通过 MIT 协议的
Scintilla5.NET 封装）。

![技术栈](https://img.shields.io/badge/.NET-8.0%20WinForms-blue) ![协议](https://img.shields.io/badge/license-MIT-green)

## 功能

- **多标签编辑**：未保存修改标记、标签右键菜单（关闭/关闭其他/复制路径/在资源
  管理器中显示）、Ctrl+Tab 切换、滚轮切换、中键关闭、拖放与命令行打开
- **语法高亮**：22 种语言 —— C/C++、C#、Java、Go、Rust、JavaScript、TypeScript、
  Python、Ruby、Lua、PHP、HTML、XML、CSS、JSON、SQL、Markdown、Batch、
  PowerShell、INI、YAML、纯文本
- **编辑体验**：行号、代码折叠、当前行高亮、括号匹配、自动缩进、多光标
  （Ctrl+点击）、列块选择（Alt+Shift）、行复制/删除/上下移动、行注释/块注释、
  大小写转换、制表符↔空格、去除行尾空白
- **查找替换**：区分大小写、全字匹配、正则表达式（`\1`–`\9` 分组引用）、循环
  查找、选区内替换、计数、全部替换；F3 / Shift+F3 / Ctrl+F3 快速查找
- **编码支持**：ANSI（GBK/GB18030）、UTF-8、UTF-8 BOM、UTF-16 LE/BE 自动检测、
  菜单一键互转；CRLF / LF / CR 行尾显示与转换
- **界面**：中英双语（跟随系统，设置中切换）；亮色 / 深色主题（含深色标题栏）；
  Ctrl+滚轮缩放；自动换行；显示空白与行尾符；F11 全屏
- **状态栏**：完整路径、长度、行数、行列号、选区字数、行尾类型、编码、
  插入/覆盖、缩放比
- **侧边栏**：文件夹工作区（带文件系统监视自动刷新）+ 最近文件
- **贴心细节**：会话恢复（重开程序回到上次的标签与光标位置）、单实例运行
  （重复打开文件时转发给已运行窗口）、设置与最近文件持久化

## 构建

需要 [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)（含
Windows 桌面开发组件）。

```powershell
# 还原 + 编译
dotnet build

# 运行单元测试（53 个）
dotnet test

# 直接运行
dotnet run --project src/NotepadPro

# 发布为自包含单文件 exe（无需安装 .NET）
dotnet publish src/NotepadPro -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -o publish
```

应用程序图标由 `tools/make-icon.ps1` 脚本生成（原创绘制），可随时重新生成：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/make-icon.ps1
```

## 目录结构

```
src/NotepadPro/
├─ Core/            文档模型、编码检测、查找引擎、会话与设置持久化
├─ Languages/       22 种语言的语法定义（lexer、关键字、样式角色映射）
├─ Localization/    中英双语字符串表（嵌入式 JSON）
├─ UI/              主窗口、自绘标签条、对话框、侧边栏、主题管理
└─ Program.cs       入口：单实例互斥 + 命名管道参数转发
tests/NotepadPro.Tests/   xUnit 单元测试（编码/EOL/语言/持久化/本地化）
tools/                    图标生成脚本
```

## 键盘快捷键

| 操作 | 快捷键 |
|---|---|
| 新建 / 打开 / 保存 | Ctrl+N / Ctrl+O / Ctrl+S |
| 另存为 / 全部保存 | Ctrl+Alt+S / Ctrl+Shift+S |
| 查找 / 替换 / 转到行 | Ctrl+F / Ctrl+H / Ctrl+G |
| 查找下一个 / 上一个 | F3 / Shift+F3 |
| 选词查找 | Ctrl+F3 |
| 行注释 / 块注释 | Ctrl+Q / Ctrl+Shift+Q |
| 复制行 / 删除行 | Ctrl+D / Ctrl+L |
| 行上移 / 下移 | Ctrl+Shift+↑ / Ctrl+Shift+↓ |
| 切换标签 | Ctrl+Tab / Ctrl+Shift+Tab |
| 缩放 | Ctrl+滚轮 / Ctrl+数字加减 |
| 全屏 | F11 |

## 协议

MIT License。详见 [LICENSE](LICENSE)。

## 路线图

- [ ] 文件比较
- [ ] 函数列表面板
- [ ] 列编辑模式增强
- [ ] 插件接口
- [ ] 更多语言与自定义主题导入
