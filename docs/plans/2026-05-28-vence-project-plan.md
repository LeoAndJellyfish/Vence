# Vence 文思项目计划书

> 执行提示：按本文任务逐项实施，每个任务完成后运行测试并提交一次小步 commit。

**Goal:** 构建 Vence 文思的 Windows-first 智能 Markdown 编辑器 MVP，并为私测版保留 AI、导出、历史记录和未来同步能力。

**Architecture:** 采用模块化单体：WinUI 3 + C# 负责桌面应用外壳、业务命令、本地存储和 AI provider 抽象；WebView2 承载复杂编辑器内核；Markdown 文件是内容真源，SQLite 保存索引、快照、建议和批注。

**Tech Stack:** .NET 10 LTS, C#, WinUI 3, Windows App SDK, WebView2, SQLite, EF Core, Microsoft.Extensions.AI, xUnit, FluentAssertions, Serilog.

---

## 1. 项目阶段

| 阶段 | 时间 | 目标 | 交付物 |
| --- | --- | --- | --- |
| Phase 0 | 1 周 | 技术验证与产品范围冻结 | Spike 原型、MVP 范围、风险清单 |
| Phase 1 | 2 周 | 应用骨架和本地文档系统 | WinUI Shell、工作区、文件保存、SQLite |
| Phase 2 | 3 周 | 编辑器 MVP | Markdown 编辑、预览、源码、LaTeX、Mermaid、图片 |
| Phase 3 | 3 周 | AI 辅助 v1 | 语法检查、润色、读者模式、建议接受/拒绝 |
| Phase 4 | 2 周 | 体验打磨与打包 | 历史记录、导出、设置、安装包 |
| Phase 5 | 3-4 周 | 私测硬化 | 崩溃修复、性能、隐私、反馈闭环 |

建议节奏：10-11 周完成可用 Alpha，14-16 周进入私测。

## 2. 里程碑

### M0：技术 Spike

成功标准：

- WinUI 3 应用可启动。
- WebView2 可加载本地编辑器页面。
- C# 与 WebView2 可双向传递文档变更。
- SQLite 可保存文档元数据。
- AI provider 可通过 mock 返回结构化建议。

### M1：本地编辑器可用

成功标准：

- 用户可以创建、打开、保存 Markdown。
- 左侧文档列表、中央编辑区、右侧 AI 面板框架可用。
- 支持基础 Markdown 块和源码视图。

### M2：文思核心体验成形

成功标准：

- 支持大纲、LaTeX、Mermaid、图片、预览。
- 支持语法检查、润色、读者模式。
- 所有 AI 修改都需要用户确认。

### M3：私测包

成功标准：

- 可安装、可更新或可替换安装。
- 连续写作 30 分钟无卡死、无丢字。
- 崩溃日志可定位问题，不记录正文全文。

## 3. 工作拆分

### Task 1：创建 .NET Solution 骨架

**Files:**

- Create: `Vence.sln`
- Create: `src/Vence.App/Vence.App.csproj`
- Create: `src/Vence.Core/Vence.Core.csproj`
- Create: `src/Vence.Storage/Vence.Storage.csproj`
- Create: `src/Vence.Markdown/Vence.Markdown.csproj`
- Create: `src/Vence.AI/Vence.AI.csproj`
- Create: `src/Vence.Jobs/Vence.Jobs.csproj`
- Create: `tests/Vence.Core.Tests/Vence.Core.Tests.csproj`

**Step 1: 初始化 solution**

Run:

```powershell
dotnet new sln -n Vence
dotnet new classlib -n Vence.Core -o src/Vence.Core
dotnet new classlib -n Vence.Storage -o src/Vence.Storage
dotnet new classlib -n Vence.Markdown -o src/Vence.Markdown
dotnet new classlib -n Vence.AI -o src/Vence.AI
dotnet new classlib -n Vence.Jobs -o src/Vence.Jobs
dotnet new xunit -n Vence.Core.Tests -o tests/Vence.Core.Tests
```

Expected: 所有项目创建成功。

**Step 2: 添加项目引用**

Run:

