using System;
using System.Windows.Input;

namespace AIAnswerTool.Utils
{
    /// <summary>
    /// 简单的命令实现，用于MVVM模式
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// 初始化RelayCommand
        /// </summary>
        /// <param name="execute">执行的动作</param>
        /// <param name="canExecute">是否可以执行的判断</param>
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");
            _execute = execute;
            _canExecute = canExecute;
        }

        /// <summary>
        /// 当CanExecute的返回值可能发生变化时发生
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// 定义确定此命令是否可以在其当前状态下执行的方法
        /// </summary>
        /// <param name="parameter">此命令使用的数据。如果此命令不需要传递数据，则该对象可以设置为null</param>
        /// <returns>如果可以执行此命令，则为true；否则为false</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute != null ? _canExecute.Invoke() : true;
        }

        /// <summary>
        /// 定义在调用此命令时要调用的方法
        /// </summary>
        /// <param name="parameter">此命令使用的数据。如果此命令不需要传递数据，则该对象可以设置为null</param>
        public void Execute(object parameter)
        {
            _execute();
        }
    }

    /// <summary>
    /// 带参数的命令实现
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        /// <summary>
        /// 初始化RelayCommand
        /// </summary>
        /// <param name="execute">执行的动作</param>
        /// <param name="canExecute">是否可以执行的判断</param>
        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");
            _execute = execute;
            _canExecute = canExecute;
        }

        /// <summary>
        /// 当CanExecute的返回值可能发生变化时发生
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// 定义确定此命令是否可以在其当前状态下执行的方法
        /// </summary>
        /// <param name="parameter">此命令使用的数据</param>
        /// <returns>如果可以执行此命令，则为true；否则为false</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute != null ? _canExecute.Invoke((T)parameter) : true;
        }

        /// <summary>
        /// 定义在调用此命令时要调用的方法
        /// </summary>
        /// <param name="parameter">此命令使用的数据</param>
        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }
    }
}