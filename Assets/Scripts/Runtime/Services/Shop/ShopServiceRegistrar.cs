using UnityEngine;
using TradeGame.Runtime.Framework;
using System;
using VContainer;
using VContainer.Unity;
namespace TradeGame.Runtime
{
    public class ShopServiceRegistrar : MonoBehaviour, IServiceRegistrar
    {
        public void RegisterServices(IContainerBuilder builder)
        {
            builder.Register<ShopService>(Lifetime.Singleton).As<IShopService>();
        }
    }
}
