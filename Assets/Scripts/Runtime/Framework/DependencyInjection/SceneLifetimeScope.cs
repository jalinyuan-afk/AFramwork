using VContainer;
using VContainer.Unity;
using UnityEngine;

namespace TradeGame.Runtime.Framework
{
    /// <summary>
    /// 项目内的场景生命周期容器基类，封装 VContainer 的 LifetimeScope。
    /// 其他场景专用容器可以继承此类进行额外配置。
    /// </summary>
    public class SceneLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // 默认不注册额外服务；派生类可覆盖并调用 base.Configure(builder) 或直接实现自己的注册逻辑。
        }
    }
}
