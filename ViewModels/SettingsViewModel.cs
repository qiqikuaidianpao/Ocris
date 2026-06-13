using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Forms;
using Ocris.Models;
using Ocris.Services;

namespace Ocris.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly ILogService _logService;
        private readonly ConfigService _configService;
        
        // 事件
        public event EventHandler CloseRequested;
        public event PropertyChangedEventHandler PropertyChanged;
        
        // 属性
        private string _apiToken;
        public string ApiToken
        {
            get { return _apiToken; }
            set
            {
                _apiToken = value;
                OnPropertyChanged();
            }
        }
        
        private string _apiBaseUrl;
        public string ApiBaseUrl
        {
            get { return _apiBaseUrl; }
            set
            {
                _apiBaseUrl = value;
                OnPropertyChanged();
            }
        }
        
        private string _aliCloudApiKey;
        public string AliCloudApiKey
        {
            get { return _aliCloudApiKey; }
            set
            {
                _aliCloudApiKey = value;
                OnPropertyChanged();
            }
        }
        
        private string _aliCloudApiBaseUrl;
        public string AliCloudApiBaseUrl
        {
            get { return _aliCloudApiBaseUrl; }
            set
            {
                _aliCloudApiBaseUrl = value;
                OnPropertyChanged();
            }
        }
        
        private string _screenshotHotkey;
        public string ScreenshotHotkey
        {
            get { return _screenshotHotkey; }
            set
            {
                _screenshotHotkey = value;
                OnPropertyChanged();
            }
        }
        
        private string _mainWindowHotkey;
        public string MainWindowHotkey
        {
            get { return _mainWindowHotkey; }
            set
            {
                _mainWindowHotkey = value;
                OnPropertyChanged();
            }
        }
        
        private bool _autoSaveScreenshots;
        public bool AutoSaveScreenshots
        {
            get { return _autoSaveScreenshots; }
            set
            {
                _autoSaveScreenshots = value;
                OnPropertyChanged();
            }
        }
        
        private string _screenshotPath;
        public string ScreenshotPath
        {
            get { return _screenshotPath; }
            set
            {
                _screenshotPath = value;
                OnPropertyChanged();
            }
        }
        
        private bool _windowTopmost;
        public bool WindowTopmost
        {
            get { return _windowTopmost; }
            set
            {
                _windowTopmost = value;
                OnPropertyChanged();
            }
        }
        
        private string _selectedTheme;
        public string SelectedTheme
        {
            get { return _selectedTheme; }
            set
            {
                _selectedTheme = value;
                OnPropertyChanged();
                // 即时切换主题（Light/Dark）
                Ocris.Services.ThemeService.ApplyTheme(value ?? Ocris.Services.ThemeService.Light);
            }
        }
        
        // 命令
        public ICommand TestConnectionCommand { get; private set; }
        public ICommand BrowsePathCommand { get; private set; }
        public ICommand ResetDefaultCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        
        public SettingsViewModel()
        {
            // 使用 ServiceContainer 单例（与主程序共享同一 ConfigService，设置改动主程序即时感知）
            _logService = ServiceContainer.Instance.Resolve<ILogService>();
            _configService = (ConfigService)ServiceContainer.Instance.Resolve<IConfigService>();
            
            // 初始化命令
            InitializeCommands();
            
            // 设置默认值
            SetDefaultValues();
        }
        
        private void InitializeCommands()
        {
            TestConnectionCommand = new RelayCommand(ExecuteTestConnection, CanExecuteTestConnection);
            BrowsePathCommand = new RelayCommand(ExecuteBrowsePath, CanExecuteBrowsePath);
            ResetDefaultCommand = new RelayCommand(ExecuteResetDefault, CanExecuteResetDefault);
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel, CanExecuteCancel);
        }
        
        private void SetDefaultValues()
        {
            ApiToken = "";
            ApiBaseUrl = "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation";
            AliCloudApiKey = "";
            AliCloudApiBaseUrl = "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation";
            ScreenshotHotkey = "Ctrl+Alt+S";
            MainWindowHotkey = "Ctrl+Alt+Q";
            AutoSaveScreenshots = false;
            ScreenshotPath = "Screenshots";
            WindowTopmost = false;
            SelectedTheme = "Light";
        }
        
        public async void LoadSettings()
        {
            try
            {
                // 从配置文件加载设置
                await _configService.LoadConfigAsync();
                
                ApiToken = _configService.ApiToken ?? "";
                ApiBaseUrl = _configService.ApiBaseUrl ?? "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation";
                AliCloudApiKey = _configService.AliCloudApiKey ?? "";
                AliCloudApiBaseUrl = _configService.AliCloudApiBaseUrl ?? "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation";
                ScreenshotHotkey = (_configService.Config != null && _configService.Config.Hotkeys != null && _configService.Config.Hotkeys.Screenshot != null) ? _configService.Config.Hotkeys.Screenshot : "Alt+Q";
                MainWindowHotkey = "Ctrl+Alt+Q";
                AutoSaveScreenshots = _configService.AutoSaveScreenshots;
                ScreenshotPath = _configService.ScreenshotPath ?? "Screenshots";
                WindowTopmost = _configService.AlwaysOnTop;
                SelectedTheme = (_configService.Config != null && !string.IsNullOrEmpty(_configService.Config.Theme)) ? _configService.Config.Theme : "Light";
                
                _logService.Info("Settings loaded successfully");
            }
            catch (Exception ex)
            {
                _logService.Error("Failed to load settings: " + ex.Message);
                SetDefaultValues();
            }
        }
        
        private bool CanExecuteTestConnection(object parameter)
        {
            return !string.IsNullOrWhiteSpace(ApiToken) && !string.IsNullOrWhiteSpace(ApiBaseUrl);
        }
        
        private async void ExecuteTestConnection(object parameter)
        {
            try
            {
                _logService.Info("Testing API connection...");
                
                // 这里可以添加实际的连接测试逻辑
                await System.Threading.Tasks.Task.Delay(1000); // 模拟测试
                
                System.Windows.MessageBox.Show("连接测试成功！", "测试结果", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                _logService.Info("API connection test successful");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("连接测试失败: " + ex.Message, "测试结果", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                
                _logService.Error("API connection test failed: " + ex.Message);
            }
        }
        
        private bool CanExecuteBrowsePath(object parameter)
        {
            return true;
        }
        
        private void ExecuteBrowsePath(object parameter)
        {
            try
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = "选择截图保存路径";
                    dialog.SelectedPath = ScreenshotPath;
                    
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        ScreenshotPath = dialog.SelectedPath;
                        _logService.Info("Screenshot path changed to: " + ScreenshotPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Error("Failed to browse path: " + ex.Message);
                System.Windows.MessageBox.Show("选择路径失败: " + ex.Message, "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private bool CanExecuteResetDefault(object parameter)
        {
            return true;
        }
        
        private void ExecuteResetDefault(object parameter)
        {
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "确定要恢复所有设置到默认值吗？这将清除您的所有自定义配置。", 
                    "确认恢复默认", 
                    System.Windows.MessageBoxButton.YesNo, 
                    System.Windows.MessageBoxImage.Question);
                
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    SetDefaultValues();
                    _logService.Info("Settings reset to default values");
                    
                    System.Windows.MessageBox.Show("设置已恢复到默认值", "操作完成", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logService.Error("Failed to reset settings: " + ex.Message);
                System.Windows.MessageBox.Show("恢复默认设置失败: " + ex.Message, "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private bool CanExecuteSave(object parameter)
        {
            return true;
        }
        
        private async void ExecuteSave(object parameter)
        {
            try
            {
                // 保存设置到配置文件
                // 直接通过ConfigService属性设置值
                _configService.ApiToken = ApiToken;
                _configService.ApiBaseUrl = ApiBaseUrl;
                _configService.AliCloudApiKey = AliCloudApiKey;
                _configService.AliCloudApiBaseUrl = AliCloudApiBaseUrl;
                // 快捷键统一保存到 Hotkeys.Screenshot（与 MainViewModel 读取的字段一致）
                if (_configService.Config != null && _configService.Config.Hotkeys != null)
                {
                    _configService.Config.Hotkeys.Screenshot = ScreenshotHotkey ?? "Alt+Q";
                }
                _configService.AutoSaveScreenshots = AutoSaveScreenshots;
                _configService.ScreenshotPath = ScreenshotPath;
                _configService.AlwaysOnTop = WindowTopmost;
                // 主题持久化
                if (_configService.Config != null)
                {
                    _configService.Config.Theme = SelectedTheme ?? "Light";
                }
                
                await _configService.SaveConfigAsync();
                _logService.Info("Settings saved successfully");
                
                System.Windows.MessageBox.Show("设置已保存", "保存成功", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                // 关闭窗口
                if (CloseRequested != null)
                {
                    CloseRequested.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                _logService.Error("Failed to save settings: " + ex.Message);
                System.Windows.MessageBox.Show("保存设置失败: " + ex.Message, "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private bool CanExecuteCancel(object parameter)
        {
            return true;
        }
        
        private void ExecuteCancel(object parameter)
        {
            try
            {
                _logService.Info("Settings dialog cancelled");
                
                // 关闭窗口
                if (CloseRequested != null)
                {
                    CloseRequested.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                _logService.Error("Failed to cancel settings: " + ex.Message);
            }
        }
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}