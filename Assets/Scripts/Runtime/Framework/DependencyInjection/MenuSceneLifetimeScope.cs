using System;
using System.Collections.Generic;
using System.Linq;
using VContainer;
using VContainer.Unity;
using UnityEngine;
using Sirenix.OdinInspector;

using TradeGame.Runtime;

namespace TradeGame.Runtime.Framework
{
    /// <summary>
    /// 菜单场景生命周期容器：专用于菜单场景的依赖注入管理
    /// 继承 VContainer.Unity.SceneLifetimeScope，自动获得父子链建立、场景生命周期同步、依赖查找委托、自动清理功能
    /// 额外提供菜单专用服务注册配置
    /// </summary>
    [Title("Menu Scene Lifetime Scope (菜单场景生命周期容器)")]
    [InfoBox("说明：\n- 继承 VContainer.SceneLifetimeScope，自动建立父子链、同步场景生命周期、提供依赖查找委托、自动清理资源。\n- 可配置菜单专用服务注册器和安装器。", InfoMessageType.Info)]
    public class MenuSceneLifetimeScope : SceneLifetimeScope
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
        private List<MonoBehaviour> serviceRegistrars = new();

        [SerializeField]
        [LabelText("附加安装器 (Additional Installers)")]
        [Tooltip("可选：添加实现 IInstaller 的组件以注册额外服务（例如模块化的安装器）。")]
        [ListDrawerSettings(Expanded = true)]
        private List<MonoBehaviour> additionalInstallers = new();

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

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            // 注册手动配置的服务
            RegisterManualServices(builder);

            // 调用服务注册器接口
            InvokeServiceRegistrars(builder);

            // 调用附加安装器
            InvokeAdditionalInstallers(builder);

            LogManager.Info(LogCategory.System, "[MenuSceneLifetimeScope] 菜单场景容器配置完成");
        }

        private void RegisterManualServices(IContainerBuilder builder)
        {




            foreach (var reg in manualRegistrations)
            {
                if (reg.implementation == null)
                {
                    LogManager.Warning(LogCategory.System, "[MenuSceneLifetimeScope] 跳过了无效的手动服务注册");
                    continue;
                }

                var implementationType = reg.implementation.GetType();
                switch (reg.lifetime)
                {
                    case Lifetime.Singleton:
                        builder.RegisterInstance(reg.implementation).As(implementationType);
                        break;
                    case Lifetime.Scoped:
                    case Lifetime.Transient:
                        builder.Register(_ => reg.implementation, reg.lifetime).As(implementationType);
                        break;
                }

                LogManager.Info(LogCategory.System, $"[MenuSceneLifetimeScope] 手动注册服务: {implementationType.Name} ({reg.lifetime})");
            }
        }

        private void InvokeServiceRegistrars(IContainerBuilder builder)
        {
            bool usedManualList = false;
            if (serviceRegistrars != null && serviceRegistrars.Count > 0)
            {
                foreach (var obj in serviceRegistrars)
                {
                    if (obj is IServiceRegistrar registrar)
                    {
                        registrar.RegisterServices(builder);
                        LogManager.Info(LogCategory.System, $"[MenuSceneLifetimeScope] 调用手动配置的服务注册器: {registrar.GetType().Name}");
                        usedManualList = true;
                    }
                    else if (obj != null)
                    {
                        LogManager.Warning(LogCategory.System, $"[MenuSceneLifetimeScope] 跳过非 IServiceRegistrar 组件: {obj.GetType().Name}");
                    }
                }
            }

            // 如果没有手动配置的注册器，回退到自动查找（仅限当前场景）
            if (!usedManualList)
            {
                LogManager.Info(LogCategory.System, "[MenuSceneLifetimeScope] 未配置手动注册器，开始自动查找当前场景...");
                var registrars = gameObject.scene.GetRootGameObjects()
                    .SelectMany(go => go.GetComponentsInChildren<MonoBehaviour>())
                    .OfType<IServiceRegistrar>();
                foreach (var registrar in registrars)
                {
                    registrar.RegisterServices(builder);
                    LogManager.Info(LogCategory.System, $"[MenuSceneLifetimeScope] 调用自动发现的服务注册器: {registrar.GetType().Name}");
                }
            }
        }

        private void InvokeAdditionalInstallers(IContainerBuilder builder)
        {
            foreach (var installer in additionalInstallers)
            {
                if (installer == null) continue;
                if (installer is VContainer.Unity.IInstaller vcInstaller)
                {
                    vcInstaller.Install(builder);
                    LogManager.Info(LogCategory.System, $"[MenuSceneLifetimeScope] 调用附加安装器: {installer.GetType().Name}");
                }
                else
                {
                    LogManager.Warning(LogCategory.System, $"[MenuSceneLifetimeScope] 跳过非 IInstaller 组件: {installer.GetType().Name}");
                }
            }
        }
    }
}
