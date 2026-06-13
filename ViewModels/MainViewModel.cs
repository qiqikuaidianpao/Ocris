using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Ocris.Models;
using Ocris.Services;
using Ocris.Utils;

namespace Ocris.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Fields
        
        private readonly IAIService _aiService;
        private readonly ILogService _logService;
        private readonly IScreenshotService _screenshotService;
        private readonly IOCRService _ocrService;
        private readonly IHotkeyService _hotkeyService;
        
        private string _questionText = "截图 (Alt+Q) · 粘贴 · 或在此输入文字...";
        private string _answerText = "识别文本后，选择「翻译 / 解释 / 智能解答」查看处理结果。";
        private string _statusText = "就绪";
        private string _tokenUsage = "";
        private bool _isProcessing = false;
        private bool _isWindowTopmost = false; // 添加窗口置顶状态字段
        
        // 参考选项相关字段
        private ObservableCollection<ChoiceOption> _choiceOptions = new ObservableCollection<ChoiceOption>();
        private Visibility _choiceOptionsVisibility = Visibility.Collapsed;
        private Visibility _judgmentAnswerVisibility = Visibility.Collapsed;
        private Visibility _otherAnswerVisibility = Visibility.Collapsed;
        private Visibility _emptyStateVisibility = Visibility.Visible;
        private string _judgmentIcon = "";
        private string _judgmentText = "";
        private string _finalAnswerText = "";
        
        // 快捷键和固定区域OCR相关字段
        private bool _hasScreenshot = false;
        private Rectangle? _selectedArea = null;
        private readonly IConfigService _configService;
        private string _selectedQuestionType = "Auto";
        
        #endregion
        
        #region Properties
        
        public string QuestionText
        {
            get { return _questionText; }
            set
            {
                _questionText = value;
                OnPropertyChanged();
            }
        }
        
        public string AnswerText
        {
            get { return _answerText; }
            set
            {
                _answerText = value;
                OnPropertyChanged();
            }
        }
        
        public string StatusText
        {
            get { return _statusText; }
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }
        
        public string TokenUsage
        {
            get { return _tokenUsage; }
            set
            {
                _tokenUsage = value;
                OnPropertyChanged();
            }
        }
        
        public bool IsProcessing
        {
            get { return _isProcessing; }
            set
            {
                _isProcessing = value;
                OnPropertyChanged();
                OnPropertyChanged("CanExecuteCommands");
                OnPropertyChanged("IsNotProcessing");
            }
        }
        
        public bool CanExecuteCommands 
        {
            get { return !IsProcessing; }
        }
        
        // 添加窗口置顶属性
        public bool IsWindowTopmost
        {
            get { return _isWindowTopmost; }
            set
            {
                _isWindowTopmost = value;
                OnPropertyChanged();
            }
        }
        
        // 兼容性属性：与UI绑定的IsNotProcessing属性
        public bool IsNotProcessing
        {
            get { return !IsProcessing; }
        }
        
        // 参考选项相关属性
        public ObservableCollection<ChoiceOption> ChoiceOptions
        {
            get { return _choiceOptions; }
            set
            {
                _choiceOptions = value;
                OnPropertyChanged();
            }
        }
        
        public Visibility ChoiceOptionsVisibility
        {
            get { return _choiceOptionsVisibility; }
            set
            {
                _choiceOptionsVisibility = value;
                OnPropertyChanged();
            }
        }
        
        public Visibility JudgmentAnswerVisibility
        {
            get { return _judgmentAnswerVisibility; }
            set
            {
                _judgmentAnswerVisibility = value;
                OnPropertyChanged();
            }
        }
        
        public Visibility OtherAnswerVisibility
        {
            get { return _otherAnswerVisibility; }
            set
            {
                _otherAnswerVisibility = value;
                OnPropertyChanged();
            }
        }
        
        public Visibility EmptyStateVisibility
        {
            get { return _emptyStateVisibility; }
            set
            {
                _emptyStateVisibility = value;
                OnPropertyChanged();
            }
        }
        
        public string JudgmentIcon
        {
            get { return _judgmentIcon; }
            set
            {
                _judgmentIcon = value;
                OnPropertyChanged();
            }
        }
        
        public string JudgmentText
        {
            get { return _judgmentText; }
            set
            {
                _judgmentText = value;
                OnPropertyChanged();
            }
        }
        
        public string FinalAnswerText
        {
            get { return _finalAnswerText; }
            set
            {
                _finalAnswerText = value;
                OnPropertyChanged();
            }
        }
        
        // 快捷键和固定区域OCR相关属性
        public bool HasScreenshot
        {
            get { return _hasScreenshot; }
            private set
            {
                _hasScreenshot = value;
                OnPropertyChanged();
                // 通知相关命令更新状态
                var lockCommand = LockAreaCommand as Utils.RelayCommand;
                if (lockCommand != null)
                {
                    lockCommand.RaiseCanExecuteChanged();
                }
            }
        }
        
        public bool IsAreaLocked
        {
            get
            {
                try
                {
                    return _configService != null && _configService.Config != null && _configService.Config.FixedArea != null && _configService.Config.FixedArea.IsEnabled;
                }
                catch
                {
                    return false;
                }
            }
        }
        
        private Rectangle? SelectedArea
        {
            get { return _selectedArea; }
            set
            {
                _selectedArea = value;
                // 通知相关命令更新状态
                var lockCommand = LockAreaCommand as Utils.RelayCommand;
                if (lockCommand != null)
                {
                    lockCommand.RaiseCanExecuteChanged();
                }
            }
        }
        
        // 锁定区域预览相关字段
        private BitmapSource _lockedAreaPreview = null;
        
        // 题目类型选择属性
        public string SelectedQuestionType
        {
            get { return _selectedQuestionType; }
            set
            {
                _selectedQuestionType = value;
                OnPropertyChanged();
            }
        }
        
        // 锁定区域预览属性
        public BitmapSource LockedAreaPreview
        {
            get { return _lockedAreaPreview; }
            set
            {
                _lockedAreaPreview = value;
                OnPropertyChanged();
            }
        }
        
        #endregion
        
        #region Commands
        
        public ICommand GetAnswerCommand { get; private set; }
        public ICommand TranslateCommand { get; private set; }
        public ICommand ExplainCommand { get; private set; }
        public ICommand BatchOCRCommand { get; private set; }
        public ICommand CopyAnswerCommand { get; private set; }
        public ICommand ClearCommand { get; private set; }
        public ICommand SettingsCommand { get; private set; }
        public ICommand TestConnectionCommand { get; private set; }
        public ICommand ScreenshotCommand { get; private set; }
        public ICommand ClearHistoryCommand { get; private set; }
        public ICommand LockAreaCommand { get; private set; }
        public ICommand QuickOCRCommand { get; private set; }
        
        #endregion
        
        #region Constructor
        
        public MainViewModel(IAIService aiService, ILogService logService, IScreenshotService screenshotService, IOCRService ocrService, IHotkeyService hotkeyService, IConfigService configService)
        {
            
            if (aiService == null)
            {
                throw new ArgumentNullException("aiService");
            }
            if (logService == null)
            {
                throw new ArgumentNullException("logService");
            }
            if (screenshotService == null)
            {
                throw new ArgumentNullException("screenshotService");
            }
            if (ocrService == null)
            {
                throw new ArgumentNullException("ocrService");
            }
            if (hotkeyService == null)
            {
                throw new ArgumentNullException("hotkeyService");
            }
            if (configService == null)
            {
                throw new ArgumentNullException("configService");
            }
            
            _aiService = aiService;
            
            _logService = logService;
            _logService.Info("MainViewModel构造函数开始执行");
            
            _screenshotService = screenshotService;
            
            _ocrService = ocrService;
            
            _hotkeyService = hotkeyService;
            
            _configService = configService;
            _logService.Info("所有服务引用设置完成");
            
            // 初始化命令
            _logService.Info("开始初始化命令");
            
            GetAnswerCommand = new Utils.RelayCommand(() => ExecuteGetAnswer(null), () => CanExecuteGetAnswer(null));
            TranslateCommand = new Utils.RelayCommand(() => ExecuteTranslate(null), () => CanExecuteGetAnswer(null));
            ExplainCommand = new Utils.RelayCommand(() => ExecuteExplain(null), () => CanExecuteGetAnswer(null));
            BatchOCRCommand = new Utils.RelayCommand(() => ExecuteBatchOCR(null), () => CanExecuteCommands);
            CopyAnswerCommand = new Utils.RelayCommand(() => ExecuteCopyAnswer(null), () => CanExecuteCopyAnswer(null));
            
            ClearCommand = new Utils.RelayCommand(() => ExecuteClear(null), () => CanExecuteClear(null));
            
            SettingsCommand = new Utils.RelayCommand(() => ExecuteSettings(null), () => CanExecuteSettings(null));
            
            TestConnectionCommand = new Utils.RelayCommand(() => ExecuteTestConnection(null), () => CanExecuteTestConnection(null));
            
            ScreenshotCommand = new Utils.RelayCommand(() => ExecuteScreenshot(null), () => CanExecuteScreenshot(null));
            
            ClearHistoryCommand = new Utils.RelayCommand(() => ExecuteClearHistory(null), () => CanExecuteClearHistory(null));
            
            LockAreaCommand = new Utils.RelayCommand(() => ExecuteLockArea(null), () => CanExecuteLockArea(null));
            
            QuickOCRCommand = new Utils.RelayCommand(() => ExecuteQuickOCR(null), () => CanExecuteQuickOCR(null));
            
            _logService.Info("所有命令初始化完成");
            
            // 初始化参考选项相关属性
            _logService.Info("开始初始化参考选项相关属性");
            
            _choiceOptions = new ObservableCollection<ChoiceOption>();
            _choiceOptionsVisibility = Visibility.Collapsed;
            _judgmentAnswerVisibility = Visibility.Collapsed;
            _otherAnswerVisibility = Visibility.Collapsed;
            _emptyStateVisibility = Visibility.Visible;
            _judgmentIcon = "";
            _judgmentText = "";
            _finalAnswerText = "";
            
            _logService.Info("参考选项相关属性初始化完成");
            
            // 初始化热键
            _logService.Info("开始初始化热键...");
            try
            {
                InitializeHotkeys();
                _logService.Info("热键初始化完成");
            }
            catch (Exception hotkeyEx)
            {
                _logService.Error(hotkeyEx, "热键初始化失败: {0}", hotkeyEx.Message);
                // 不抛出异常，允许应用程序继续运行
            }
            
            // 订阅配置变化：设置改快捷键后自动重注册（免重启）
            try { _configService.ConfigChanged += OnConfigChanged; }
            catch (Exception subEx) { _logService.Warn("订阅配置变化失败: {0}", subEx.Message); }

            _logService.Info("MainViewModel initialized");
        }
        
        #endregion
        
        #region Command Methods
        
        private bool CanExecuteGetAnswer(object parameter)
        {
            return CanExecuteCommands && !string.IsNullOrWhiteSpace(QuestionText);
        }
        
        private async void ExecuteGetAnswer(object parameter)
        {
            try
            {
                IsProcessing = true;
                StatusText = "正在获取答案...";
                
                // 清空上次的回答内容
                AnswerText = "";
                
                // 清空参考选项
                ClearReferenceOptions();
                
                // 设置处理中的提示
                AnswerText = "正在思考中，请稍候...";
                
                // 直接使用QuestionText，不需要创建AIRequest对象
                
                var response = await _aiService.AnswerQuestionAsync(QuestionText);
                
                if (response.IsSuccess)
                {
                    AnswerText = response.Answer ?? "获取答案成功，但内容为空";
                    StatusText = "答案获取成功";
                    TokenUsage = "Token使用情况: 已获取";
                    
                    // 解析答案并提取参考选项
                    ParseAnswerAndExtractReferenceOptions(response.Answer);
                }
                else
                {
                    AnswerText = string.Format("获取答案失败: {0}", response.ErrorMessage);
                    StatusText = "获取答案失败";
                    
                    // 清空参考选项
                    ClearReferenceOptions();
                }
            }
            catch (Exception ex)
            {
                AnswerText = string.Format("发生错误: {0}", ex.Message);
                StatusText = "发生错误";
                _logService.Error(ex, string.Format("GetAnswer error: {0}", ex.Message));
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        #region 智能动作（翻译/解释 —— 把 AI 从单一答题扩展为多种可插拔动作）

        private async void ExecuteTranslate(object parameter)
        {
            await ExecuteActionAsync("你是一位专业翻译。请将下列文本翻译成中文（若原文已是中文，则翻译成英文）。只输出译文，不要附加解释：", "翻译");
        }

        private async void ExecuteExplain(object parameter)
        {
            await ExecuteActionAsync("你是一位知识讲解专家。请对下列内容做名词解释或详细说明，要求条理清晰、分点阐述：", "解释");
        }

        /// <summary>
        /// 通用智能动作：临时切换 systemPrompt 处理 QuestionText，完成后恢复原 prompt
        /// </summary>
        private async System.Threading.Tasks.Task ExecuteActionAsync(string actionPrompt, string actionName)
        {
            if (string.IsNullOrWhiteSpace(QuestionText))
            {
                StatusText = "请先截图识别或输入文本";
                return;
            }
            try
            {
                IsProcessing = true;
                StatusText = "正在" + actionName + "...";
                AnswerText = "";
                ClearReferenceOptions();
                AnswerText = "正在处理中，请稍候...";

                string originalPrompt = (_configService != null && _configService.Config != null) ? (_configService.Config.SystemPrompt ?? "") : "";
                _aiService.SetSystemPrompt(actionPrompt);
                try
                {
                    var response = await _aiService.AnswerQuestionAsync(QuestionText, QuestionType.Text);
                    if (response.IsSuccess)
                    {
                        AnswerText = response.Answer ?? (actionName + "结果为空");
                        StatusText = actionName + "完成";
                    }
                    else
                    {
                        AnswerText = string.Format("{0}失败: {1}", actionName, response.ErrorMessage);
                        StatusText = actionName + "失败";
                    }
                }
                finally
                {
                    _aiService.SetSystemPrompt(originalPrompt);
                }
            }
            catch (Exception ex)
            {
                AnswerText = string.Format("发生错误: {0}", ex.Message);
                StatusText = "发生错误";
                _logService.Error(ex, string.Format("{0} error: {1}", actionName, ex.Message));
            }
            finally
            {
                IsProcessing = false;
            }
        }

        #endregion

        #region 批量 OCR

        /// <summary>
        /// 批量 OCR：多选图片文件，逐张识别，结果汇总到识别文本区
        /// </summary>
        private async void ExecuteBatchOCR(object parameter)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*",
                Title = "选择要 OCR 的图片（可多选）"
            };
            if (dlg.ShowDialog() != true) return;

            var files = dlg.FileNames;
            if (files == null || files.Length == 0) return;

            IsProcessing = true;
            StatusText = "批量识别中...";
            ClearReferenceOptions();
            AnswerText = "";

            var sb = new System.Text.StringBuilder();
            try
            {
                if (!_ocrService.IsInitialized)
                {
                    StatusText = "正在初始化 OCR 引擎...";
                    var initOk = await _ocrService.InitializeAsync();
                    if (!initOk)
                    {
                        AnswerText = "OCR 引擎初始化失败，请检查模型文件";
                        StatusText = "初始化失败";
                        return;
                    }
                }

                int idx = 0;
                foreach (var file in files)
                {
                    idx++;
                    StatusText = "识别中 " + idx + "/" + files.Length + ": " + System.IO.Path.GetFileName(file);
                    try
                    {
                        using (var bmp = new System.Drawing.Bitmap(file))
                        {
                            var res = await _ocrService.RecognizeTextFromBitmapAsync(bmp);
                            sb.AppendLine("=== " + System.IO.Path.GetFileName(file) + " ===");
                            sb.AppendLine(res != null ? res.Text : "(未识别到文本)");
                            sb.AppendLine();
                        }
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine("=== " + System.IO.Path.GetFileName(file) + " ===");
                        sb.AppendLine("[识别失败: " + ex.Message + "]");
                        sb.AppendLine();
                    }
                }

                QuestionText = sb.ToString();
                StatusText = "批量识别完成，共 " + files.Length + " 张，可继续翻译/解释/解答";
            }
            catch (Exception ex)
            {
                AnswerText = "批量识别出错: " + ex.Message;
                StatusText = "批量识别出错";
                _logService.Error(ex, "BatchOCR error: " + ex.Message);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        #endregion

        /// <summary>
        /// 复制处理结果到剪贴板
        /// </summary>
        private bool CanExecuteCopyAnswer(object parameter)
        {
            return !string.IsNullOrWhiteSpace(AnswerText);
        }

        private void ExecuteCopyAnswer(object parameter)
        {
            try
            {
                System.Windows.Clipboard.SetText(AnswerText ?? "");
                StatusText = "结果已复制到剪贴板";
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "复制结果失败: " + ex.Message);
                StatusText = "复制失败";
            }
        }

        private bool CanExecuteClear(object parameter)
        {
            return CanExecuteCommands;
        }
        
        private void ExecuteClear(object parameter)
        {
            QuestionText = "截图 (Alt+Q) · 粘贴 · 或在此输入文字...";
            AnswerText = "识别文本后，选择「翻译 / 解释 / 智能解答」查看处理结果。";
            TokenUsage = "";
            StatusText = "已清空";
            
            // 清空参考选项
            ClearReferenceOptions();
            
            // 清空截图和固定区域状态
            HasScreenshot = false;
            SelectedArea = null;
            
            // 清空预览图片
            LockedAreaPreview = null;
            
            // 禁用固定区域
            try
            {
                if (_configService != null && _configService.Config != null && _configService.Config.FixedArea != null)
                {
                    _configService.Config.FixedArea.IsEnabled = false;
                    Task.Run(async () => await _configService.SaveConfigAsync());
                    OnPropertyChanged("IsAreaLocked");
                    var quickOcrCommand = QuickOCRCommand as Utils.RelayCommand;
                    if (quickOcrCommand != null)
                    {
                        quickOcrCommand.RaiseCanExecuteChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Warn("清空固定区域配置失败: {0}", ex.Message);
            }
            
            _logService.Info("Content cleared");
        }
        
        private bool CanExecuteSettings(object parameter)
        {
            return CanExecuteCommands;
        }
        
        private static bool _isSettingsWindowOpen = false;
        
        private void ExecuteSettings(object parameter)
        {
            try
            {
                // 防止重复打开设置窗口
                if (_isSettingsWindowOpen)
                {
                    _logService.Info("Settings window is already open");
                    return;
                }
                
                _logService.Info("Opening settings window");
                _isSettingsWindowOpen = true;
                
                var settingsWindow = new Ocris.Views.SettingsWindow();
                settingsWindow.Owner = System.Windows.Application.Current.MainWindow;
                settingsWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                
                // 监听窗口关闭事件
                settingsWindow.Closed += (s, e) => { _isSettingsWindowOpen = false; };
                
                settingsWindow.ShowDialog();
                
                _logService.Info("Settings window closed");
            }
            catch (Exception ex)
            {
                _isSettingsWindowOpen = false;
                _logService.Error(ex, "Failed to open settings window: {0}", ex.Message);
                System.Windows.MessageBox.Show("打开设置窗口失败: " + ex.Message, "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private bool CanExecuteTestConnection(object parameter)
        {
            return CanExecuteCommands;
        }
        
        private async void ExecuteTestConnection(object parameter)
        {
            await TestApiConnectionAsync();
        }
        
        private bool CanExecuteClearHistory(object parameter)
        {
            return CanExecuteCommands;
        }
        
        private void ExecuteClearHistory(object parameter)
        {
            try
            {
                _aiService.ClearHistory();
                StatusText = "对话历史已清除";
                _logService.Info("对话历史已清除");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "清除对话历史失败: {0}", ex.Message);
                StatusText = "清除历史失败: " + ex.Message;
            }
        }
        
        private bool CanExecuteScreenshot(object parameter)
        {
            return CanExecuteCommands;
        }
        
        private async void ExecuteScreenshot(object parameter)
        {
            try
            {
                IsProcessing = true;
                StatusText = "正在截图...";
                _logService.Info("开始截图操作");

                var result = await _screenshotService.CaptureInteractiveAreaAsync();
                
                if (result != null && result.IsSuccess)
                {
                    StatusText = "截图成功，正在识别文字...";
                    _logService.Info("截图成功");
                    
                    // 设置截图状态和区域信息
                    HasScreenshot = true;
                    if (result.CaptureRegion != Rectangle.Empty)
                    {
                        SelectedArea = result.CaptureRegion;
                        _logService.Info("截图成功，设置区域: X={0}, Y={1}, Width={2}, Height={3}", 
                            result.CaptureRegion.X, result.CaptureRegion.Y, result.CaptureRegion.Width, result.CaptureRegion.Height);
                    }
                    else
                    {
                        _logService.Warn("截图成功但区域为空");
                    }
                    
                    // 进行OCR识别
                    try
                    {
                        // 确保OCR引擎已初始化
                        if (!_ocrService.IsInitialized)
                        {
                            StatusText = "正在初始化OCR引擎...";
                            var initResult = await _ocrService.InitializeAsync();
                            if (!initResult)
                            {
                                StatusText = "OCR引擎初始化失败，请手动输入题目";
                                QuestionText = "OCR引擎初始化失败，请手动输入题目内容...";
                                return;
                            }
                        }
                        
                        // 从截图的Bitmap对象进行OCR识别
                        var ocrResult = await _ocrService.RecognizeTextFromBitmapAsync(result.Image);
                        
                        if (ocrResult != null && !string.IsNullOrEmpty(ocrResult.Text))
                        {
                            StatusText = string.Format("OCR识别完成，置信度: {0:P1}", ocrResult.Confidence);
                            QuestionText = ocrResult.Text.Trim();
                            _logService.Info("OCR识别成功，识别文字: {0}", ocrResult.Text);
                        }
                        else
                        {
                            StatusText = "OCR识别失败，请手动输入题目";
                            QuestionText = "未识别到文字，请手动输入题目内容...";
                            _logService.Info("OCR识别失败，未识别到文字");
                        }
                    }
                    catch (Exception ocrEx)
                    {
                        StatusText = "OCR识别异常，请手动输入题目";
                        QuestionText = string.Format("OCR识别异常: {0}\n\n请手动输入题目内容...", ocrEx.Message);
                        _logService.Error(ocrEx, "OCR识别异常: {0}", ocrEx.Message);
                    }
                }
                else if (result != null && !result.IsSuccess)
                {
                    StatusText = "截图被取消";
                    _logService.Info("用户取消了截图操作");
                }
                else
                {
                    StatusText = "截图失败";
                    _logService.Error("截图操作失败，返回结果为null");
                }
            }
            catch (Exception ex)
            {
                StatusText = string.Format("截图失败: {0}", ex.Message);
                _logService.Error(ex, "截图操作异常: {0}", ex.Message);
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        private bool CanExecuteLockArea(object parameter)
        {
            // 简化条件，只要有截图就能锁定
            var canExecute = CanExecuteCommands && HasScreenshot;
            _logService.Info("检查锁定区域命令状态: CanExecuteCommands={0}, HasScreenshot={1}, SelectedArea={2}", 
                CanExecuteCommands, HasScreenshot, SelectedArea != null ? "有值" : "空");
            return canExecute;
        }
        
        private void ExecuteLockArea(object parameter)
        {
            try
            {
                if (HasScreenshot)
                {
                    var config = _configService.Config;
                    if (config.FixedArea == null)
                    {
                        config.FixedArea = new Models.FixedAreaSettings();
                    }
                    
                    config.FixedArea.IsEnabled = true;
                    
                    // 如果SelectedArea为空，使用默认区域
                    if (SelectedArea != null && SelectedArea.HasValue)
                    {
                        config.FixedArea.X = SelectedArea.Value.X;
                        config.FixedArea.Y = SelectedArea.Value.Y;
                        config.FixedArea.Width = SelectedArea.Value.Width;
                        config.FixedArea.Height = SelectedArea.Value.Height;
                        _logService.Info("使用选中区域: X={0}, Y={1}, Width={2}, Height={3}", 
                            SelectedArea.Value.X, SelectedArea.Value.Y, SelectedArea.Value.Width, SelectedArea.Value.Height);
                    }
                    else
                    {
                        // 使用默认区域（屏幕中心的300x200区域）
                        var screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
                        var screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
                        config.FixedArea.X = (int)(screenWidth / 2 - 150);
                        config.FixedArea.Y = (int)(screenHeight / 2 - 100);
                        config.FixedArea.Width = 300;
                        config.FixedArea.Height = 200;
                        _logService.Info("使用默认区域: X={0}, Y={1}, Width={2}, Height={3}", 
                            config.FixedArea.X, config.FixedArea.Y, config.FixedArea.Width, config.FixedArea.Height);
                    }
                    
                    Task.Run(async () => await _configService.SaveConfigAsync());
                    
                    OnPropertyChanged("IsAreaLocked");
                    var quickOcrCommand = QuickOCRCommand as Utils.RelayCommand;
                    if (quickOcrCommand != null)
                    {
                        quickOcrCommand.RaiseCanExecuteChanged();
                    }
                    
                    // 更新预览图片
                    UpdateLockedAreaPreview();
                    
                    StatusText = "已锁定区域，按F4可快速识别";
                    
                    // 强制刷新所有命令状态
                    RefreshCommandStates();
                    
                    // 记录日志时检查SelectedArea是否为空
                    if (SelectedArea.HasValue)
                    {
                        _logService.Info("区域已锁定: X={0}, Y={1}, Width={2}, Height={3}", 
                            SelectedArea.Value.X, SelectedArea.Value.Y, SelectedArea.Value.Width, SelectedArea.Value.Height);
                    }
                    else
                    {
                        _logService.Info("区域已锁定: 使用默认区域 X={0}, Y={1}, Width={2}, Height={3}", 
                            config.FixedArea.X, config.FixedArea.Y, config.FixedArea.Width, config.FixedArea.Height);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText = string.Format("锁定区域失败: {0}", ex.Message);
                _logService.Error(ex, "锁定区域失败: {0}", ex.Message);
            }
        }
        
        private bool CanExecuteQuickOCR(object parameter)
        {
            return CanExecuteCommands && IsAreaLocked;
        }
        
        private async void ExecuteQuickOCR(object parameter)
        {
            try
            {
                if (!IsAreaLocked)
                {
                    StatusText = "请先锁定区域";
                    return;
                }
                
                IsProcessing = true;
                StatusText = "正在快速识别...";
                _logService.Info("开始快速识别操作");
                
                var config = _configService.Config.FixedArea;
                
                // 检查配置有效性
                if (config.Width <= 0 || config.Height <= 0)
                {
                    StatusText = "固定区域配置无效，请重新锁定";
                    return;
                }
                
                // 对固定区域进行截图
                var engine = ServiceContainer.Instance.Resolve<IScreenshotEngine>();
                var bitmap = engine.CaptureFixedArea(config.X, config.Y, config.Width, config.Height);
                
                if (bitmap != null)
                {
                    StatusText = "截图成功，正在识别文字...";
                    
                    // 确保OCR引擎已初始化
                    if (!_ocrService.IsInitialized)
                    {
                        StatusText = "正在初始化OCR引擎...";
                        var initResult = await _ocrService.InitializeAsync();
                        if (!initResult)
                        {
                            StatusText = "OCR引擎初始化失败";
                            return;
                        }
                    }
                    
                    // 进行OCR识别
                    var ocrResult = await _ocrService.RecognizeTextFromBitmapAsync(bitmap);
                    
                    if (ocrResult != null && !string.IsNullOrEmpty(ocrResult.Text))
                    {
                        QuestionText = ocrResult.Text.Trim();
                        StatusText = string.Format("识别成功，置信度: {0:P1}，正在获取答案...", ocrResult.Confidence);
                        _logService.Info("快速识别成功，识别文字: {0}", ocrResult.Text);
                        
                        // 自动获取答案
                        ExecuteGetAnswer(null);
                    }
                    else
                    {
                        StatusText = "未识别到文字，请检查区域设置";
                        _logService.Info("快速识别失败，未识别到文字");
                    }
                    
                    bitmap.Dispose();
                }
                else
                {
                    StatusText = "截图失败，请检查区域设置";
                    _logService.Error("快速截图失败，返回结果为null");
                }
            }
            catch (Exception ex)
            {
                StatusText = string.Format("快速识别失败: {0}", ex.Message);
                _logService.Error(ex, "快速识别失败: {0}", ex.Message);
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// 解析答案并提取参考选项
        /// </summary>
        /// <param name="answer">AI返回的答案</param>
        private void ParseAnswerAndExtractReferenceOptions(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
            {
                ClearReferenceOptions();
                return;
            }
            
            try
            {
                // 检测题目类型并解析
                if (IsChoiceQuestion(answer))
                {
                    ParseChoiceQuestion(answer);
                }
                else if (IsJudgmentQuestion(answer))
                {
                    ParseJudgmentQuestion(answer);
                }
                else
                {
                    ParseOtherQuestion(answer);
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "解析答案时发生错误: {0}", ex.Message);
                ClearReferenceOptions();
            }
        }
        
        /// <summary>
        /// 判断是否为选择题
        /// </summary>
        private bool IsChoiceQuestion(string answer)
        {
            // 检查是否包含选项标识符 A、B、C、D等
            var choicePattern = @"[A-Z][.、：:]";
            return Regex.IsMatch(answer, choicePattern) && 
                   (answer.Contains("选择") || answer.Contains("答案") || 
                    Regex.IsMatch(answer, @"[A-Z][.、：:].{1,200}"));
        }
        
        /// <summary>
        /// 判断是否为判断题
        /// </summary>
        private bool IsJudgmentQuestion(string answer)
        {
            return answer.Contains("正确") || answer.Contains("错误") || 
                   answer.Contains("对") || answer.Contains("错") ||
                   answer.Contains("是") || answer.Contains("否") ||
                   answer.Contains("True") || answer.Contains("False") ||
                   answer.Contains("√") || answer.Contains("×");
        }
        
        /// <summary>
        /// 解析选择题
        /// </summary>
        private void ParseChoiceQuestion(string answer)
        {
            // 提取正确答案 - 支持多选题（如：A、AB、ACD等）
            var correctAnswerPattern = @"答案[是为：:]*\s*([A-Z]+)";
            var correctMatch = Regex.Match(answer, correctAnswerPattern);
            
            if (correctMatch.Success)
            {
                string correctAnswer = correctMatch.Groups[1].Value;
                
                // 显示正确答案选项（支持多选：A、AB、ACD等）
                FinalAnswerText = correctAnswer;
                
                ChoiceOptionsVisibility = Visibility.Collapsed;
                JudgmentAnswerVisibility = Visibility.Collapsed;
                OtherAnswerVisibility = Visibility.Visible;
                EmptyStateVisibility = Visibility.Collapsed;
            }
            else
            {
                // 如果无法提取到明确答案，尝试从选项中找到正确答案
                var optionPattern = @"([A-Z])[.、：:]\s*(.+?)(?=[A-Z][.、：:]|$)";
                var matches = Regex.Matches(answer, optionPattern, RegexOptions.Singleline);
                
                // 尝试其他答案模式 - 支持多选
                var alternativePattern = @"正确答案[是为：:]*\s*([A-Z]+)";
                var alternativeMatch = Regex.Match(answer, alternativePattern);
                
                if (alternativeMatch.Success)
                {
                    FinalAnswerText = alternativeMatch.Groups[1].Value;
                    
                    ChoiceOptionsVisibility = Visibility.Collapsed;
                    JudgmentAnswerVisibility = Visibility.Collapsed;
                    OtherAnswerVisibility = Visibility.Visible;
                    EmptyStateVisibility = Visibility.Collapsed;
                }
                else
                {
                    ParseOtherQuestion(answer);
                }
            }
        }
        
        /// <summary>
        /// 解析判断题
        /// </summary>
        private void ParseJudgmentQuestion(string answer)
        {
            string judgmentText = "";
            string judgmentIcon = "";
            
            // 判断答案，使用简化的显示
            if (answer.Contains("正确") || answer.Contains("对") || 
                answer.Contains("是") || answer.Contains("True") || answer.Contains("√"))
            {
                judgmentText = "正确";
                judgmentIcon = "✓";
            }
            else if (answer.Contains("错误") || answer.Contains("错") || 
                     answer.Contains("否") || answer.Contains("False") || answer.Contains("×"))
            {
                judgmentText = "错误";
                judgmentIcon = "✗";
            }
            else
            {
                // 如果无法明确判断，显示为其他类型
                ParseOtherQuestion(answer);
                return;
            }
            
            // 设置判断题显示
            JudgmentText = judgmentText;
            JudgmentIcon = judgmentIcon;
            
            ChoiceOptionsVisibility = Visibility.Collapsed;
            JudgmentAnswerVisibility = Visibility.Visible;
            OtherAnswerVisibility = Visibility.Collapsed;
            EmptyStateVisibility = Visibility.Collapsed;
        }
        
        /// <summary>
        /// 解析其他类型题目
        /// </summary>
        private void ParseOtherQuestion(string answer)
        {
            // 提取最终答案
            var answerPattern = @"答案[是为：:]*\s*(.+?)(?:\n|$)";
            var match = Regex.Match(answer, answerPattern);
            
            string finalAnswer = "";
            if (match.Success)
            {
                finalAnswer = match.Groups[1].Value.Trim();
            }
            else
            {
                // 如果没有明确的答案标识，取最后一行或最后一句
                var lines = answer.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0)
                {
                    finalAnswer = lines.Last().Trim();
                }
            }
            
            if (string.IsNullOrEmpty(finalAnswer))
            {
                finalAnswer = "请查看详细解答";
            }
            
            FinalAnswerText = finalAnswer;
            
            ChoiceOptionsVisibility = Visibility.Collapsed;
            JudgmentAnswerVisibility = Visibility.Collapsed;
            OtherAnswerVisibility = Visibility.Visible;
            EmptyStateVisibility = Visibility.Collapsed;
        }
        
        /// <summary>
        /// 更新锁定区域预览
        /// </summary>
        private void UpdateLockedAreaPreview()
        {
            try
            {
                if (!IsAreaLocked)
                {
                    LockedAreaPreview = null;
                    return;
                }
                
                var config = _configService.Config.FixedArea;
                if (config == null || config.Width <= 0 || config.Height <= 0)
                {
                    LockedAreaPreview = null;
                    return;
                }
                
                // 使用截图引擎截取锁定区域
                var engine = ServiceContainer.Instance.Resolve<IScreenshotEngine>();
                var bitmap = engine.CaptureFixedArea(config.X, config.Y, config.Width, config.Height);
                
                if (bitmap != null)
                {
                    LockedAreaPreview = ConvertBitmapToBitmapSource(bitmap);
                    bitmap.Dispose();
                }
                else
                {
                    LockedAreaPreview = null;
                }
            }
            catch (Exception ex)
            {
                _logService.Warn("更新锁定区域预览失败: {0}", ex.Message);
                LockedAreaPreview = null;
            }
        }
        
        /// <summary>
        /// 将Bitmap转换为BitmapSource
        /// </summary>
        /// <param name="bitmap">Bitmap对象</param>
        /// <returns>BitmapSource对象</returns>
        private BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
        {
            if (bitmap == null)
                return null;
                
            try
            {
                var hBitmap = bitmap.GetHbitmap();
                try
                {
                    return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
                finally
                {
                    // 释放非托管资源
                    DeleteObject(hBitmap);
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Bitmap转换为BitmapSource失败: {0}", ex.Message);
                return null;
            }
        }
        
        /// <summary>
        /// 删除GDI对象
        /// </summary>
        /// <param name="hObject">GDI对象句柄</param>
        /// <returns>是否成功</returns>
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
        
        /// <summary>
        /// 刷新所有命令状态
        /// </summary>
        private void RefreshCommandStates()
        {
            try
            {
                var lockCommand = LockAreaCommand as Utils.RelayCommand;
                if (lockCommand != null)
                {
                    lockCommand.RaiseCanExecuteChanged();
                }
                
                var quickOcrCommand = QuickOCRCommand as Utils.RelayCommand;
                if (quickOcrCommand != null)
                {
                    quickOcrCommand.RaiseCanExecuteChanged();
                }
                
                _logService.Info("刷新命令状态完成");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "刷新命令状态失败: {0}", ex.Message);
            }
        }
        
        /// <summary>
        /// 清空参考选项
        /// </summary>
        private void ClearReferenceOptions()
        {
            ChoiceOptions.Clear();
            ChoiceOptionsVisibility = Visibility.Collapsed;
            JudgmentAnswerVisibility = Visibility.Collapsed;
            OtherAnswerVisibility = Visibility.Collapsed;
            EmptyStateVisibility = Visibility.Visible;
            
            JudgmentIcon = "";
            JudgmentText = "";
            FinalAnswerText = "";
        }
        
        private async Task TestApiConnectionAsync()
        {
            try
            {
                IsProcessing = true;
                StatusText = "正在测试连接...";
                
                // 直接使用字符串进行测试连接
                
                var response = await _aiService.AnswerQuestionAsync("测试连接");
                
                if (response.IsSuccess)
                {
                    StatusText = "连接测试成功";
                    _logService.Info("API connection test successful");
                }
                else
                {
                    StatusText = string.Format("连接测试失败: {0}", response.ErrorMessage);
                    _logService.Error(string.Format("API connection test failed: {0}", response.ErrorMessage));
                }
            }
            catch (Exception ex)
            {
                StatusText = string.Format("连接测试错误: {0}", ex.Message);
                _logService.Error(ex, string.Format("API connection test error: {0}", ex.Message));
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        #endregion
        
        #region Hotkey Methods
        
        /// <summary>
        /// 配置变化时重新注册热键（用户在设置改快捷键后即时生效，免重启）
        /// </summary>
        private void OnConfigChanged(object sender, EventArgs e)
        {
            try
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        _hotkeyService.UnregisterAll();
                        InitializeHotkeys();
                        StatusText = "配置已更新，热键已重新注册";
                    }
                    catch (Exception ex)
                    {
                        _logService.Error(ex, "热键重注册失败: {0}", ex.Message);
                    }
                }));
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "OnConfigChanged 异常: {0}", ex.Message);
            }
        }

        private void InitializeHotkeys()
        {
            try
            {
                _logService.Info("开始注册热键（从配置读取）...");

                // 绑定热键事件（在注册之前绑定）
                _hotkeyService.HotkeyPressed += OnHotkeyPressed;
                _hotkeyService.HotkeyRegistrationFailed += OnHotkeyRegistrationFailed;

                // 从 config.json -> Hotkeys 读取并解析热键
                var hk = _configService.Config.Hotkeys;
                RegisterFromConfig("screenshot", "截图", hk.Screenshot, "快速截图并识别文字");
                RegisterFromConfig("toggle_window", "唤醒窗口", hk.ToggleWindow, "显示/隐藏主窗口");
                RegisterFromConfig("settings", "设置", hk.Settings, "打开设置窗口");
                RegisterFromConfig("exit", "退出", hk.Exit, "退出程序");
                RegisterFromConfig("clear_history", "清除历史", hk.Clear, "清除对话历史");
                RegisterFromConfig("quick_ocr", "快速识别", hk.QuickOCR, "快速识别固定区域");

                _logService.Info("热键初始化完成");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "热键初始化失败: {0}", ex.Message);
            }
        }

        /// <summary>
        /// 从配置字符串解析并注册热键
        /// </summary>
        private void RegisterFromConfig(string id, string name, string hotkeyStr, string description)
        {
            if (string.IsNullOrWhiteSpace(hotkeyStr))
            {
                _logService.Warn("热键 [{0}] 未配置，跳过注册", name);
                return;
            }
            ModifierKeys modifiers;
            Key key;
            if (!Ocris.Utils.HotkeyParser.TryParse(hotkeyStr, out modifiers, out key))
            {
                _logService.Error("热键 [{0}] 配置无效：{1}，跳过注册", name, hotkeyStr);
                return;
            }
            RegisterHotkeyWithRetry(id, name, modifiers, key, description);
        }
        
        private void RegisterHotkeyWithRetry(string id, string name, ModifierKeys modifiers, Key key, string description, int maxRetries = 3)
        {
            int retryCount = 0;
            bool success = false;
            
            while (retryCount < maxRetries && !success)
            {
                try
                {
                    _logService.Info("尝试注册热键 '{0}' ({1}+{2})，第 {3} 次尝试", name, modifiers, key, retryCount + 1);
                    
                    // 检查热键是否可用
                    if (!_hotkeyService.IsHotkeyAvailable(modifiers, key))
                    {
                        _logService.Warn("热键组合 {0}+{1} 已被占用，尝试替代方案", modifiers, key);
                        
                        // 尝试替代热键组合
                        if (TryAlternativeHotkey(id, name, ref modifiers, ref key, description))
                        {
                            continue; // 使用新的热键组合重试
                        }
                        else
                        {
                            _logService.Error("无法找到可用的热键组合替代方案");
                            break;
                        }
                    }
                    
                    success = _hotkeyService.RegisterHotkey(id, name, modifiers, key, description);
                    
                    if (success)
                    {
                        _logService.Info("热键 '{0}' ({1}+{2}) 注册成功", name, modifiers, key);
                        
                        // 验证热键是否真正活动
                        if (_hotkeyService.IsHotkeyActive(id))
                        {
                            _logService.Info("热键 '{0}' 状态验证通过，处于活动状态", name);
                        }
                        else
                        {
                            _logService.Warn("热键 '{0}' 注册成功但状态验证失败", name);
                        }
                    }
                    else
                    {
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            _logService.Warn("热键 '{0}' 注册失败，{1} 秒后重试", name, retryCount);
                            System.Threading.Thread.Sleep(retryCount * 1000); // 递增延迟
                        }
                    }
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logService.Error(ex, "热键 '{0}' 注册异常，第 {1} 次尝试失败: {2}", name, retryCount, ex.Message);
                    
                    if (retryCount < maxRetries)
                    {
                        System.Threading.Thread.Sleep(retryCount * 1000);
                    }
                }
            }
            
            if (!success)
            {
                _logService.Error("热键 '{0}' 注册最终失败，已尝试 {1} 次", name, maxRetries);
            }
        }
        
        private bool TryAlternativeHotkey(string id, string name, ref ModifierKeys modifiers, ref Key key, string description)
        {
            // 定义替代热键方案
            var alternatives = new[]
            {
                new { Modifiers = ModifierKeys.Control | ModifierKeys.Alt, Key = Key.Q },
                new { Modifiers = ModifierKeys.Shift | ModifierKeys.Alt, Key = Key.Q },
                new { Modifiers = ModifierKeys.Alt, Key = Key.S },
                new { Modifiers = ModifierKeys.Control | ModifierKeys.Shift, Key = Key.Q }
            };
            
            foreach (var alt in alternatives)
            {
                if (_hotkeyService.IsHotkeyAvailable(alt.Modifiers, alt.Key))
                {
                    _logService.Info("找到可用的替代热键组合: {0}+{1}", alt.Modifiers, alt.Key);
                    modifiers = alt.Modifiers;
                    key = alt.Key;
                    return true;
                }
            }
            
            return false;
        }
        
        private void OnHotkeyPressed(string hotkeyId)
        {
            try
            {
                _logService.Info("热键触发: {0}", hotkeyId);
                
                // 根据热键ID执行相应操作
                switch (hotkeyId)
                {
                    case "screenshot":
                        // 在UI线程中执行截图命令
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (CanExecuteScreenshot(null))
                            {
                                ExecuteScreenshot(null);
                            }
                        }));
                        break;
                    case "toggle_window":
                        // 显示/隐藏主窗口
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            var mainWindow = Application.Current.MainWindow;
                            if (mainWindow != null)
                            {
                                if (mainWindow.WindowState == WindowState.Minimized || !mainWindow.IsVisible)
                                {
                                    mainWindow.Show();
                                    mainWindow.WindowState = WindowState.Normal;
                                    mainWindow.Activate();
                                }
                                else
                                {
                                    mainWindow.Hide();
                                }
                            }
                        }));
                        break;
                    case "settings":
                        // 打开设置窗口
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (CanExecuteSettings(null))
                            {
                                ExecuteSettings(null);
                            }
                        }));
                        break;
                    case "exit":
                        // 退出程序
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            Application.Current.Shutdown();
                        }));
                        break;
                    case "clear_history":
                        // 清除对话历史
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (CanExecuteClearHistory(null))
                            {
                                ExecuteClearHistory(null);
                            }
                        }));
                        break;
                    case "quick_ocr":
                        // 快速识别
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (CanExecuteQuickOCR(null))
                            {
                                ExecuteQuickOCR(null);
                            }
                        }));
                        break;
                    default:
                        _logService.Warn("未知的热键ID: {0}", hotkeyId);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "热键处理失败: {0}", ex.Message);
            }
        }
        
        private void OnHotkeyRegistrationFailed(string reason)
        {
            _logService.Error("热键注册失败: {0}", reason);
            // 可以在这里显示用户通知
        }
        
        #endregion
        
        #region INotifyPropertyChanged
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        #endregion
    }
}