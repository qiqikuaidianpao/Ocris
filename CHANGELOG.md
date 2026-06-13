# 变更日志

本项目遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

## [2.0.0] — 2026-06-13 · 首版发布

一款开源的截图 OCR 智能助手 —— 截图即识别，识别内容可被多种智能动作处理。

### ✨ 核心功能
- **智能截图**：自研轻量引擎（GDI BitBlt + UIA 窗口/控件智能识别），区域选择、多屏、高 DPI 适配
- **OCR 识别**：基于 PaddleOCR 离线识别，段落重排、批量图片
- **智能动作**：AI 答题 / 翻译 / 解释 / 复制（可插拔，AI 不是唯一目的）
- **暗色主题**：Light / Dark 即时切换 + 持久化
- **全局快捷键**：可配置，Win32 真冲突检测，改后免重启

### 🏗️ 架构
- WPF + MVVM + 自定义 ServiceContainer（依赖注入）
- 引擎可替换（截图 / OCR / AI 接口抽象）
- 动作路由（OCR 结果分发到不同动作）

### 🔒 安全
- 配置脱敏（`config.json` 不入库）
- OCR 离线运行（识别内容不外传）

### 📦 工程化
- MIT 许可证、README、CONTRIBUTING、.editorconfig
- GitHub Actions CI
- 现代 UI（靛蓝配色、明暗主题）
