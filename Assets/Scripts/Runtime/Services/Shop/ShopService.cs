using System;
using Cysharp.Threading.Tasks;
using UniRx;
using TradeGame.Runtime.Framework;
namespace TradeGame.Runtime
{
    public class ShopService : IShopService
    {
        private readonly IEventBus _eventBus;
        private readonly ReactiveProperty<int> _gold = new ReactiveProperty<int>(1000);

        public ShopService(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async UniTask<bool> PurchaseItem(string itemId, int quantity)
        {
            // 模拟异步购买
            await UniTask.Delay(500);
            _gold.Value -= quantity;
            _eventBus.Publish("玩家金币变化", _gold.Value);
            return true;
        }

        public IObservable<int> OnGoldChanged => _gold;
    }
}