```powershell
dotnet sln add src/Vence.Core/Vence.Core.csproj
dotnet sln add src/Vence.Storage/Vence.Storage.csproj
dotnet sln add src/Vence.Markdown/Vence.Markdown.csproj
dotnet sln add src/Vence.AI/Vence.AI.csproj
dotnet sln add src/Vence.Jobs/Vence.Jobs.csproj
dotnet sln add tests/Vence.Core.Tests/Vence.Core.Tests.csproj
dotnet add tests/Vence.Core.Tests/Vence.Core.Tests.csproj reference src/Vence.Core/Vence.Core.csproj
```

Expected: `dotnet build` 通过。

**Step 3: Commit**

```powershell
git add Vence.sln src tests
git commit -m "chore: bootstrap Vence solution"
```

### Task 2：建立领域模型和命令边界

**Files:**

- Create: `src/Vence.Core/Documents/Document.cs`
- Create: `src/Vence.Core/Documents/DocumentRange.cs`
- Create: `src/Vence.Core/Suggestions/Suggestion.cs`
- Create: `src/Vence.Core/Commands/ICommand.cs`
- Create: `src/Vence.Core/Commands/ApplySuggestionCommand.cs`
- Test: `tests/Vence.Core.Tests/Suggestions/ApplySuggestionCommandTests.cs`

**Step 1: 写失败测试**

验证：AI suggestion 不会自动写入文档，只有 ApplySuggestionCommand 执行后才修改正文。

**Step 2: 实现最小领域模型**

字段建议：

- `Document.Id`
- `Document.Path`
- `Document.Content`
- `Suggestion.Id`
- `Suggestion.Range`
- `Suggestion.Replacement`
- `Suggestion.Status`

**Step 3: 跑测试**

Run:

```powershell
dotnet test tests/Vence.Core.Tests/Vence.Core.Tests.csproj
```

Expected: PASS。

**Step 4: Commit**

```powershell
git add src/Vence.Core tests/Vence.Core.Tests
git commit -m "feat: add document suggestion command model"
```

### Task 3：实现本地存储 MVP

**Files:**

- Create: `src/Vence.Storage/IWorkspaceStore.cs`
- Create: `src/Vence.Storage/FileSystemWorkspaceStore.cs`
- Create: `src/Vence.Storage/VenceDbContext.cs`
- Create: `src/Vence.Storage/Entities/DocumentEntity.cs`
- Create: `tests/Vence.Storage.Tests/Vence.Storage.Tests.csproj`

**Step 1: 写存储测试**

验证：

- 新文档可以保存为 `.md`。
- 重新打开后内容一致。
- SQLite 中记录 path、title、updated_at。

**Step 2: 安装依赖**

Run:

```powershell
dotnet add src/Vence.Storage/Vence.Storage.csproj package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/Vence.Storage/Vence.Storage.csproj package Microsoft.EntityFrameworkCore.Design
```

**Step 3: 实现文件系统 + SQLite 存储**

规则：

- Markdown 正文写文件。
- SQLite 只保存元数据。
- 保存前写临时文件，再原子替换，避免损坏文档。

**Step 4: Commit**

```powershell
git add src/Vence.Storage tests/Vence.Storage.Tests
git commit -m "feat: add local workspace storage"
```

### Task 4：创建 WinUI 3 应用外壳

**Files:**

- Create: `src/Vence.App/App.xaml`
- Create: `src/Vence.App/App.xaml.cs`
- Create: `src/Vence.App/MainWindow.xaml`
- Create: `src/Vence.App/MainWindow.xaml.cs`
- Create: `src/Vence.App/Views/ShellView.xaml`
- Create: `src/Vence.App/ViewModels/ShellViewModel.cs`

**Step 1: 创建 WinUI 3 项目**

使用 Visual Studio 的 Blank App, Packaged (WinUI 3 in Desktop) 模板，或按 Windows App SDK CLI 模板创建。

**Step 2: 实现三栏布局**

布局：

- 左侧：文档库。
- 中间：编辑器区域。
- 右侧：文思 AI 面板。
- 底部：字数、字符数、行数、缩放、格式。

