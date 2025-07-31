using System;

namespace AIAnswerTool.Models
{
    /// <summary>
    /// API响应基类
    /// </summary>
    public class ApiResponse
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// 响应数据
        /// </summary>
        public object Data { get; set; }
        
        /// <summary>
        /// 创建成功响应
        /// </summary>
        /// <param name="data">响应数据</param>
        /// <returns>成功响应</returns>
        public static ApiResponse CreateSuccess(object data = null)
        {
            return new ApiResponse
            {
                Success = true,
                Data = data
            };
        }
        
        /// <summary>
        /// 创建失败响应
        /// </summary>
        /// <param name="errorMessage">错误消息</param>
        /// <returns>失败响应</returns>
        public static ApiResponse Failure(string errorMessage)
        {
            return new ApiResponse
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
    
    /// <summary>
    /// 泛型API响应类
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class ApiResponse<T> : ApiResponse
    {
        /// <summary>
        /// 强类型响应数据
        /// </summary>
        public new T Data { get; set; }
        
        /// <summary>
        /// 创建成功响应
        /// </summary>
        /// <param name="data">响应数据</param>
        /// <returns>成功响应</returns>
        public static new ApiResponse<T> CreateSuccess(T data)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data
            };
        }
        
        /// <summary>
        /// 创建失败响应
        /// </summary>
        /// <param name="errorMessage">错误消息</param>
        /// <returns>失败响应</returns>
        public static new ApiResponse<T> Failure(string errorMessage)
        {
            return new ApiResponse<T>
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}