using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PaddleOCRSharp;
using Ocris.Utils;
using Ocris.Services;

namespace Ocris.Services
{
    /// <summary>
    /// OCR文字识别服务实现
    /// </summary>
    public class OCRService : IOCRService
    {
        private readonly ILogService _logService;
        private readonly IConfigService _configService;
        private PaddleOCREngine _ocrEngine;
        private OCRParameter _ocrParameter;
        private bool _isInitialized;
        private string _currentLanguage;

        public bool IsInitialized { get { return _isInitialized; } }
        public string CurrentLanguage { get { return _currentLanguage; } }

        public event EventHandler<OCRResult> TextRecognized;

        public OCRService(ILogService logService, IConfigService configService)
        {
            if (logService == null)
                throw new ArgumentNullException("logService");
            if (configService == null)
                throw new ArgumentNullException("configService");
            _logService = logService;
            _configService = configService;
            _currentLanguage = "zh-cn";
        }

        /// <summary>
        /// 初始化OCR引擎
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                _logService.Info("正在初始化OCR引擎...");

                // 创建OCR参数配置
                _ocrParameter = new OCRParameter()
                {
                    use_gpu = false,
                    gpu_id = 0,
                    cpu_math_library_num_threads = 10,
                    enable_mkldnn = true,
                    cls = true,
                    det = true,
                    rec = true,
                    use_angle_cls = true,
                    det_db_score_mode = true,
                    max_side_len = 1280,
                    det_db_thresh = 0.3f,
                    det_db_box_thresh = 0.5f,
                    det_db_unclip_ratio = 2.0f,
                    cls_thresh = 0.9f,
                    rec_batch_num = 6,
                    rec_img_h = 48,
                    rec_img_w = 320
                };

                // 获取模型路径配置
                var appPath = AppDomain.CurrentDomain.BaseDirectory;
                var modelPath = Path.Combine(appPath, "inference");
                var detModelPath = Path.Combine(modelPath, "ch_PP-OCRv4_det_infer");
                var clsModelPath = Path.Combine(modelPath, "ch_ppocr_mobile_v2.0_cls_infer");
                var recModelPath = Path.Combine(modelPath, "ch_PP-OCRv4_rec_infer");
                var keysPath = Path.Combine(modelPath, "ppocr_keys.txt");
                
                // 检查模型文件是否存在
                if (!Directory.Exists(detModelPath))
                {
                    _logService.Error("检测模型路径不存在: {0}", detModelPath);
                    return false;
                }
                if (!Directory.Exists(clsModelPath))
                {
                    _logService.Error("分类模型路径不存在: {0}", clsModelPath);
                    return false;
                }
                if (!Directory.Exists(recModelPath))
                {
                    _logService.Error("识别模型路径不存在: {0}", recModelPath);
                    return false;
                }
                if (!File.Exists(keysPath))
                {
                    _logService.Error("字典文件不存在: {0}", keysPath);
                    return false;
                }

                // 创建OCR模型配置
                var ocrModel = new OCRModelConfig()
                {
                    det_infer = detModelPath,
                    cls_infer = clsModelPath,
                    rec_infer = recModelPath,
                    keys = keysPath
                };

                _logService.Info("OCR模型路径配置: det={0}, cls={1}, rec={2}, keys={3}", 
                    detModelPath, clsModelPath, recModelPath, keysPath);

                // 创建OCR引擎
                _logService.Info("正在创建PaddleOCR引擎...");
                _ocrEngine = new PaddleOCREngine(ocrModel, _ocrParameter);
                
                // 验证引擎是否创建成功
                if (_ocrEngine == null)
                {
                    _logService.Error("PaddleOCR引擎创建失败，返回null");
                    return false;
                }
                
                _logService.Info("PaddleOCR引擎创建成功，正在进行初始化测试...");
                
