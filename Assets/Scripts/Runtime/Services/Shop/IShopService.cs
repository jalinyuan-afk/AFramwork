using System;
using Cysharp.Threading.Tasks;
using UniRx;
using TradeGame.Runtime.Framework;
namespace TradeGame.Runtime
{
    public interface IShopService
    {
        UniTask<bool> PurchaseItem(string itemId, int quantity);
        IObservable<int> OnGoldChanged { get; }
    }

}