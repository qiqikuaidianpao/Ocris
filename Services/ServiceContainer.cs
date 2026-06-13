using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ocris.Services
{
    /// <summary>
    /// 简单的IoC容器实现
    /// 支持单例和瞬态服务注册
    /// </summary>
    public class ServiceContainer : IDisposable
    {
        private readonly Dictionary<Type, ServiceDescriptor> _services;
        private readonly Dictionary<Type, object> _singletonInstances;
        private readonly object _lock = new object();
        private bool _disposed = false;

        // 静态实例
        private static ServiceContainer _instance;
        private static readonly object _staticLock = new object();

        public ServiceContainer()
        {
            _services = new Dictionary<Type, ServiceDescriptor>();
            _singletonInstances = new Dictionary<Type, object>();
        }

        /// <summary>
        /// 获取默认实例
        /// </summary>
        public static ServiceContainer Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_staticLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ServiceContainer();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 初始化默认容器并注册服务
        /// </summary>
        public static void Initialize()
        {
            var container = Instance;
            
            // 注册服务
            container.RegisterSingleton<ILogService, LogService>();
            container.RegisterSingleton<IConfigService, ConfigService>();
            container.RegisterSingleton<IAIService, AliCloudAIService>();
            container.RegisterSingleton<IOCRService, OCRService>();
            container.RegisterSingleton<IHotkeyService, HotkeyService>();
            container.RegisterSingleton<IWindowDetectionService, WindowDetectionService>();
            container.RegisterSingleton<IScreenshotEngine, GdiScreenshotEngine>();
            container.RegisterSingleton<IScreenshotService, ScreenshotAdapter>();
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public static void Cleanup()
        {
            lock (_staticLock)
            {
                if (_instance != null)
                {
                    _instance.Dispose();
                    _instance = null;
                }
            }
        }

        /// <summary>
        /// 注册单例服务
        /// </summary>
        public void RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            RegisterSingleton<TInterface>(typeof(TImplementation));
        }

        /// <summary>
        /// 注册单例服务（指定实现类型）
        /// </summary>
        public void RegisterSingleton<TInterface>(Type implementationType)
        {
            if (implementationType == null)
                throw new ArgumentNullException("implementationType");

            lock (_lock)
            {
                _services[typeof(TInterface)] = new ServiceDescriptor
                {
                    ServiceType = typeof(TInterface),
                    ImplementationType = implementationType,
                    Lifetime = ServiceLifetime.Singleton
                };
            }
        }

        /// <summary>
        /// 注册单例服务（指定实例）
        /// </summary>
        public void RegisterSingleton<TInterface>(TInterface instance)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");

            lock (_lock)
            {
                _services[typeof(TInterface)] = new ServiceDescriptor
                {
                    ServiceType = typeof(TInterface),
                    Instance = instance,
                    Lifetime = ServiceLifetime.Singleton
                };

                _singletonInstances[typeof(TInterface)] = instance;
            }
        }

        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        public void RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            RegisterTransient<TInterface>(typeof(TImplementation));
        }

        /// <summary>
        /// 注册瞬态服务（指定实现类型）
        /// </summary>
        public void RegisterTransient<TInterface>(Type implementationType)
        {
            if (implementationType == null)
                throw new ArgumentNullException("implementationType");

            lock (_lock)
            {
                _services[typeof(TInterface)] = new ServiceDescriptor
                {
                    ServiceType = typeof(TInterface),
                    ImplementationType = implementationType,
                    Lifetime = ServiceLifetime.Transient
                };
            }
        }

        /// <summary>
        /// 解析服务
        /// </summary>
        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        /// <summary>
        /// 解析服务（指定类型）
        /// </summary>
        public object Resolve(Type serviceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException("serviceType");

            ServiceDescriptor descriptor;
            lock (_lock)
            {
                if (!_services.TryGetValue(serviceType, out descriptor))
                {
                    throw new InvalidOperationException(string.Format("服务类型 {0} 未注册", serviceType.Name));
                }
            }

            // 如果是单例且已有实例，直接返回
            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                lock (_lock)
                {
                    if (_singletonInstances.ContainsKey(serviceType))
                    {
                        return _singletonInstances[serviceType];
                    }
                }
            }

            // 创建实例
            object instance;
            if (descriptor.Instance != null)
            {
                instance = descriptor.Instance;
            }
            else
            {
                instance = CreateInstance(descriptor.ImplementationType);
            }

            // 如果是单例，缓存实例
            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                lock (_lock)
                {
                    if (!_singletonInstances.ContainsKey(serviceType))
                    {
                        _singletonInstances[serviceType] = instance;
                    }
                }
            }

            return instance;
        }

        /// <summary>
        /// 尝试解析服务
        /// </summary>
        public bool TryResolve<T>(out T service)
        {
            try
            {
                service = Resolve<T>();
                return true;
            }
            catch
            {
                service = default(T);
                return false;
            }
        }

        /// <summary>
        /// 检查服务是否已注册
        /// </summary>
        public bool IsRegistered<T>()
        {
            return IsRegistered(typeof(T));
        }

        /// <summary>
        /// 检查服务是否已注册（指定类型）
        /// </summary>
        public bool IsRegistered(Type serviceType)
        {
            lock (_lock)
            {
                return _services.ContainsKey(serviceType);
            }
        }

        /// <summary>
        /// 初始化所有单例服务
        /// </summary>
        public async Task InitializeServicesAsync()
        {
            var initializableSingletons = new List<object>();

            lock (_lock)
            {
                foreach (var kvp in _services)
                {
                    if (kvp.Value.Lifetime == ServiceLifetime.Singleton)
                    {
                        var instance = Resolve(kvp.Key);
                        if (instance is IAsyncInitializable)
                        {
                            initializableSingletons.Add(instance);
                        }
                    }
                }
            }

            // 异步初始化所有需要初始化的单例服务
            foreach (var service in initializableSingletons)
            {
                var initializable = service as IAsyncInitializable;
                if (initializable != null)
                {
                    await initializable.InitializeAsync();
                }
            }
        }

        /// <summary>
        /// 创建实例（简单的构造函数注入）
        /// </summary>
        private object CreateInstance(Type type)
        {
            var constructors = type.GetConstructors();
            
            // 检查是否有可用的构造函数
            if (constructors == null || constructors.Length == 0)
            {
                throw new InvalidOperationException(
                    string.Format("类型 {0} 没有可用的公共构造函数", type.Name));
            }
            
            // 优先选择参数最多的构造函数
            var constructor = constructors[0];
            foreach (var ctor in constructors)
            {
                if (ctor.GetParameters().Length > constructor.GetParameters().Length)
                {
                    constructor = ctor;
                }
            }

            var parameters = constructor.GetParameters();
            var args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                try
                {
                    args[i] = Resolve(paramType);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        string.Format("无法解析构造函数参数 {0} (类型: {1}): {2}", 
                            parameters[i].Name, paramType.Name, ex.Message), ex);
                }
            }

            try
            {
                return Activator.CreateInstance(type, args);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    string.Format("创建类型 {0} 的实例失败: {1}", type.Name, ex.Message), ex);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_lock)
                {
                    // 释放所有单例实例
                    foreach (var instance in _singletonInstances.Values)
                    {
                        var disposable = instance as IDisposable;
                        if (disposable != null)
                        {
                            try
                            {
                                disposable.Dispose();
                            }
                            catch
                            {
                                // 忽略释放时的异常
                            }
                        }
                    }

                    _singletonInstances.Clear();
                    _services.Clear();
                }

                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 服务描述符
    /// </summary>
    internal class ServiceDescriptor
    {
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public object Instance { get; set; }
        public ServiceLifetime Lifetime { get; set; }
    }

    /// <summary>
    /// 服务生命周期
    /// </summary>
    public enum ServiceLifetime
    {
        Singleton,
        Transient
    }

    /// <summary>
    /// 异步初始化接口
    /// </summary>
    public interface IAsyncInitializable
    {
        Task InitializeAsync();
    }
}