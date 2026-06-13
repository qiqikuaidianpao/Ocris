# Ocris / 识界 — 产品设计文档

> 开源的截图 OCR 智能助手：截图即识别，识别内容可被多种智能动作处理。
>
> **Ocris**（英文主名）/ **识界**（中文名）。源自拉丁语 *ocris*，意为"山峰、锐利的顶端"——寓意锐利地捕捉每一个字符、登顶识别精度；前三字母 OCR 恰好呼应技术内核。

## 1. 产品定位

- **一句话定位**：一款以**截图 + OCR 为核心卖点**的桌面工具，AI（答题 / 翻译 / 解释 / 总结）作为**可插拔的智能动作**之一，而非唯一目的。
- **目标用户**：学生、在线学习者、需要快速提取屏幕文字与智能处理的重度用户。
- **差异化**：
  - vs Umi-OCR（纯 OCR）：多一层"智能后处理"动作，不只转文字。
  - vs PixPin（闭源商业）：开源、引擎可替换、可二次开发。
  - vs 天若OCR：AI 能力更强，架构更现代（MVVM + 依赖注入 + 引擎抽象）。

## 2. 核心理念

1. **截图 → OCR 是主线**，是产品的核心卖点与第一体验。
2. **AI 是 Action，不是主链路**：OCR 结果经"动作路由器"分发到不同动作（复制 / 答题 / 翻译 / 解释 / 总结），AI 仅是其中一类动作。这从架构上保证产品不被 AI 绑死。
3. **引擎可替换**：截图引擎、OCR 引擎、AI 引擎均接口抽象、可配置切换。
4. **开源 · 离线优先 · 配置驱动**。

## 3. 功能架构

| 模块 | 功能 | 参考 | 现状 |
|------|------|------|------|
| 截图 | 区域截图 + UI 元素智能识别高亮 | PixPin / Snipaste | ⚠️ 需重建（去 ShareX） |
| 截图 | 多屏 + 高 DPI 适配 | 全员 | ⚠️ 逻辑在死代码 |
| 截图 | 全局快捷键（配置化） | 全员 | ⚠️ 硬编码需改 |
| OCR | 离线 PaddleOCR（默认） | Umi-OCR | ✅ 已有 |
| OCR | 段落重排 / 竖排 / 去水印 | Umi-OCR | ❌ Phase 2 |
| OCR | 批量图片 OCR | Umi-OCR | ❌ Phase 2 |
| OCR | 公式识别 → LaTeX | PixPin | ❌ Phase 2 |
| 智能动作 | 选择题 / 判断题答题 | — | ⚠️ 有雏形，需解耦 |
| 智能动作 | 名词解释 / 简答 | — | ❌ Phase 3 |
| 智能动作 | 翻译 / 总结 / 改写 | 天若OCR | ❌ Phase 3 |
| 引擎 | OCR 引擎可切换（离线 / 在线 API） | Umi-OCR | ❌ 待抽象 |
| 引擎 | AI 引擎可切换（阿里云 / OpenAI / 本地） | 天若OCR | ⚠️ 两套并存需收敛 |
| 工程化 | 历史记录 | 全员 | ❌ Phase 4 |
| 工程化 | HTTP API / 命令行 | Umi-OCR | ❌ Phase 4 |

## 4. 技术架构

```
┌──────────────────────────────────────────────────────────┐
│  UI 层 (WPF Views)                                        │
│  MainWindow / ScreenshotWindow(选区) / SettingsWindow     │
├──────────────────────────────────────────────────────────┤
│  应用层 (ViewModels + 动作路由)                            │
│  OCR 结果 → IActionRouter → 分发到 IAction 实现           │
│   AnswerAction / TranslateAction / ExplainAction / Copy   │
├──────────────────────────────────────────────────────────┤
│  服务层 (接口驱动，引擎可替换)                              │
│  IScreenshotEngine  → GdiScreenshotEngine (BitBlt+UIA)     │
│  IOCREngine         → PaddleOCREngine (默认，可换在线API)  │
│  IAIEngine          → AliCloudEngine / OpenAIEngine        │
│  IWindowDetector    → UiaWindowDetector (快照+缓存查找)    │
├──────────────────────────────────────────────────────────┤
│  基础设施 (IoC / Config / Log / Hotkey)                    │
│  ServiceContainer + 统一 ConfigService (JSON)             │
└──────────────────────────────────────────────────────────┘
```

