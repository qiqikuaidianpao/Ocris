# AIAnswerTool

一个基于WPF的智能问答工具，集成了OCR文字识别和AI对话功能。

## 功能特性

- 🖼️ **屏幕截图**: 支持区域截图和全屏截图
- 🔍 **OCR识别**: 基于PaddleOCR的高精度文字识别
- 🤖 **AI对话**: 集成阿里云AI服务，支持智能问答
- ⌨️ **快捷键**: 支持全局快捷键操作
- 📝 **日志记录**: 完整的操作日志记录
- ⚙️ **配置管理**: 灵活的配置文件管理

## 技术栈

- **框架**: .NET Framework 4.7.2
- **UI**: WPF (Windows Presentation Foundation)
- **OCR**: PaddleOCR
- **AI服务**: 阿里云DashScope API
- **架构模式**: MVVM

## 项目结构

```
AIAnswerTool/
├── Models/          # 数据模型
├── Services/        # 业务服务层
├── ViewModels/      # 视图模型
├── Views/           # 用户界面
├── Utils/           # 工具类
├── Resources/       # 资源文件
├── lib/             # 第三方库
└── Properties/      # 项目属性
```

## 快速开始

### 环境要求

- Windows 10/11
- .NET Framework 4.7.2 或更高版本
- Visual Studio 2019 或更高版本（开发）

### 从GitHub获取项目

1. 克隆项目到本地
```bash
git clone https://github.com/qiqikuaidianpao/AIAnswerTool.git
cd AIAnswerTool
```

2. 恢复NuGet包（如果需要）
```bash
nuget restore
# 或者在Visual Studio中右键解决方案 -> 还原NuGet包
```

### 编译运行

**方法一：使用PowerShell脚本**
```powershell
.\build.ps1
```

**方法二：使用Visual Studio**
1. 双击 `AIAnswerTool.sln` 打开项目
2. 按 `Ctrl+Shift+B` 编译项目
3. 按 `F5` 运行程序

**方法三：使用命令行**
```bash
# 编译项目
msbuild AIAnswerTool.sln /p:Configuration=Release

# 运行程序
cd bin\Release
AIAnswerTool.exe
```

### 配置说明

在 `App.config` 中配置以下参数：

- `AliCloudApiKey`: 阿里云API密钥
- `AliCloudBaseUrl`: 阿里云服务地址
- `LogPath`: 日志文件路径
- `ScreenshotPath`: 截图保存路径

## 使用说明

1. **截图识别**: 使用快捷键进行屏幕截图
2. **文字识别**: 自动对截图进行OCR识别
3. **AI问答**: 将识别的文字发送给AI进行智能回答
4. **结果查看**: 在主界面查看AI回答结果

## 开发说明

### 核心服务

- `IScreenshotService`: 截图服务接口
- `IOCRService`: OCR识别服务接口
- `IAIService`: AI对话服务接口
- `ILogService`: 日志服务接口
- `IConfigService`: 配置管理服务接口

### 主要特性

- **模块化设计**: 各功能模块独立，便于维护和扩展
- **配置驱动**: 支持通过配置文件调整各种参数
- **日志完整**: 详细的操作日志，便于问题排查
- **异常处理**: 完善的异常处理机制

## 贡献指南

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开 Pull Request

## 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情

## 更新日志

### v1.0.0
- 初始版本发布
- 基础截图和OCR功能
- AI对话集成
- 配置管理系统

## 联系方式

如有问题或建议，请通过以下方式联系：

- 提交 Issue
- 发送邮件到: [your-email@example.com]

---

**注意**: 使用前请确保已正确配置阿里云API密钥和相关服务。