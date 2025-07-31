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
        
        #endregion
        
        #region Constructor
        
        public MainViewModel(IAIService aiService, ILogService logService, IScreenshotService screenshotService, IOCRService ocrService)
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
            _aiService = aiService;
            _logService = logService;
            _screenshotService = screenshotService;
            _ocrService = ocrService;
            
            // 初始化命令
            GetAnswerCommand = new RelayCommand(ExecuteGetAnswer, CanExecuteGetAnswer);
            ClearCommand = new RelayCommand(ExecuteClear, CanExecuteClear);
            SettingsCommand = new RelayCommand(ExecuteSettings, CanExecuteSettings);
            TestConnectionCommand = new RelayCommand(ExecuteTestConnection, CanExecuteTestConnection);
            ScreenshotCommand = new RelayCommand(ExecuteScreenshot, CanExecuteScreenshot);
            
            // 初始化参考选项相关属性
            _choiceOptions = new ObservableCollection<ChoiceOption>();
            _choiceOptionsVisibility = Visibility.Collapsed;
            _judgmentAnswerVisibility = Visibility.Collapsed;
            _otherAnswerVisibility = Visibility.Collapsed;
            _emptyStateVisibility = Visibility.Visible;
            _judgmentIcon = "";
            _judgmentText = "";
            _finalAnswerText = "";
            
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
        
        private void ExecuteSettings(object parameter)
        {
            try
            {
                _logService.Info("Opening settings window");
                
                var settingsWindow = new AIAnswerTool.Views.SettingsWindow();
                settingsWindow.Owner = System.Windows.Application.Current.MainWindow;
                settingsWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                settingsWindow.ShowDialog();
                
                _logService.Info("Settings window closed");
            }
            catch (Exception ex)
            {
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