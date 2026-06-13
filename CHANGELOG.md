# 变更日志

本项目遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

## [2.0.0] — 2026-06-13 · Ocris 重构发布

从「AIAnswerTool 答题工具」重构为「**Ocris 截图 OCR 智能助手**」开源产品。

### 🎯 重新定位
- 产品定位调整为：**截图 + OCR 为核心卖点**，AI（答题/翻译/解释）作为可插拔动作
- 全局改名 `AIAnswerTool` → `Ocris / 识界`（命名空间、程序集、文件、品牌）

### ✨ 新增
- **智能动作**：🌐 翻译 / 💡 解释 / 📋 复制（OCR 结果可走不同 AI 处理）
- **暗色主题**：Light / Dark 即时切换 + 持久化（ThemeService）
- **OCR 段落重排**：按坐标聚类行，替代"每块一行"碎片化
- **批量图片 OCR**：多选图片逐张识别
- **自研截图引擎**：GDI BitBlt + UIA 窗口智能识别（零第三方截图库依赖）
- **热键免重启**：配置变更自动重注册
- **现代 UI**：靛蓝配色、卡片布局、品牌统一

### 🐛 修复（原 4 个核心问题）
- ① 截图窗口智能识别（激活 ScreenshotWindow + WindowDetectionService）
- ③ 多屏截图覆盖（VirtualScreen，去反射魔改的 RestrictToActiveMonitor）
- ② 快捷键配置化（读 config）+ Win32 真冲突检测
- ④ 设置接入 ServiceContainer 单例（不再孤立实例）

### 🔒 安全
- API Key 脱敏（`config.json` 不入库 + `.gitignore` + 模板 `config.example.json`）

### 🏗️ 架构
- 去 ShareX 化（移除 ShareX.ScreenCaptureLib 等 DLL 依赖）
- 接口重命名：`IShareXScreenshotEngine → IScreenshotEngine` 等（源码零 ShareX 残留）

### 📦 工程化
- LICENSE (MIT)、.editorconfig、README 全面重写
- Debug 日志噪音清理、AssemblyInfo v2.0.0

### ⚠️ 破坏性变更
- 仓库名/命名空间变更（`AIAnswerTool` → `Ocris`）
- 配置结构：快捷键统一到 `Hotkeys` 段（旧 `ScreenShotKeys` 字段废弃）

---

## [1.x] — 原 AIAnswerTool

历史版本（基于 ShareX 的答题工具）。见 git 历史。
