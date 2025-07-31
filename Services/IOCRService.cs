using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace AIAnswerTool.Services
{
    /// <summary>
    /// OCR识别结果
    /// </summary>
    public class OCRResult
    {
        /// <summary>
        /// 识别的文本内容
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 置信度（0-1）
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// 文本区域
        /// </summary>
        public Rectangle BoundingBox { get; set; }

        /// <summary>
        /// 识别语言
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// 处理时间（毫秒）
        /// </summary>
        public long ProcessingTime { get; set; }

        /// <summary>
        /// 文本块列表
        /// </summary>
        public List<TextBlock> TextBlocks { get; set; }
    }

    /// <summary>
    /// OCR文本块
    /// </summary>
    public class TextBlock
    {
        /// <summary>
        /// 文本内容
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 文本位置
        /// </summary>
        public Rectangle BoundingBox { get; set; }

        /// <summary>
        /// 置信度
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// 行号
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// 文本框的四个角点坐标
        /// </summary>
        public System.Drawing.Point[] BoxPoints { get; set; }
    }

    /// <summary>
    /// OCR服务接口
    /// </summary>
    public interface IOCRService
    {
        /// <summary>
        /// 初始化OCR引擎
        /// </summary>
        /// <returns>初始化是否成功</returns>
        Task<bool> InitializeAsync();



        /// <summary>
        /// 识别图像中的文本（详细结果）
        /// </summary>
        /// <param name="image">图像</param>
        /// <returns>文本块列表</returns>
        Task<List<TextBlock>> RecognizeTextBlocksAsync(Bitmap image);

        /// <summary>
        /// 识别图像中的文本
        /// </summary>
        /// <param name="image">包含要识别文本的Bitmap对象</param>
        /// <returns>OCR识别结果</returns>
        Task<OCRResult> RecognizeTextFromBitmapAsync(Bitmap image);

        /// <summary>
        /// 识别文件中的文本
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>OCR结果</returns>
        [Obsolete("Use RecognizeTextFromBitmapAsync instead as it is more efficient.")]
        Task<OCRResult> RecognizeTextFromFileAsync(string filePath);

        /// <summary>
        /// 设置识别语言
        /// </summary>
        /// <param name="language">语言代码（如：zh-cn, en）</param>
        void SetLanguage(string language);

        /// <summary>
        /// 获取支持的语言列表
        /// </summary>
        /// <returns>语言列表</returns>
        List<string> GetSupportedLanguages();

        /// <summary>
        /// 预处理图像以提高识别准确率
        /// </summary>
        /// <param name="image">原始图像</param>
        /// <returns>处理后的图像</returns>
        Bitmap PreprocessImage(Bitmap image, ILogService logService);



        /// <summary>
        /// OCR引擎是否已初始化
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 当前使用的语言
        /// </summary>
        string CurrentLanguage { get; }

        /// <summary>
        /// OCR识别完成事件
        /// </summary>
        event EventHandler<OCRResult> TextRecognized;
    }
}