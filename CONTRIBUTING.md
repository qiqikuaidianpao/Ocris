# 贡献指南

感谢你考虑为 Ocris 贡献！🎉

## 🐛 反馈与建议

- **Bug / 功能建议**：提 [Issue](../../issues)，请描述清楚复现步骤与期望
- 提交前请先搜索是否已有类似 Issue

## 🔧 代码贡献流程

1. **Fork** 仓库
2. 创建分支：`git checkout -b feat/your-feature`
3. 提交改动（遵循下方约定）
4. 发起 **Pull Request**，说明改动内容与动机

## 📐 代码约定

### 语言版本（重要）

项目使用 **.NET Framework 4.8 + Framework MSBuild（C# 5 编译器）**，**不支持 C# 6+ 语法**：

| ❌ 禁用 | ✅ 替代 |
|---------|---------|
| `out ModifierKeys x`（out 变量声明）| 预声明变量 + `out x` |
| `$"..."`（字符串插值）| `string.Format(...)` |
| `obj?.Member`（null 条件）| 显式 `if (obj != null)` |

### 风格

- 遵循 `.editorconfig`（UTF-8、CRLF、4 空格缩进）
- 标识符与代码用英文；中文仅用于 UI 文案、日志、注释
- 遵循现有 **MVVM + ServiceContainer** 架构（服务接口驱动、依赖注入）

### 提交信息

格式：`类型: 描述`

- `feat` 新功能
- `fix` 修复
- `refactor` 重构
- `docs` 文档
- `chore` 杂项

示例：`feat: 新增滚动截图`、`fix: 多屏截图坐标偏移`

## 🛠️ 本地构建

```powershell
.\build.ps1
# 或
MSBuild Ocris.csproj /p:Configuration=Debug
```

输出：`bin\Debug\Ocris.exe`

> 首次运行需复制 `config.example.json` 为 `config.json` 并填入 AI API Key。

## 🎯 设计理念

贡献前建议阅读 [`docs/DESIGN.md`](docs/DESIGN.md)：

- **OCR 是核心**，AI 是可插拔动作
- **引擎可替换**（截图/OCR/AI 接口抽象）
- **离线优先**（OCR 本地，不外传）

## 📄 许可证

贡献的代码将在 [MIT License](LICENSE) 下发布。
