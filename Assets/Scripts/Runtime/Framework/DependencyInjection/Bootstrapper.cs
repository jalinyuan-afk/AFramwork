using System;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;
using UnityHFSM;

using TradeGame.Runtime;

namespace TradeGame.Runtime.Framework
{
    /// <summary>
    /// 容器引导程序：负责初始化VContainer容器并注册全局服务
    /// 可以挂载到场景中作为根LifetimeScope
    /// </summary>
    [Title("Bootstrapper (容器引导)")]
    [InfoBox("说明：\n- Parent: 设置 Bootstrapper 的父对象。\n- Auto Run: 勾选以在场景启动时自动构建容器。\n- Auto Inject Game Objects: 勾选以自动注入场景中的 GameObjects。\n下面的列表用于手动或额外注册服务。", InfoMessageType.Info)]
    public class Bootstrapper : LifetimeScope
    {
        [SerializeField]
        [LabelText("手动注册 (Manual Registrations)")]
        [Tooltip("在此添加需要手动注册到容器的组件及其生命周期。")]
        [ListDrawerSettings(Expanded = true)]
        private List<ServiceRegistration> manualRegistrations = new();

        [SerializeField]
        [LabelText("服务注册器 (Service Registrars)")]
        [Tooltip("通过实现 IServiceRegistrar 接口的组件将被调用以注册服务。优先使用此列表进行手动配置。")]
        [ListDrawerSettings(Expanded = true, HideAddButton = false)]
        private List<MonoBehaviour> _serviceRegistrars = new();

        [SerializeField]
        [LabelText("附加安装器 (Additional Installers)")]
        [Tooltip("可选：添加实现 IInstaller 的组件以注册额外服务（例如模块化的安装器）。")]
        [ListDrawerSettings(Expanded = true)]
        private List<MonoBehaviour> _additionalInstallers = new();
        private bool _isBuilt;
        private static Bootstrapper _cachedInstance;

        /// <summary>
        /// 服务注册信息（用于编辑器配置）
        /// </summary>
        [Serializable]
        public class ServiceRegistration
        {
            [LabelText("实现 (Implementation)")]
            [Tooltip("要注册的组件实例。该组件的类型将作为注册类型。")]
            public MonoBehaviour implementation;

            [LabelText("生命周期 (Lifetime)")]
            [Tooltip("指定注册到容器时使用的生命周期（Singleton / Scoped / Transient）。")]
            public Lifetime lifetime = Lifetime.Singleton;
        }
        //服务注册：服务注册节点只关心有哪些服务，不关心它们如何初始化
        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            // 注册核心框架服务
            RegisterFrameworkServices(builder);

            // 注册手动配置的服务
            RegisterManualServices(builder);

            // 调用服务注册器接口，允许其他模块注册服务
            InvokeServiceRegistrars(builder);
        }

        private void RegisterFrameworkServices(IContainerBuilder builder)
        {
            // 注意：VContainer 已自动注册 IObjectResolver，无需手动注册

            // 注册事件总线
            builder.Register<EventBus>(Lifetime.Singleton).As<IEventBus>();

            // 注册任务调度器
            builder.Register<TaskScheduler>(Lifetime.Singleton).As<ITaskScheduler>();

            // 注册时间服务
            builder.Register<TimeService>(Lifetime.Singleton).As<ITimeService>();

            // 注意：业务服务（如 ShopService）应由各自的 IServiceRegistrar 负责注册
            // 例如 ShopServiceRegistrar 会注册 IShopService
            // 这样做的好处是保持 Bootstrapper 的整洁，职责清晰
            // 注册配置模块（纯C#安装器）
            // new ConfigInstaller().Install(builder);
            // // 注册资源模块（纯C#安装器）
            // new ResourceInstaller().Install(builder);
            // // 注册场景模块（纯C#安装器）
            // new SceneInstaller().Install(builder);
            // // 注册本地化模块（纯C#安装器）
            // new LocalizationInstaller().Install(builder);
            // // 注册流程模块（纯C#安装器）
            // new ProcedureInstaller().Install(builder);
            // // 注册数据表模块（纯C#安装器）
            // new DataTableInstaller().Install(builder);
            // new SaveInstaller().Install(builder);
            // // 注册UI模块（纯C#安装器）
            // new UIInstaller().Install(builder);

            // // 注册核心服务到根容器，供跨场景访问
            // builder.Register<PlayerProfileService>(Lifetime.Transient).As<IPlayerProfileService>();
            // builder.Register<AttributeService>(Lifetime.Singleton).As<IAttributeService>();
            // builder.Register<FactionService>(Lifetime.Singleton).As<IFactionService>();

            // 注册自定义安装器
            foreach (var installer in _additionalInstallers)
            {
                if (installer == null) continue;
                if (installer is VContainer.Unity.IInstaller vcInstaller)
                {
                    vcInstaller.Install(builder);
                    LogManager.Info(LogCategory.System, $"[Bootstrapper] 调用自定义安装器: {installer.GetType().Name}");
                }
                else
                {
                    LogManager.Warning(LogCategory.System, $"[Bootstrapper] 跳过非 IInstaller 组件: {installer.GetType().Name}");
                }
            }

            LogManager.Info(LogCategory.System, "[Bootstrapper] 框架服务注册完成");
        }

