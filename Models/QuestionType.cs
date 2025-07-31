using System;

namespace AIAnswerTool.Models
{
    /// <summary>
    /// 问题类型枚举
    /// </summary>
    public enum QuestionType
    {
        /// <summary>
        /// 文本问题
        /// </summary>
        Text,
        
        /// <summary>
        /// 图片问题
        /// </summary>
        Image,
        
        /// <summary>
        /// 混合问题（文本+图片）
        /// </summary>
        Mixed,
        
        /// <summary>
        /// 选择题
        /// </summary>
        MultipleChoice,
        
        /// <summary>
        /// 判断题
        /// </summary>
        TrueFalse,
        
        /// <summary>
        /// 填空题
        /// </summary>
        FillInBlank,
        
        /// <summary>
        /// 简答题
        /// </summary>
        ShortAnswer,
        
        /// <summary>
        /// 未知类型
        /// </summary>
        Unknown
    }
}