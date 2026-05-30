# Vence 文思

Vence 文思是一款 Windows-first 的智能 Markdown 编辑器。它的产品原则是让作者保留主体性：Markdown 文件是内容真源，AI 只提供建议，所有修改都需要用户确认。

## 当前状态

项目正在按 MVP 计划推进，已完成：

- WinUI 3 桌面应用外壳和三栏写作界面。
- WebView2 编辑器宿主与 C# / JavaScript 消息桥。
- 本地 Markdown 文件保存、SQLite 文档元数据。
- Markdown 大纲解析。
- AI 建议 schema、mock provider 测试和失败保护。
- 后台 job 队列，支持取消、重试、进度事件和默认并发限制。
- 显式保存快照和恢复命令。

## 技术栈

- .NET 10
- C#
- WinUI 3 / Windows App SDK
- WebView2
- SQLite / EF Core
- Markdig
- Microsoft.Extensions.AI
- xUnit

## 项目结构

```text
src/
  Vence.App/          WinUI 3 应用外壳
  Vence.EditorHost/   WebView2 编辑器宿主和桥接协议
  Vence.Core/         文档、命令、建议等核心模型
  Vence.Storage/      本地文件、SQLite 元数据、快照
  Vence.Markdown/     Markdown 解析和大纲
  Vence.AI/           AI provider 抽象、prompt、建议解析
  Vence.Jobs/         后台任务队列
tests/
  Vence.*.Tests/      各模块单元测试
docs/
  architecture/       技术架构
  plans/              实施计划
  qa/                 私测检查清单
editor/               编辑器前端脚手架
scripts/              打包和维护脚本
```

## 常用命令

```powershell
$env:DOTNET_CLI_HOME="$PWD\.dotnet"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT="1"

dotnet restore Vence.sln
dotnet build Vence.sln --no-restore -m:1 -v:minimal
dotnet test tests/Vence.Core.Tests/Vence.Core.Tests.csproj --no-restore -v:minimal
dotnet test tests/Vence.Storage.Tests/Vence.Storage.Tests.csproj --no-restore -v:minimal
dotnet test tests/Vence.Markdown.Tests/Vence.Markdown.Tests.csproj --no-restore -v:minimal
dotnet test tests/Vence.AI.Tests/Vence.AI.Tests.csproj --no-restore -v:minimal
dotnet test tests/Vence.Jobs.Tests/Vence.Jobs.Tests.csproj --no-restore -v:minimal
```

## 打包

默认生成 x64 Release 发布产物：

```powershell
.\scripts\package.ps1
```

指定平台：

```powershell
.\scripts\package.ps1 -Configuration Release -Platform x64
```

发布产物默认输出到：

```text
artifacts/publish/Vence.App/Release/x64
```

## 私测

私测前请按 [private-beta-checklist.md](docs/qa/private-beta-checklist.md) 检查核心流程、失败场景和隐私边界。

## 设计原则

- AI 克制：AI 只产生建议，不直接改正文。
- 本地优先：文档默认保存在本地 Markdown 文件中。
- 可恢复：显式保存生成快照，恢复失败不能破坏当前正文。
- 模块化：UI、编辑器、存储、AI、后台任务分层演进。
