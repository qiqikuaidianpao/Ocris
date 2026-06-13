using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Ocris.Services;
using Ocris.Models;

namespace Ocris.ViewModels
{
    /// <summary>
    /// 截图窗口视图模型
    /// </summary>
    public class ScreenshotViewModel : INotifyPropertyChanged
    {
        private string _statusText;
        private bool _isSelecting;
        private string _selectionInfo;
        private bool _showToolbar;
        private ScreenshotMode _currentMode;


        /// <summary>
        /// 状态文本
        /// </summary>
        public string StatusText
        {
            get { return _statusText; }
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 是否正在选择
        /// </summary>
        public bool IsSelecting
        {
            get { return _isSelecting; }
            set
            {
                if (_isSelecting != value)
                {
                    _isSelecting = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 选择区域信息
        /// </summary>
        public string SelectionInfo
        {
            get { return _selectionInfo; }
            set
            {
                if (_selectionInfo != value)
                {
                    _selectionInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 是否显示工具栏
        /// </summary>
        public bool ShowToolbar
        {
            get { return _showToolbar; }
            set
            {
                if (_showToolbar != value)
                {
                    _showToolbar = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 当前截图模式
        /// </summary>
        public ScreenshotMode CurrentMode
        {
            get { return _currentMode; }
            set
            {
                if (_currentMode != value)
                {
                    _currentMode = value;
                    OnPropertyChanged();
                    UpdateStatusForMode();
                }
            }
        }



        /// <summary>
        /// 保存命令
        /// </summary>
        public ICommand SaveCommand { get; private set; }

        /// <summary>
        /// 复制命令
        /// </summary>
        public ICommand CopyCommand { get; private set; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public ICommand CancelCommand { get; private set; }

        public ScreenshotViewModel()
        {
            CurrentMode = ScreenshotMode.Smart;
            StatusText = "移动鼠标自动识别窗口，点击选择窗口或拖拽选择区域";
            SelectionInfo = "";
            
            // 初始化命令
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CopyCommand = new RelayCommand(ExecuteCopy, CanExecuteCopy);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        /// <summary>
        /// 更新选择信息
        /// </summary>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        public void UpdateSelectionInfo(double width, double height)
        {
            SelectionInfo = string.Format("区域: {0:F0} × {1:F0}", width, height);
        }

        /// <summary>
        /// 开始选择
        /// </summary>
        public void StartSelection()
        {
            IsSelecting = true;
            StatusText = "正在选择区域...";
            ShowToolbar = false;
        }

        /// <summary>
        /// 完成选择
        /// </summary>
        public void CompleteSelection()
        {
            IsSelecting = false;
            StatusText = "选择完成，请选择操作";
            ShowToolbar = true;
        }

        /// <summary>
        /// 取消选择
        /// </summary>
        public void CancelSelection()
        {
            IsSelecting = false;
            ShowToolbar = false;
            SelectionInfo = "";
            UpdateStatusForMode();
        }

        /// <summary>
        /// 根据当前模式更新状态文本
        /// </summary>
        private void UpdateStatusForMode()
        {
            // 简化版本：只有一种智能模式
            StatusText = "移动鼠标自动识别窗口，点击选择窗口或拖拽选择区域";
        }

        // 倒计时功能已移除，简化后不再需要

        #region 命令实现

        private bool CanExecuteSave(object parameter)
        {
            return !IsSelecting && ShowToolbar;
        }

        private void ExecuteSave(object parameter)
        {
            // 保存逻辑由视图处理
        }

        private bool CanExecuteCopy(object parameter)
        {
            return !IsSelecting && ShowToolbar;
        }

        private void ExecuteCopy(object parameter)
        {
            // 复制逻辑由视图处理
        }

        private void ExecuteCancel(object parameter)
        {
            // 取消逻辑由视图处理
        }



        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }

    /// <summary>
    /// 简单的命令实现
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute != null ? _canExecute.Invoke(parameter) : true;
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}