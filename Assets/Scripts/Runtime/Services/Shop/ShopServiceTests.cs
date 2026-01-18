using System;
using Cysharp.Threading.Tasks;
using TradeGame.Runtime.Framework;
using UnityEngine;
using UniRx;

namespace TradeGame.Runtime.Tests
{
    /// <summary>
    /// 商店服务集成测试
    /// 用于在 Unity Play 模式下手动测试商店功能
    /// </summary>
    public class ShopServiceTests : MonoBehaviour
    {
        private IShopService _shopService;
        private IEventBus _eventBus;
        private CompositeDisposable _disposables = new CompositeDisposable();

        private void Start()
        {
            Debug.Log("=== 商店服务测试开始 ===");
            RunAllTests();
        }

        private async void RunAllTests()
        {
            try
            {
                // 测试 1: 服务解析
                await TestServiceResolution();
                await UniTask.Delay(500);

                // 测试 2: 购买功能
                await TestPurchaseItem();
                await UniTask.Delay(500);

                // 测试 3: 金币变化事件
                await TestGoldChangeEvent();
                await UniTask.Delay(500);

                // 测试 4: ReactiveProperty 订阅
                await TestReactivePropertySubscription();
                await UniTask.Delay(500);

                // 测试 5: 多次购买
                await TestMultiplePurchases();

                Debug.Log("=== 所有测试完成 ===");
            }
            catch (Exception ex)
            {
                Debug.LogError($"测试失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 测试 1: 验证服务是否正确注册和解析
        /// </summary>
        private async UniTask TestServiceResolution()
        {
            Debug.Log("\n[测试 1] 服务解析测试");

            _shopService = Bootstrapper.Resolve<IShopService>();
            _eventBus = Bootstrapper.Resolve<IEventBus>();

            if (_shopService == null)
            {
                Debug.LogError("✗ IShopService 解析失败");
                return;
            }

            if (_eventBus == null)
            {
                Debug.LogError("✗ IEventBus 解析失败");
                return;
            }

            Debug.Log("✓ 服务解析成功");
            Debug.Log($"  - ShopService 类型: {_shopService.GetType().Name}");
            Debug.Log($"  - EventBus 类型: {_eventBus.GetType().Name}");

            await UniTask.Yield();
        }

        /// <summary>
        /// 测试 2: 验证购买功能是否正常工作
        /// </summary>
        private async UniTask TestPurchaseItem()
        {
            Debug.Log("\n[测试 2] 购买物品测试");

            bool result = await _shopService.PurchaseItem("TestItem", 1);

            if (result)
            {
                Debug.Log("✓ 购买成功");
            }
            else
            {
                Debug.LogError("✗ 购买失败");
            }
        }

        /// <summary>
        /// 测试 3: 验证事件总线是否正确传递金币变化事件
        /// </summary>
        private async UniTask TestGoldChangeEvent()
        {
            Debug.Log("\n[测试 3] 事件总线测试");

            bool eventReceived = false;
            int receivedGold = 0;

            // 订阅事件（通过 OnGoldChanged ReactiveProperty）
            var subscription = _shopService.OnGoldChanged.Subscribe(gold =>
            {
                eventReceived = true;
                receivedGold = gold;
                Debug.Log($"  - 事件接收: 金币变化为 {gold}");
            });

            // 触发购买
            await _shopService.PurchaseItem("Sword", 2);
            await UniTask.Delay(100); // 等待事件传播

            if (eventReceived)
            {
                Debug.Log($"✓ 事件接收成功，当前金币: {receivedGold}");
            }
            else
            {
                Debug.LogError("✗ 未接收到事件");
            }

            subscription.Dispose();
        }

        /// <summary>
        /// 测试 4: 验证 ReactiveProperty 订阅是否正常
        /// </summary>
        private async UniTask TestReactivePropertySubscription()
        {
            Debug.Log("\n[测试 4] ReactiveProperty 订阅测试");

            int updateCount = 0;
            int latestGold = 0;

            // 订阅 OnGoldChanged
            _shopService.OnGoldChanged
                .Subscribe(gold =>
                {
                    updateCount++;
                    latestGold = gold;
                    Debug.Log($"  - OnGoldChanged 触发: 金币 = {gold}");
                })
                .AddTo(_disposables);

            // 触发购买
            await _shopService.PurchaseItem("Potion", 1);
            await UniTask.Delay(100);

            if (updateCount > 0)
            {
                Debug.Log($"✓ ReactiveProperty 订阅成功，更新次数: {updateCount}，当前金币: {latestGold}");
            }
            else
            {
                Debug.LogError("✗ ReactiveProperty 未触发更新");
            }
        }

        /// <summary>
        /// 测试 5: 验证多次购买的金币扣除是否正确
        /// </summary>
        private async UniTask TestMultiplePurchases()
        {
            Debug.Log("\n[测试 5] 多次购买测试");

            int initialGold = 0;
            int finalGold = 0;

            // 获取初始金币
            _shopService.OnGoldChanged
                .Take(1)
                .Subscribe(gold => initialGold = gold)
                .AddTo(_disposables);

            await UniTask.Delay(50);
            Debug.Log($"  - 初始金币: {initialGold}");

            // 进行 3 次购买
            for (int i = 1; i <= 3; i++)
            {
                await _shopService.PurchaseItem($"Item{i}", 1);
                await UniTask.Delay(100);
            }

            // 等待最新金币更新
            await UniTask.Delay(100);
            _shopService.OnGoldChanged
                .Take(1)
                .Subscribe(gold => finalGold = gold)
                .AddTo(_disposables);

            await UniTask.Delay(50);
            Debug.Log($"  - 最终金币: {finalGold}");
            Debug.Log($"  - 消耗金币: {initialGold - finalGold}");

            if (finalGold < initialGold)
            {
                Debug.Log("✓ 多次购买测试成功");
            }
            else
            {
                Debug.LogError("✗ 金币未正确扣除");
            }
        }

        private void OnDestroy()
        {
            _disposables?.Dispose();
            Debug.Log("=== 测试组件销毁，资源已释放 ===");
        }
    }
}