                // 进行简单的初始化测试
                try
                {
                    // 创建一个小的测试图像
                    using (var testBitmap = new Bitmap(100, 50))
                    {
                        using (var g = Graphics.FromImage(testBitmap))
                        {
                            g.Clear(Color.White);
                            g.DrawString("测试", new Font("Arial", 12), Brushes.Black, 10, 10);
                        }
                        
                        _logService.Info("正在进行OCR引擎测试...");
                        var testResult = _ocrEngine.DetectText(testBitmap);
                        var blockCount = testResult != null && testResult.TextBlocks != null ? testResult.TextBlocks.Count : 0;
                        _logService.Info("OCR引擎测试完成，检测到 {0} 个文本块", blockCount);
                    }
                }
                catch (Exception testEx)
                {
                    _logService.Error(testEx, "OCR引擎测试失败: {0}", testEx.Message);
                    // 测试失败不影响初始化，继续进行
                }

                _isInitialized = true;
                _logService.Info("OCR引擎初始化成功");
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "OCR引擎初始化失败: {0}", ex.Message);
                _isInitialized = false;
                return false;
            }
        }

        /// <summary>
        /// 识别图像中的文本
        /// </summary>
        public async Task<OCRResult> RecognizeTextFromBitmapAsync(Bitmap image)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("OCR引擎未初始化");
            }

            try
            {
                _logService.Info("开始OCR文字识别...");

                // 预处理图像
                var processedImage = PreprocessImage(image, _logService);



                // 执行OCR识别
                var ocrResult = await Task.Run(() => _ocrEngine.DetectText(processedImage));

                // 添加详细的调试日志
                _logService.Info("OCR识别完成，识别到 {0} 个文本块", ocrResult.TextBlocks != null ? ocrResult.TextBlocks.Count : 0);
                
                if (ocrResult.TextBlocks != null)
                {
                    for (int i = 0; i < ocrResult.TextBlocks.Count; i++)
                    {
                        var block = ocrResult.TextBlocks[i];
                        _logService.Info("文本块 {0}: 内容='{1}', 置信度={2:F3}", i + 1, block.Text != null ? block.Text : "<null>", block.Score);
                    }
                }

                // 转换结果格式
                var validTextBlocks = ocrResult.TextBlocks != null ? ocrResult.TextBlocks.Where(t => !string.IsNullOrWhiteSpace(t.Text)).ToList() : new List<PaddleOCRSharp.TextBlock>();
                var allText = RebuildParagraphs(validTextBlocks);
                
                var result = new OCRResult
                {
                    Text = allText,
                    Confidence = validTextBlocks.Any() ? validTextBlocks.Average(t => t.Score) : 0,
                    TextBlocks = ocrResult.TextBlocks != null ? ocrResult.TextBlocks.Select(ConvertTextBlock).ToList() : new List<TextBlock>(),
                    ProcessingTime = (long)(DateTime.Now - DateTime.MinValue).TotalMilliseconds
                };

                _logService.Info("最终OCR结果: 文本='{0}', 置信度={1:F3}", result.Text, result.Confidence);
                
                // 触发识别完成事件
                if (TextRecognized != null)
                {
                    TextRecognized(this, result);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "OCR识别失败");
                return new OCRResult { Text = string.Empty, Confidence = 0 };
            }
        }

        /// <summary>
        /// 识别图像中的文本（详细结果）
        /// </summary>
        public async Task<List<TextBlock>> RecognizeTextBlocksAsync(Bitmap image)
        {
            var result = await RecognizeTextFromBitmapAsync(image);
            return result.TextBlocks;
        }

        /// <summary>
        /// 识别文件中的文本
        /// </summary>
        [Obsolete("Use RecognizeTextFromBitmapAsync instead as it is more efficient.")]
        public async Task<OCRResult> RecognizeTextFromFileAsync(string filePath)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("OCR引擎未初始化");
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logService.Error("文件路径无效或文件不存在: {0}", filePath);
                return new OCRResult { Text = string.Empty, Confidence = 0 };
            }

            try
            {
                using (var bitmap = new Bitmap(filePath))
                {
                    return await RecognizeTextFromBitmapAsync(bitmap);
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "从文件识别文本失败: {0}", filePath);
                return new OCRResult { Text = string.Empty, Confidence = 0 };
            }
        }

        /// <summary>
        /// 设置识别语言
        /// </summary>
        public void SetLanguage(string language)
        {
            _currentLanguage = language ?? "zh-cn";
            _logService.Info("OCR语言设置为: {0}", _currentLanguage);
        }

        /// <summary>
        /// 获取支持的语言列表
        /// </summary>
        public List<string> GetSupportedLanguages()
        {
            return new List<string>
            {
                "zh-cn", // 中文简体
                "zh-tw", // 中文繁体
                "en",    // 英文
                "ja",    // 日文
                "ko"     // 韩文
            };
        }

        /// <summary>
        /// 预处理图像以提高识别准确率
        /// </summary>
        public Bitmap PreprocessImage(Bitmap image, ILogService logService)
        {
            try
            {
                var tempPath = Path.GetTempPath();
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                // 原始图像
                image.Save(Path.Combine(tempPath, string.Format("ocr_debug_{0}_0_original.png", timestamp)), ImageFormat.Png);

                // 1. 灰度化
                var grayscaleImage = ImageProcessor.ConvertToGrayscale(image);
                grayscaleImage.Save(Path.Combine(tempPath, string.Format("ocr_debug_{0}_1_grayscale.png", timestamp)), ImageFormat.Png);
                _logService.Info("Saved grayscale image for debugging.");

                // 2. 增强对比度
                var contrastImage = ImageProcessor.EnhanceContrast(grayscaleImage, 1.5f);
                contrastImage.Save(Path.Combine(tempPath, string.Format("ocr_debug_{0}_2_contrast.png", timestamp)), ImageFormat.Png);
                _logService.Info("Saved contrast image for debugging.");

                // 3. 锐化
                var sharpenedImage = ImageProcessor.SharpenImage(contrastImage);
                sharpenedImage.Save(Path.Combine(tempPath, string.Format("ocr_debug_{0}_3_sharpened.png", timestamp)), ImageFormat.Png);
                _logService.Info("Saved sharpened image for debugging.");

                // 4. 缩放
                var scaledImage = ImageProcessor.ScaleImage(sharpenedImage, 2.0f);
                scaledImage.Save(Path.Combine(tempPath, string.Format("ocr_debug_{0}_4_scaled.png", timestamp)), ImageFormat.Png);
                _logService.Info("Saved scaled image for debugging.");

                return scaledImage;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "图像预处理失败: {0}", ex.Message);
                return image; // 返回原图像
            }
        }

        /// <summary>
        /// 段落重排：按 Y 坐标聚类成行（同行文本块合并为一段），行间换行。
        /// 改进默认 string.Join("\n") 导致的"每块一行"碎片化排版。
        /// </summary>
        private string RebuildParagraphs(List<PaddleOCRSharp.TextBlock> blocks)
        {
            if (blocks == null || blocks.Count == 0) return string.Empty;

            // 过滤空文本，按顶部 Y 排序（从上到下），同行再按左 X 排序（从左到右）
            var sorted = blocks.Where(b => !string.IsNullOrWhiteSpace(b.Text))
                               .Select(b => new { Block = b, Top = b.BoxPoints.Min(p => p.Y), Left = b.BoxPoints.Min(p => p.X) })
                               .OrderBy(x => x.Top)
                               .ThenBy(x => x.Left)
                               .ToList();

            var lines = new List<string>();
            var currentLine = new List<string>();
            float prevTop = float.MinValue;
            const float threshold = 8f; // Y 差小于此值视为同一行的不同文本块

            foreach (var item in sorted)
            {
                if (currentLine.Count > 0 && Math.Abs(item.Top - prevTop) > threshold)
                {
                    // 进入新行：把当前行用空格连接成一段
                    lines.Add(string.Join(" ", currentLine));
                    currentLine.Clear();
                }
                currentLine.Add(item.Block.Text.Trim());
                prevTop = item.Top;
            }
            if (currentLine.Count > 0)
            {
                lines.Add(string.Join(" ", currentLine));
            }
            return string.Join("\n", lines);
        }

        /// <summary>
        /// 转换PaddleOCR的TextBlock到自定义TextBlock
        /// </summary>
        private TextBlock ConvertTextBlock(PaddleOCRSharp.TextBlock paddleTextBlock)
        {
            return new TextBlock
            {
                Text = paddleTextBlock.Text,
                Confidence = paddleTextBlock.Score,
                BoundingBox = new Rectangle(
                    (int)paddleTextBlock.BoxPoints.Min(p => p.X),
                    (int)paddleTextBlock.BoxPoints.Min(p => p.Y),
                    (int)(paddleTextBlock.BoxPoints.Max(p => p.X) - paddleTextBlock.BoxPoints.Min(p => p.X)),
                    (int)(paddleTextBlock.BoxPoints.Max(p => p.Y) - paddleTextBlock.BoxPoints.Min(p => p.Y))
                ),
                BoxPoints = paddleTextBlock.BoxPoints.Select(p => new System.Drawing.Point((int)p.X, (int)p.Y)).ToArray()
            };
        }

        /// <summary>
        /// 测试OCR功能
        /// </summary>
        public async Task<bool> TestOCRFunctionAsync()
        {
            try
            {
                _logService.Info("开始OCR功能测试...");
                
                // 创建测试图像
                var testImagePath = CreateTestImage();
                if (string.IsNullOrEmpty(testImagePath))
                {
                    _logService.Error("创建测试图像失败");
                    return false;
                }
                
                _logService.Info(string.Format("测试图像已创建: {0}", testImagePath));
                
                // 确保OCR引擎已初始化
                if (!_isInitialized)
                {
                    _logService.Info("OCR引擎未初始化，正在初始化...");
                    var initResult = await InitializeAsync();
                    if (!initResult)
                    {
                        _logService.Error("OCR引擎初始化失败");
                        return false;
                    }
                }
                
                // 进行OCR识别
                using (var bitmap = new System.Drawing.Bitmap(testImagePath))
                {
                    var result = await RecognizeTextFromBitmapAsync(bitmap);
                
                    if (result != null && !string.IsNullOrEmpty(result.Text))
                    {
                        _logService.Info(string.Format("OCR测试成功 - 识别文本: {0}", result.Text));
                        _logService.Info(string.Format("置信度: {0:F2}", result.Confidence));
                        
                        // 清理测试文件
                        try
                        {
                            if (File.Exists(testImagePath))
                            {
                                File.Delete(testImagePath);
                            }
                        }
                        catch { }
                        
                        return true;
                    }
                    else
                    {
                        _logService.Error("OCR测试失败: 未识别到文本或结果为空");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "OCR功能测试异常: {0}", ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// 创建测试图像
        /// </summary>
        private string CreateTestImage()
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), string.Format("ocr_test_{0:yyyyMMdd_HHmmss}.png", DateTime.Now));
                
                using (var bitmap = new System.Drawing.Bitmap(600, 200, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    // 设置高质量渲染
                    graphics.Clear(System.Drawing.Color.White);
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    
                    // 绘制测试文字
                    using (var font = new System.Drawing.Font("微软雅黑", 16, System.Drawing.FontStyle.Regular))
                    {
                        graphics.DrawString("这是中文测试文字", font, System.Drawing.Brushes.Black, 50, 30);
                        graphics.DrawString("English Test Text", font, System.Drawing.Brushes.Black, 50, 70);
                        graphics.DrawString("数字测试: 123456789", font, System.Drawing.Brushes.Black, 50, 110);
                        graphics.DrawString("混合文本: Hello 世界 2024", font, System.Drawing.Brushes.Black, 50, 150);
                    }
                    
                    bitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
                }
                
                return tempPath;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "创建测试图像失败: {0}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (_ocrEngine != null)
                {
                    _ocrEngine.Dispose();
                }
                _isInitialized = false;
                _logService.Info("OCR引擎资源已释放");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "释放OCR引擎资源失败: {0}", ex.Message);
            }
        }
    }

}