**Step 3: 视觉约束**

- 暖白背景。
- 极低对比边框。
- 8px 或更小圆角。
- 工具按钮优先图标。
- 不做营销页式卡片堆叠。

**Step 4: Commit**

```powershell
git add src/Vence.App
git commit -m "feat: add WinUI shell layout"
```

### Task 5：接入 WebView2 编辑器 Host

**Files:**

- Create: `src/Vence.EditorHost/Vence.EditorHost.csproj`
- Create: `src/Vence.EditorHost/EditorHostView.xaml`
- Create: `src/Vence.EditorHost/EditorBridge.cs`
- Create: `src/Vence.EditorHost/EditorMessage.cs`
- Create: `editor/package.json`
- Create: `editor/src/main.ts`
- Create: `editor/src/bridge.ts`

**Step 1: 定义 bridge message schema**

消息必须包含：

- `version`
- `type`
- `requestId`
- `payload`

**Step 2: C# 发送初始文档**

Expected: WebView2 加载后显示 Markdown 内容。

**Step 3: 编辑器回传变更**

Expected: C# 接收到 `document.changed`，Core 内存文档更新。

**Step 4: Commit**

```powershell
git add src/Vence.EditorHost editor
git commit -m "feat: add WebView2 editor host"
```

### Task 6：Markdown 解析、预览与大纲

**Files:**

- Create: `src/Vence.Markdown/MarkdownDocument.cs`
- Create: `src/Vence.Markdown/MarkdownParser.cs`
- Create: `src/Vence.Markdown/OutlineBuilder.cs`
- Create: `tests/Vence.Markdown.Tests/OutlineBuilderTests.cs`

**Step 1: 写大纲测试**

输入：

```markdown
# 标题
## 历史与城市风貌
## 艺术与文化影响
```

Expected: 输出 H1 + 两个 H2。

**Step 2: 实现大纲解析**

使用成熟 Markdown parser，避免正则堆叠。

**Step 3: Commit**

```powershell
git add src/Vence.Markdown tests/Vence.Markdown.Tests
git commit -m "feat: add markdown outline parser"
```

### Task 7：AI 建议系统 v1

**Files:**

- Create: `src/Vence.AI/IAssistantService.cs`
- Create: `src/Vence.AI/IChatClientFactory.cs`
- Create: `src/Vence.AI/SuggestionSchema.cs`
- Create: `src/Vence.AI/Prompts/GrammarCheckPrompt.cs`
- Create: `src/Vence.AI/Prompts/ReaderModePrompt.cs`
- Create: `tests/Vence.AI.Tests/AssistantServiceTests.cs`

**Step 1: 写 mock provider 测试**

验证：

- 输入段落后返回结构化 Suggestion。
- 无效 JSON 被拒绝，不进入文档。
- Provider 失败时返回可展示错误。

**Step 2: 安装 Microsoft.Extensions.AI**

Run:

```powershell
dotnet add src/Vence.AI/Vence.AI.csproj package Microsoft.Extensions.AI
```

**Step 3: 实现服务**

MVP 功能：

- GrammarCheck
- Rewrite
- ReaderMode

**Step 4: Commit**

```powershell
git add src/Vence.AI tests/Vence.AI.Tests
git commit -m "feat: add AI suggestion service"
```

### Task 8：后台任务队列

**Files:**

- Create: `src/Vence.Jobs/IBackgroundJobQueue.cs`
- Create: `src/Vence.Jobs/BackgroundJobQueue.cs`
- Create: `src/Vence.Jobs/JobStatus.cs`
- Test: `tests/Vence.Jobs.Tests/BackgroundJobQueueTests.cs`

**Step 1: 写取消测试**

验证：用户继续输入时，可以取消旧的 AI job。

**Step 2: 实现队列**

要求：

- 支持 cancellation token。
- 支持 retry。
- 支持 progress event。
- 默认限制并发，避免 AI 请求失控。

**Step 3: Commit**

```powershell
git add src/Vence.Jobs tests/Vence.Jobs.Tests
git commit -m "feat: add background job queue"
```

