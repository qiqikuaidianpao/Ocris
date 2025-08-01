using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AIAnswerTool.Models;
using AIAnswerTool.Services;
using AIAnswerTool.Utils;

namespace AIAnswerTool.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Fields
        
        private readonly IAIService _aiService;
        private readonly ILogService _logService;
        private readonly IScreenshotService _screenshotService;
        private readonly IOCRService _ocrService;
        private readonly IHotkeyService _hotkeyService;
        
        private string _questionText = "请输入题目或使用截图功能获取题目...";
        private string _answerText = "请输入题目并点击\"获取答案\"按钮...";
        private string _statusText = "就绪";
        private string _tokenUsage = "";
        private bool _isProcessing = false;
        
        // 参考选项相关字段
        private ObservableCollection<ChoiceOption> _choiceOptions = new ObservableCollection<ChoiceOption>();
        private Visibility _choiceOptionsVisibility = Visibility.Collapsed;
        private Visibility _judgmentAnswerVisibility = Visibility.Collapsed;
        private Visibility _otherAnswerVisibility = Visibility.Collapsed;
        private Visibility _emptyStateVisibility = Visibility.Visible;
        private string _judgmentIcon = "";
        private string _judgmentText = "";
        private string _finalAnswerText = "";
        
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
            }
        }
        
        public bool CanExecuteCommands 
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
        
        #endregion
        
        #region Commands
        
        public ICommand GetAnswerCommand { get; private set; }
        public ICommand ClearCommand { get; private set; }
        public ICommand SettingsCommand { get; private set; }
        public ICommand TestConnectionCommand { get; private set; }
        public ICommand ScreenshotCommand { get; private set; }
        public ICommand ClearHistoryCommand { get; private set; }
        
        #endregion
        
        #region Constructor
        
        public MainViewModel(IAIService aiService, ILogService logService, IScreenshotService screenshotService, IOCRService ocrService, IHotkeyService hotkeyService)
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
            _aiService = aiService;
            _logService = logService;
            _screenshotService = screenshotService;
            _ocrService = ocrService;
            _hotkeyService = hotkeyService;
            
            // 初始化命令
            GetAnswerCommand = new RelayCommand(ExecuteGetAnswer, CanExecuteGetAnswer);
            ClearCommand = new RelayCommand(ExecuteClear, CanExecuteClear);
            SettingsCommand = new RelayCommand(ExecuteSettings, CanExecuteSettings);
            TestConnectionCommand = new RelayCommand(ExecuteTestConnection, CanExecuteTestConnection);
            ScreenshotCommand = new RelayCommand(ExecuteScreenshot, CanExecuteScreenshot);
            ClearHistoryCommand = new RelayCommand(ExecuteClearHistory, CanExecuteClearHistory);
            
            // 初始化参考选项相关属性
            _choiceOptions = new ObservableCollection<ChoiceOption>();
            _choiceOptionsVisibility = Visibility.Collapsed;
            _judgmentAnswerVisibility = Visibility.Collapsed;
            _otherAnswerVisibility = Visibility.Collapsed;
            _emptyStateVisibility = Visibility.Visible;
            _judgmentIcon = "";
            _judgmentText = "";
            _finalAnswerText = "";
            
            // 初始化热键
            InitializeHotkeys();
            
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
        
        private bool CanExecuteClear(object parameter)
        {
            return CanExecuteCommands;
        }
        
        private void ExecuteClear(object parameter)
        {
            QuestionText = "请输入题目或使用截图功能获取题目...";
            AnswerText = "请输入题目并点击\"获取答案\"按钮...";
            TokenUsage = "";
            StatusText = "已清空";
            
            // 清空参考选项
            ClearReferenceOptions();
            
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
                
                var settingsWindow = new AIAnswerTool.Views.SettingsWindow();
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
            // 提取正确答案
            var correctAnswerPattern = @"答案[是为：:]*\s*([A-Z])";
            var correctMatch = Regex.Match(answer, correctAnswerPattern);
            
            if (correctMatch.Success)
            {
                string correctAnswer = correctMatch.Groups[1].Value;
                
                // 只显示正确答案选项（如：A）
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
                
                // 尝试其他答案模式
                var alternativePattern = @"正确答案[是为：:]*\s*([A-Z])";
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
            
            // 判断答案，使用简化的显示
            if (answer.Contains("正确") || answer.Contains("对") || 
                answer.Contains("是") || answer.Contains("True") || answer.Contains("√"))
            {
                judgmentText = "对";
            }
            else if (answer.Contains("错误") || answer.Contains("错") || 
                     answer.Contains("否") || answer.Contains("False") || answer.Contains("×"))
            {
                judgmentText = "错";
            }
            else
            {
                // 如果无法明确判断，显示为其他类型
                ParseOtherQuestion(answer);
                return;
            }
            
            // 使用简化显示，只显示"对"或"错"
            FinalAnswerText = judgmentText;
            
            ChoiceOptionsVisibility = Visibility.Collapsed;
            JudgmentAnswerVisibility = Visibility.Collapsed;
            OtherAnswerVisibility = Visibility.Visible;
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
        
        private void InitializeHotkeys()
        {
            try
            {
                // 绑定热键事件（在注册之前绑定）
                _hotkeyService.HotkeyPressed += OnHotkeyPressed;
                _hotkeyService.HotkeyRegistrationFailed += OnHotkeyRegistrationFailed;
                
                // 注册截图热键 (Alt+Q) 带重试机制
                RegisterHotkeyWithRetry("screenshot", "截图", ModifierKeys.Alt, Key.Q, "快速截图并识别文字");
                
                // 注册唤醒窗口热键 (Alt+W)
                RegisterHotkeyWithRetry("toggle_window", "唤醒窗口", ModifierKeys.Alt, Key.W, "显示/隐藏主窗口");
                
                // 注册设置热键 (Alt+S)
                RegisterHotkeyWithRetry("settings", "设置", ModifierKeys.Alt, Key.S, "打开设置窗口");
                
                // 注册退出热键 (Alt+X)
                RegisterHotkeyWithRetry("exit", "退出", ModifierKeys.Alt, Key.X, "退出程序");
                
                // 注册清除历史热键 (Alt+C)
                RegisterHotkeyWithRetry("clear_history", "清除历史", ModifierKeys.Alt, Key.C, "清除对话历史");
                
                _logService.Info("热键初始化完成");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "热键初始化失败: {0}", ex.Message);
            }
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
        
        private void OnHotkeyPressed(object sender, Models.HotkeyEventArgs e)
        {
            try
            {
                _logService.Info("热键触发: {0}", e.HotkeyInfo.Name);
                
                // 根据热键ID执行相应操作
                switch (e.HotkeyInfo.Id)
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
                    default:
                        _logService.Warn("未知的热键ID: {0}", e.HotkeyInfo.Id);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "热键处理失败: {0}", ex.Message);
            }
        }
        
        private void OnHotkeyRegistrationFailed(object sender, string reason)
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