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

## 构建与发布流程

Vence 当前采用 MSIX 打包的 WinUI 3 应用，并保留 Windows App SDK 自包含部署，减少目标电脑运行时差异。日常开发使用 package identity loose layout 运行，正式交付生成 `artifacts` 下的 MSIX 安装包。

| 场景 | 命令 | 说明 |
| --- | --- | --- |
| 日常调试运行 | `.\scripts\dev.ps1` | 使用 Debug/x64 和 package identity 直接运行应用，不生成安装包。 |
| 只验证编译 | `.\scripts\build.ps1` | 编译整个解决方案，输出在各项目 `bin/obj` 下。 |
| 运行测试 | `.\scripts\test.ps1` | 运行整个解决方案的测试项目。 |
| 生成 Dev 安装包 | `.\scripts\stage-dev.ps1` | 发布 Debug MSIX，默认输出到 `artifacts\dev\Vence.App\x64-msix`。 |
| 生成正式发布包 | `.\scripts\release.ps1` | 先跑 Release 测试，再发布 Release MSIX。 |
| 安装发布包 | `.\scripts\install-msix.ps1 -TrustCertificate` | 首次安装本地测试证书并安装 MSIX。 |
| 启动已安装版本 | `.\scripts\run-published.ps1` | 默认启动已安装的 MSIX 应用。 |

常用参数：

```powershell
.\scripts\dev.ps1 -Platform x64
.\scripts\build.ps1 -Configuration Release -Platform x64
.\scripts\test.ps1 -Configuration Release -Platform x64
.\scripts\stage-dev.ps1 -Platform x64
.\scripts\release.ps1 -Platform x64
.\scripts\install-msix.ps1 -Configuration Release -Platform x64 -TrustCertificate
.\scripts\run-published.ps1 -Channel Release -Platform x64
```

正式发布产物默认输出到：

```powershell
artifacts\publish\Vence.App\Release\x64-msix\Vence.App_1.0.0.0_x64_Test\Vence.App_1.0.0.0_x64.msix
```

Dev 安装包默认输出到：

```powershell
artifacts\dev\Vence.App\x64-msix\Vence.App_1.0.0.0_x64_Test\Vence.App_1.0.0.0_x64.msix
```

## 构建、运行、发布的区别

- `build` 只检查代码是否能编译，产物主要给开发工具和测试使用，不作为交付目录。
- `dev` 是本机调试运行，适合边改边看，使用 MSIX package identity 但不生成安装包。
- `stage-dev` 是 Debug MSIX，适合测试安装、更新和用户环境问题。
- `release` 是正式交付入口，默认先测试，再生成 Release MSIX。
- `x64` 表示 CPU 架构；`msix` 表示有包身份、可安装、可升级的 Windows 应用包。

MSIX 本地测试证书默认生成在：

```powershell
artifacts\certs\Vence.TestCertificate.pfx
artifacts\certs\Vence.TestCertificate.cer
```

首次安装本地签名的 MSIX 时运行：

```powershell
.\scripts\install-msix.ps1 -TrustCertificate
```

如果只想验证能否生成包但不需要安装，可生成未签名包：

```powershell
.\scripts\release.ps1 -Platform x64 -Unsigned
```

仍可使用非打包自包含 exe 作为故障兜底：

```powershell
.\scripts\package.ps1 -Configuration Release -Platform x64
```

但日常建议使用 MSIX 脚本，避免混淆构建、发布和安装。

## 私测

私测前请按 [private-beta-checklist.md](docs/qa/private-beta-checklist.md) 检查核心流程、失败场景和隐私边界。

## 设计原则

- AI 克制：AI 只产生建议，不直接改正文。
- 本地优先：文档默认保存在本地 Markdown 文件中。
- 可恢复：显式保存生成快照，恢复失败不能破坏当前正文。
- 模块化：UI、编辑器、存储、AI、后台任务分层演进。