### Task 9：历史记录与恢复

**Files:**

- Create: `src/Vence.Storage/Snapshots/ISnapshotStore.cs`
- Create: `src/Vence.Storage/Snapshots/SnapshotStore.cs`
- Create: `src/Vence.Core/Commands/RestoreSnapshotCommand.cs`

**Step 1: 写恢复测试**

验证：保存两次后可以恢复到上一个版本。

**Step 2: 实现快照策略**

策略：

- 显式保存时生成快照。
- 自动保存只更新草稿，不污染历史。
- 快照可以先存完整内容，私测后再优化 diff。

**Step 3: Commit**

```powershell
git add src/Vence.Core src/Vence.Storage
git commit -m "feat: add document snapshots"
```

### Task 10：打包与私测检查

**Files:**

- Create: `scripts/package.ps1`
- Create: `docs/qa/private-beta-checklist.md`
- Modify: `README.md`

**Step 1: 创建打包脚本**

Run:

```powershell
dotnet publish src/Vence.App/Vence.App.csproj -c Release
```

**Step 2: 写私测清单**

必须覆盖：

- 新建文档。
- 打开本地 Markdown。
- 保存和恢复。
- AI 建议接受/拒绝。
- 断网状态。
- API key 配置错误。
- Mermaid/LaTeX 渲染失败。

**Step 3: Commit**

```powershell
git add scripts docs/qa README.md
git commit -m "chore: add private beta packaging checklist"
```

## 4. 测试策略

- Unit tests：Core、Storage、Markdown、AI schema。
- Integration tests：文档保存、SQLite 元数据、快照恢复。
- Editor bridge tests：消息 schema、错误消息、版本兼容。
- UI smoke tests：启动、打开文档、保存、AI 面板展示。
- Performance tests：大文档打开、连续输入、AI 防抖。
- Privacy tests：日志扫描，确保不记录正文全文和 API key。

## 5. 发布策略

### Alpha

- 面向开发者和少量内部用户。
- 手动下载安装。
- 重点验证编辑稳定性和 AI 建议价值。

### Private Beta

- 面向 20-50 位真实写作者。
- 增加崩溃日志和反馈入口。
- 收集“是否真的让人更愿意写作”的定性反馈。

### Public Beta

- 增加官网下载、更新提示、基础文档。
- 明确隐私政策和 AI provider 配置说明。

## 6. 风险控制

| 风险 | 触发信号 | 应对 |
| --- | --- | --- |
| 编辑器体验延期 | 两周内无法稳定输入和保存 | 降级为 Markdown 源码编辑 + 预览，延后 WYSIWYG。 |
| AI 输出不可控 | 建议质量不稳定或语气过强 | 收紧 prompt，强制结构化 schema，加入置信度和拒绝路径。 |
| WinUI 打包复杂 | 安装包在干净机器失败 | 先用 self-contained 发布，MSIX 延后。 |
| UI 过度功能化 | 首页和主界面像普通 AI 工具 | 以概念图为视觉回归基准，每个功能入口做减法。 |
| 隐私疑虑 | 用户不清楚发送了什么 | AI 面板显示发送范围、provider、模型和状态。 |

## 7. 待确认问题

- 首发平台是否只支持 Windows 11，还是需要兼容 Windows 10？
- MVP 是否要求纯离线可用，还是允许配置远程 AI provider？
- 文档库是管理任意本地文件夹，还是应用内专属 workspace？
- 是否需要第一版就支持 PDF / DOCX 导出？
- 官网和编辑器是否同期开工，还是先做桌面 Alpha？

## 8. 执行建议

先完成 Phase 0 Spike，再决定是否正式进入工程实现。Spike 的判断标准不是“技术能不能做”，而是：

- 输入是否足够顺滑。
- WebView2 bridge 是否可控。
- AI 建议是否符合“克制辅助”的产品气质。
- 本地 Markdown + SQLite 的数据模型是否足够简单。

如果 Spike 通过，就按 Task 1 到 Task 10 顺序推进；如果 Spike 不通过，优先调整编辑器内核方案，而不是扩大功能范围。
