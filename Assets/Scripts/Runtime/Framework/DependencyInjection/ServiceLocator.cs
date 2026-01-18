using System;
using UnityEngine;

namespace TradeGame.Runtime.Framework
{
    /// <summary>
    /// 服务定位器（静态访问容器）
    /// 提供全局访问依赖注入容器的便捷方法
    /// </summary>
    public static class ServiceLocator
    {
        /// <summary>
        /// 获取指定类型的服务实例
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例</returns>
        public static T Get<T>()
        {
            return Bootstrapper.Resolve<T>();
        }

        /// <summary>
        /// 尝试获取指定类型的服务实例
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <param name="service">输出的服务实例</param>
        /// <returns>是否成功获取</returns>
        public static bool TryGet<T>(out T service)
        {
            return Bootstrapper.TryResolve(out service);
        }

        /// <summary>
        /// 检查指定类型的服务是否已注册
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>是否已注册</returns>
        public static bool IsRegistered<T>()
        {
            return Bootstrapper.TryResolve<T>(out _);
        }

        /// <summary>
        /// 安全获取服务（如果服务不存在，返回默认值）
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <param name="defaultValue">默认值</param>
        /// <returns>服务实例或默认值</returns>
        public static T GetOrDefault<T>(T defaultValue = default)
        {
            if (TryGet(out T service))
            {
                return service;
            }
            return defaultValue;
        }

        /// <summary>
        /// 获取服务，如果服务不存在则记录警告
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <param name="caller">调用者信息（用于日志）</param>
        /// <returns>服务实例或默认值</returns>
        public static T GetWithWarning<T>(object caller = null)
        {
            if (TryGet(out T service))
            {
                return service;
            }

            string callerName = caller?.GetType().Name ?? "未知";
            LogManager.Warning(LogCategory.System, $"[ServiceLocator] 未找到服务 {typeof(T).Name} (调用者: {callerName})");
            return default;
        }
    }
}