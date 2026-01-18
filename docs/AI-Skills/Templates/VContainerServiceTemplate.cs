using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace AFramework.Templates
{
    /// <summary>
    /// VContainer 服务注册模板
    /// 参考文件: Assets/Scripts/Runtime/Core/Bootstrapper.cs
    /// 
    /// 功能：
    /// - 注册服务（Register）
    /// - 解析服务（Resolve）
    /// - 生命周期管理（Singleton、Transient）
    /// </summary>
    public class VContainerServiceTemplate : LifetimeScope
    {
        #region 服务注册

        protected override void Configure(IContainerBuilder builder)
        {
            // 1. 注册单例服务（全局唯一实例）
            RegisterSingletonServices(builder);

            // 2. 注册临时服务（每次解析创建新实例）
            RegisterTransientServices(builder);

            // 3. 注册 MonoBehaviour 组件
            RegisterMonoBehaviourComponents(builder);

            // 4. 注册工厂
            RegisterFactories(builder);
        }

        #endregion

        #region 单例服务

        /// <summary>
        /// 注册单例服务
        /// 适用场景：TaskScheduler、EventBus 等全局服务
        /// </summary>
        private void RegisterSingletonServices(IContainerBuilder builder)
        {
            // 接口 + 实现类
            builder.Register<ITaskScheduler, TaskScheduler>(Lifetime.Singleton);
            builder.Register<IEventBus, EventBus>(Lifetime.Singleton);
            builder.Register<ILogManager, LogManager>(Lifetime.Singleton);

            // 只注册实现类
            builder.Register<ConfigManager>(Lifetime.Singleton);
            builder.Register<AudioManager>(Lifetime.Singleton);
        }

        #endregion

        #region 临时服务

        /// <summary>
        /// 注册临时服务
        /// 适用场景：每次使用都需要新实例的服务
        /// </summary>
        private void RegisterTransientServices(IContainerBuilder builder)
        {
            builder.Register<IAssetLoader, AssetLoader>(Lifetime.Transient);
            builder.Register<INetworkRequest, NetworkRequest>(Lifetime.Transient);
        }

        #endregion

        #region MonoBehaviour 组件

        [SerializeField] private UIManager uiManagerPrefab;
        [SerializeField] private SceneLoader sceneLoaderPrefab;

        /// <summary>
        /// 注册 MonoBehaviour 组件
        /// 适用场景：需要挂载到场景的管理器
        /// </summary>
        private void RegisterMonoBehaviourComponents(IContainerBuilder builder)
        {
            // 注册场景中已存在的组件
            builder.RegisterComponentInHierarchy<UIManager>();

            // 注册预制体实例
            builder.RegisterComponentInNewPrefab(uiManagerPrefab, Lifetime.Singleton);
        }

        #endregion

        #region 工厂注册

        /// <summary>
        /// 注册工厂
        /// 适用场景：需要动态创建对象的情况
        /// </summary>
        private void RegisterFactories(IContainerBuilder builder)
        {
            builder.RegisterFactory<string, INetworkRequest>(container => url =>
            {
                var request = container.Resolve<INetworkRequest>();
                request.SetUrl(url);
                return request;
            });
        }

        #endregion

        #region 静态解析方法

        private static LifetimeScope _globalScope;

        protected override void Awake()
        {
            base.Awake();
            _globalScope = this;
        }

        /// <summary>
        /// 全局解析服务
        /// 使用示例：var taskScheduler = Bootstrapper.Resolve<ITaskScheduler>();
        /// </summary>
        public static T Resolve<T>()
        {
            return _globalScope.Container.Resolve<T>();
        }

        /// <summary>
        /// 安全解析服务（如果不存在返回 false）
        /// </summary>
        public static bool TryResolve<T>(out T service)
        {
            try
            {
                service = _globalScope.Container.Resolve<T>();
                return true;
            }
            catch
            {
                service = default;
                return false;
            }
        }

        #endregion
    }

    #region 示例服务接口和实现

    // 接口定义
    public interface ITaskScheduler { }
    public interface IEventBus { }
    public interface ILogManager { }
    public interface IAssetLoader { }
    public interface INetworkRequest
    {
        void SetUrl(string url);
    }

    // 实现类（示例）
    public class TaskScheduler : ITaskScheduler { }
    public class EventBus : IEventBus { }
    public class LogManager : ILogManager { }
    public class AssetLoader : IAssetLoader { }
    public class NetworkRequest : INetworkRequest
    {
        public void SetUrl(string url) { }
    }

    // MonoBehaviour 管理器
    public class UIManager : MonoBehaviour { }
    public class SceneLoader : MonoBehaviour { }
    public class ConfigManager { }
    public class AudioManager { }

    #endregion
}
