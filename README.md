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

- **框架**: .NET Framework 4.8
- **UI**: WPF (Windows Presentation Foundation)
- **OCR**: PaddleOCR
- **AI服务**: 阿里云DashScope API
- **架构模式**: MVVM
- **目标平台**: x64

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

### 系统要求

- **操作系统**: Windows 10/11 (x64)
- **.NET Framework**: 4.8 或更高版本
- **Visual C++ 运行库**: Microsoft Visual C++ 2015-2022 Redistributable (x64)
- **内存**: 建议 4GB 以上
- **磁盘空间**: 至少 500MB 可用空间

### 一键运行（推荐）

**克隆并直接运行**
```bash
# 1. 克隆项目
git clone https://github.com/qiqikuaidianpao/AIAnswerTool.git
cd AIAnswerTool

# 2. 构建项目（自动处理所有依赖）
.\build.ps1

# 3. 运行程序
.\bin\Debug\AIAnswerTool.exe
```

> ✅ **保证**: 执行以上命令后，程序可直接运行，无需额外配置

### 构建说明

#### 自动构建（推荐）

项目包含自动化构建脚本，会处理所有依赖和配置：

```powershell
# 在项目根目录执行
.\build.ps1
```

**构建脚本功能**：
- 自动复制所有必需的DLL到lib文件夹
- 编译项目生成可执行文件
- 确保所有运行时依赖正确部署
- 生成完整的可分发版本

#### 手动构建

如果需要手动构建，请按以下步骤：

```bash
# 1. 确保依赖文件在正确位置
# 检查 lib/ 文件夹包含所有必需的DLL

# 2. 使用MSBuild编译
"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" AIAnswerTool.csproj /p:Configuration=Debug /p:Platform=x64

# 3. 验证输出
# 检查 bin\Debug\ 文件夹包含 AIAnswerTool.exe 和所有DLL
```

#### Visual Studio 构建

1. 使用 Visual Studio 2019 或更高版本打开 `AIAnswerTool.csproj`
2. 选择 **Debug** 配置和 **x64** 平台
3. 按 `Ctrl+Shift+B` 构建项目
4. 按 `F5` 运行程序

### 项目依赖

项目使用以下核心依赖库：

| 依赖库 | 版本 | 用途 |
|--------|------|------|
| PaddleOCRSharp | 最新 | OCR文字识别引擎 |
| Newtonsoft.Json | 最新 | JSON数据处理 |
| RestSharp | 最新 | HTTP客户端 |
| NHotkey.Wpf | 最新 | 全局快捷键支持 |
| WPFDevelopers | 最新 | WPF UI组件库 |
| Microsoft.Web.WebView2 | 最新 | 内嵌浏览器控件 |
| AutoUpdater.NET | 最新 | 自动更新功能 |

**重要说明**：
- 所有依赖已预配置在项目中
- 构建脚本会自动处理依赖部署
- 无需手动下载或安装额外组件

### 输出结构

成功构建后，`bin\Debug\` 目录包含：

```
bin\Debug\
├── AIAnswerTool.exe          # 主程序
├── AIAnswerTool.exe.config    # 配置文件
├── *.dll                      # 所有依赖库
├── inference\                 # OCR模型文件
│   ├── ch_PP-OCRv4_det_infer\
│   ├── ch_PP-OCRv4_rec_infer\
│   └── ...
└── 多语言资源文件夹
```

### 分发部署

要分发给其他用户：

1. 将整个 `bin\Debug\` 文件夹复制到目标机器
2. 确保目标机器安装了 .NET Framework 4.8
3. 直接运行 `AIAnswerTool.exe`

> 📦 **打包提示**: 可以使用 7-Zip 或 WinRAR 将 `bin\Debug\` 文件夹打包为压缩包分发



### 配置说明

在 `App.config` 中配置以下参数：

- `AliCloudApiKey`: 阿里云API密钥
- `AliCloudBaseUrl`: 阿里云服务地址
- `LogPath`: 日志文件路径
- `ScreenshotPath`: 截图保存路径

### 故障排除

#### 常见问题

**Q: 编译时提示找不到DLL文件**
```
A: 执行 .\build.ps1 脚本，它会自动复制所有必需的DLL到正确位置
```

**Q: 运行时提示缺少 .NET Framework**
```
A: 下载并安装 Microsoft .NET Framework 4.8
   下载地址: https://dotnet.microsoft.com/download/dotnet-framework/net48
```

**Q: OCR功能无法使用**
```
A: 确保以下文件存在：
   - PaddleOCR.dll
   - opencv_world470.dll
   - paddle_inference.dll
   - inference/ 文件夹及其模型文件
```

**Q: 程序启动后立即崩溃**
```
A: 检查以下项目：
   1. 确保所有DLL文件在 bin\Debug\ 目录下
   2. 检查 App.config 配置文件是否正确
   3. 查看日志文件获取详细错误信息
```

**Q: PowerShell脚本执行被阻止**
```
A: 以管理员身份运行PowerShell，执行：
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

#### 日志文件

程序运行时会生成详细的日志文件，位置：
- 默认路径: `%TEMP%\AIAnswerTool\logs\`
- 可通过 App.config 自定义路径

#### 获取帮助

如果遇到其他问题：
1. 查看日志文件获取详细错误信息
2. 在 GitHub Issues 中搜索相似问题
3. 提交新的 Issue 并附上日志文件

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

<!-- 测试Trae GitHub推送功能 - 可删除此行 -->