        private void RegisterManualServices(IContainerBuilder builder)
        {
            foreach (var reg in manualRegistrations)
            {
                if (reg.implementation == null)
                {
                    LogManager.Warning(LogCategory.System, "[Bootstrapper] 跳过了无效的手动服务注册");
                    continue;
                }

                var implementationType = reg.implementation.GetType();
                // 根据生命周期选择注册方式
                switch (reg.lifetime)
                {
                    case Lifetime.Singleton:
                        // 单例直接注册实例
                        builder.RegisterInstance(reg.implementation).As(implementationType);
                        break;
                    case Lifetime.Scoped:
                    case Lifetime.Transient:
                        // Scoped 和 Transient 通过工厂注册，以尊重生命周期
                        builder.Register(_ => reg.implementation, reg.lifetime).As(implementationType);
                        break;
                }

                LogManager.Info(LogCategory.System, $"[Bootstrapper] 手动注册服务: {implementationType.Name} ({reg.lifetime})");
            }
        }

        private void InvokeServiceRegistrars(IContainerBuilder builder)
        {
            // 优先使用手动配置的注册器列表
            bool usedManualList = false;
            if (_serviceRegistrars != null && _serviceRegistrars.Count > 0)
            {
                foreach (var obj in _serviceRegistrars)
                {
                    if (obj is IServiceRegistrar registrar)
                    {
                        registrar.RegisterServices(builder);
                        LogManager.Info(LogCategory.System, $"[Bootstrapper] 调用手动配置的服务注册器: {registrar.GetType().Name}");
                        usedManualList = true;
                    }
                    else if (obj != null)
                    {
                        LogManager.Warning(LogCategory.System, $"[Bootstrapper] 跳过非 IServiceRegistrar 组件: {obj.GetType().Name}");
                    }
                }
            }

            // 如果没有手动配置的注册器，回退到自动查找（兼容旧行为）
            if (!usedManualList)
            {
                LogManager.Info(LogCategory.System, "[Bootstrapper] 未配置手动注册器，开始自动查找...");
                var registrars = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
                foreach (var obj in registrars)
                {
                    if (obj is IServiceRegistrar registrar)
                    {
                        registrar.RegisterServices(builder);
                        LogManager.Info(LogCategory.System, $"[Bootstrapper] 调用自动发现的服务注册器: {registrar.GetType().Name}");
                    }
                }
            }
        }


        /// <summary>
        /// 手动构建容器（如果基类 autoRun 为 false）
        /// </summary>
        public new void Build()
        {
            if (!_isBuilt)
            {
                LogManager.Info(LogCategory.System, "[Bootstrapper] 开始构建容器...");
                base.Build();
                _isBuilt = true;
            }
        }

        /// <summary>
        /// 从容器中解析服务（便捷方法）
        /// </summary>
        public static T Resolve<T>()
        {
            var bootstrapper = GetCachedBootstrapper();
            if (bootstrapper == null)
            {
                LogManager.Error(LogCategory.System, "[Bootstrapper] 未找到Bootstrapper实例");
                return default;
            }

            // 确保容器已构建
            if (bootstrapper.Container == null)
            {
                bootstrapper.Build();
            }

            return bootstrapper.Container.Resolve<T>();
        }

        private static Bootstrapper GetCachedBootstrapper()
        {
            // 如果缓存为 null 或实例已被销毁，重新查找
            // 检查缓存是否有效（包括检查 GameObject 是否已被销毁）
            if (_cachedInstance == null || !IsGameObjectAlive(_cachedInstance))
            {
                _cachedInstance = FindObjectOfType<Bootstrapper>();
            }
            return _cachedInstance;
        }
        // 检查 GameObject 是否仍然存在
        private static bool IsGameObjectAlive(Bootstrapper bootstrapper)
        {
            // Unity 重载了 == 运算符，会检查对象是否已被销毁
            return bootstrapper != null && bootstrapper.gameObject != null;
        }
        // 添加测试辅助方法
        public static void ClearInstanceForTesting()
        {
            _cachedInstance = null;
        }
        /// <summary>
        /// 尝试解析服务，失败时返回false
        /// </summary>
        public static bool TryResolve<T>(out T service)
        {
            var bootstrapper = GetCachedBootstrapper();
            if (bootstrapper == null)
            {
                service = default;
                return false;
            }

            // 确保容器已构建
            if (bootstrapper.Container == null)
            {
                bootstrapper.Build();
            }

            return bootstrapper.Container.TryResolve<T>(out service);
        }
    }
}