**分层原则**：
- 每层只依赖下一层的接口，不跨层。
- 引擎层每个能力都是"接口 + 默认实现 + 可选实现"，配置切换。
- 动作路由（`IActionRouter`）是把 AI 从主链路降级为可选动作的关键抽象。

## 5. 关键技术决策

| 决策 | 选型 | 理由 / 参考 |
|------|------|------------|
| 截图引擎 | **原生 GDI `BitBlt` + `CAPTUREBLT`** | 零第三方依赖；`CAPTUREBLT` 保证分层窗口被捕获。参考 HandyScreenshot `ScreenshotHelper` |
| 窗口/控件识别 | **UI Automation（UIA）"快照 + 缓存查找"** | 截图开始一次性缓存 UI 树物理矩形，鼠标移动时层次化查找；避免实时 UIA 的慢与 COM 异常。参考 HandyScreenshot `RectDetector` |
| 多屏 | `SystemInformation.VirtualScreen` 全屏背景（MVP）→ 混合 DPI 再升级为 `EnumDisplayMonitors` 逐屏 | 物理坐标贯穿，避免 DPI 错乱 |
| 全局热键 | Win32 `RegisterHotKey` + **返回值真冲突检测**（弃用永远返回 true 的假检测） | 参考 HandyScreenshot `GlobalHotKey` |
| OCR | PaddleOCR（离线，默认） | 已有；接口抽象为 `IOCREngine` 以便后续接在线 API |
| AI | 抽象 `IAIEngine`，收敛现有阿里云 + OpenAI 两套 | 配置切换 |
| 目标框架 | 维持 .NET Framework 4.8（WPF） | 现状，暂不升级（升级到 .NET 8 列入 Phase 4 评估） |

## 6. 现有代码改造映射

| 现状 | 改造目标 |
|------|---------|
| `ShareXScreenshotEngine`（8 DLL + 反射魔改） | **删除**，新建 `GdiScreenshotEngine`（BitBlt + UIA） |
| `ScreenshotWindow`（889 行死代码） | **激活并简化**，作为选区 UI |
| `WindowDetectionService`（实时 UIA） | **重构为快照+缓存查找**，启用为窗口识别核心 |
| `AIService` + `AliCloudAIService`（两套） | **收敛为 `IAIEngine` + 多实现** |
| `MainViewModel`（1452 行，AI 绑死） | **引入 `IActionRouter`**，OCR 与 AI 解耦 |
| `SettingsViewModel`（new 独立 ConfigService） | **接入 ServiceContainer 单例** |
| 快捷键硬编码 | **配置化 + 真冲突检测** |
| `config.json`（密钥泄露 + 三套字段并存） | **脱敏 + .gitignore + 字段收敛** |
| 旧命名空间（fork 来源遗留） | **统一为 `Ocris`** |

## 7. 路线图

- **Phase 0 — 立项准备**：设计文档（本文）、全局改名 `改名`。
- **Phase 1 — 截图引擎重建（MVP）**：去 ShareX 化；新建 GDI 截图引擎；UIA 智能识别；修 4 个 Bug（窗口识别 / 多屏 / 快捷键 / 设置）；配置与密钥收敛。
- **Phase 2 — OCR 增强**：抽象 `IOCREngine`；段落重排、批量 OCR、公式识别。
- **Phase 3 — 智能动作架构**：`IActionRouter` + 多 `IAction`；抽象 `IAIEngine` 收敛两套配置；翻译 / 解释 / 总结。
- **Phase 4 — 产品化**：历史记录、HTTP API / 命令行、主题 / 多语言、README、升级 .NET 评估。

## 8. 工程约定

- **命名空间 / 程序集**：`Ocris`（RootNamespace / AssemblyName）。
- **架构**：MVVM + 自定义轻量 IoC（`ServiceContainer`，保留并完善）。
- **编码**：代码与标识符英文；中文仅用于 UI 文案、日志、注释。
- **许可证**：MIT（沿用）。
- **配置**：单一 `config.json`，敏感信息（API Key）不入库，提供 `config.example.json` 模板。
