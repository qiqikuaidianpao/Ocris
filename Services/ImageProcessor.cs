using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace AIAnswerTool.Services
{
    /// <summary>
    /// 图像处理辅助类
    /// </summary>
    public static class ImageProcessor
    {
        /// <summary>
        /// 增强图像对比度
        /// </summary>
        /// <param name="image">原始图像</param>
        /// <param name="contrast">对比度增强因子 (1.0 = 无变化, >1.0 = 增强对比度)</param>
        /// <returns>处理后的图像</returns>
        public static Bitmap EnhanceContrast(Bitmap image, float contrast = 1.2f)
        {
            var result = new Bitmap(image.Width, image.Height);
            
            using (var graphics = Graphics.FromImage(result))
            {
                // 创建颜色矩阵来调整对比度
                var colorMatrix = new ColorMatrix(new float[][]
                {
                    new float[] {contrast, 0, 0, 0, 0},
                    new float[] {0, contrast, 0, 0, 0},
                    new float[] {0, 0, contrast, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {0, 0, 0, 0, 1}
                });

                var imageAttributes = new ImageAttributes();
                imageAttributes.SetColorMatrix(colorMatrix);

                graphics.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height),
                    0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imageAttributes);
            }

            return result;
        }

        /// <summary>
        /// 调整图像亮度
        /// </summary>
        /// <param name="image">原始图像</param>
        /// <param name="brightness">亮度调整值 (-1.0 到 1.0)</param>
        /// <returns>处理后的图像</returns>
        public static Bitmap AdjustBrightness(Bitmap image, float brightness = 0.1f)
        {
            var result = new Bitmap(image.Width, image.Height);
            
            using (var graphics = Graphics.FromImage(result))
            {
                var colorMatrix = new ColorMatrix(new float[][]
                {
                    new float[] {1, 0, 0, 0, 0},
                    new float[] {0, 1, 0, 0, 0},
                    new float[] {0, 0, 1, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {brightness, brightness, brightness, 0, 1}
                });

                var imageAttributes = new ImageAttributes();
                imageAttributes.SetColorMatrix(colorMatrix);

                graphics.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height),
                    0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imageAttributes);
            }

            return result;
        }

        /// <summary>
        /// 转换为灰度图像
        /// </summary>
        /// <param name="image">原始图像</param>
        /// <returns>灰度图像</returns>
        public static Bitmap ConvertToGrayscale(Bitmap image)
        {
            var result = new Bitmap(image.Width, image.Height);

            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    var pixel = image.GetPixel(x, y);
                    var gray = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                    result.SetPixel(x, y, Color.FromArgb(gray, gray, gray));
                }
            }

            return result;
        }

        /// <summary>
        /// 锐化图像
        /// </summary>
        /// <param name="image">原始图像</param>
        /// <param name="strength">锐化强度 (0.0 到 2.0)</param>
        /// <returns>锐化后的图像</returns>
        public static Bitmap SharpenImage(Bitmap image, float strength = 1.0f)
        {
            var result = new Bitmap(image.Width, image.Height);
            
            using (var graphics = Graphics.FromImage(result))
            {
                // 锐化卷积核
                var kernel = new float[,]
                {
                    { 0, -strength, 0 },
                    { -strength, 1 + 4 * strength, -strength },
                    { 0, -strength, 0 }
                };

                // 应用卷积滤波器
                result = ApplyConvolution(image, kernel);
            }

            return result;
        }

        /// <summary>
        /// 缩放图像
        /// </summary>
        /// <param name="image">原始图像</param>
        /// <param name="scale">缩放比例</param>
        /// <param name="highQuality">是否使用高质量缩放</param>
        /// <returns>缩放后的图像</returns>
        public static Bitmap ScaleImage(Bitmap image, float scale, bool highQuality = true)
        {
            var newWidth = (int)(image.Width * scale);
            var newHeight = (int)(image.Height * scale);
            
            var result = new Bitmap(newWidth, newHeight);
            
            using (var graphics = Graphics.FromImage(result))
            {
                if (highQuality)
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                }
                else
                {
                    graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                }

                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            return result;
        }

        /// <summary>
        /// 旋转图像
        /// </summary>
        /// <param name="image">原始图像</param>
        /// <param name="angle">旋转角度（度）</param>
        /// <returns>旋转后的图像</returns>
        public static Bitmap RotateImage(Bitmap image, float angle)
        {
            // 计算旋转后的图像尺寸
            var radians = angle * Math.PI / 180;
            var cos = Math.Abs(Math.Cos(radians));
            var sin = Math.Abs(Math.Sin(radians));
            var newWidth = (int)(image.Width * cos + image.Height * sin);
            var newHeight = (int)(image.Width * sin + image.Height * cos);

            var result = new Bitmap(newWidth, newHeight);
            
            using (var graphics = Graphics.FromImage(result))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                
                // 移动到图像中心
                graphics.TranslateTransform(newWidth / 2f, newHeight / 2f);
                graphics.RotateTransform(angle);
                graphics.TranslateTransform(-image.Width / 2f, -image.Height / 2f);
                
                graphics.DrawImage(image, 0, 0);
            }

            return result;
        }

        /// <summary>
        /// 去除图像噪点
        /// </summary>
        /// <param name="image">原始图像</param>
        /// <returns>去噪后的图像</returns>
        public static Bitmap RemoveNoise(Bitmap image)
        {
            // 使用中值滤波去除噪点
            var result = new Bitmap(image.Width, image.Height);
            
            for (int x = 1; x < image.Width - 1; x++)
            {
                for (int y = 1; y < image.Height - 1; y++)
                {
                    var pixels = new Color[9];
                    var index = 0;
                    
                    // 获取3x3邻域的像素
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            pixels[index++] = image.GetPixel(x + i, y + j);
                        }
                    }
                    
                    // 计算中值
                    var medianColor = GetMedianColor(pixels);
                    result.SetPixel(x, y, medianColor);
                }
            }

            return result;
        }

        /// <summary>
        /// 应用卷积滤波器
        /// </summary>
        /// <param name="image">原始图像</param>
        /// <param name="kernel">卷积核</param>
        /// <returns>滤波后的图像</returns>
        private static Bitmap ApplyConvolution(Bitmap image, float[,] kernel)
        {
            var result = new Bitmap(image.Width, image.Height);
            var kernelSize = kernel.GetLength(0);
            var offset = kernelSize / 2;

            for (int x = offset; x < image.Width - offset; x++)
            {
                for (int y = offset; y < image.Height - offset; y++)
                {
                    float r = 0, g = 0, b = 0;
                    
                    for (int i = 0; i < kernelSize; i++)
                    {
                        for (int j = 0; j < kernelSize; j++)
                        {
                            var pixel = image.GetPixel(x + i - offset, y + j - offset);
                            var weight = kernel[i, j];
                            
                            r += pixel.R * weight;
                            g += pixel.G * weight;
                            b += pixel.B * weight;
                        }
                    }
                    
                    // 确保颜色值在有效范围内
                    r = Math.Max(0, Math.Min(255, r));
                    g = Math.Max(0, Math.Min(255, g));
                    b = Math.Max(0, Math.Min(255, b));
                    
                    result.SetPixel(x, y, Color.FromArgb((int)r, (int)g, (int)b));
                }
            }

            return result;
        }

        /// <summary>
        /// 获取颜色数组的中值
        /// </summary>
        /// <param name="colors">颜色数组</param>
        /// <returns>中值颜色</returns>
        private static Color GetMedianColor(Color[] colors)
        {
            var r = new int[colors.Length];
            var g = new int[colors.Length];
            var b = new int[colors.Length];
            
            for (int i = 0; i < colors.Length; i++)
            {
                r[i] = colors[i].R;
                g[i] = colors[i].G;
                b[i] = colors[i].B;
            }
            
            Array.Sort(r);
            Array.Sort(g);
            Array.Sort(b);
            
            var medianIndex = colors.Length / 2;
            return Color.FromArgb(r[medianIndex], g[medianIndex], b[medianIndex]);
        }

        /// <summary>
        /// 自动调整图像以提高OCR识别率
        /// </summary>
        /// <param name="image">原始图像</param>
        /// <returns>优化后的图像</returns>
        public static Bitmap OptimizeForOCR(Bitmap image)
        {
            // 1. 转换为灰度
            var grayscale = ConvertToGrayscale(image);
            
            // 2. 增强对比度
            var enhanced = EnhanceContrast(grayscale, 1.3f);
            
            // 3. 轻微锐化
            var sharpened = SharpenImage(enhanced, 0.5f);
            
            // 4. 如果图像太小，进行放大，保持宽高比
            if (image.Width < 300 || image.Height < 100)
            {
                float scaleX = 300f / image.Width;
                float scaleY = 100f / image.Height;
                var scale = Math.Min(scaleX, scaleY); // 使用最小的缩放比以保持内容不超出
                sharpened = ScaleImage(sharpened, scale, true);
            }
            
            return sharpened;
        }
    }
}