using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AIAnswerTool.Models
{
    /// <summary>
    /// 选择题选项模型
    /// </summary>
    public class ChoiceOption : INotifyPropertyChanged
    {
        private string _optionLabel;
        private string _optionText;
        private bool _isCorrect;

        /// <summary>
        /// 选项标签 (A, B, C, D等)
        /// </summary>
        public string OptionLabel
        {
            get { return _optionLabel; }
            set
            {
                _optionLabel = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 选项内容
        /// </summary>
        public string OptionText
        {
            get { return _optionText; }
            set
            {
                _optionText = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否为正确答案
        /// </summary>
        public bool IsCorrect
        {
            get { return _isCorrect; }
            set
            {
                _isCorrect = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}