using VContainer;

namespace TradeGame.Runtime.Framework
{
    /// <summary>
    /// 服务注册器接口：允许模块化注册服务到VContainer容器
    /// </summary>
    public interface IServiceRegistrar
    {
        /// <summary>
        /// 注册本模块所需的服务
        /// </summary>
        /// <param name="builder">VContainer容器构建器</param>
        void RegisterServices(IContainerBuilder builder);
    }
}