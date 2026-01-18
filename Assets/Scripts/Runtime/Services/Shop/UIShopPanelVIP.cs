using UnityEngine;
using UnityEngine.UI;
using TradeGame.Runtime.Framework;
using UniRx;
using TMPro;
using System.Collections.Generic;
using VContainer;

namespace TradeGame.Runtime
{
    /// <summary>
    /// VIP 专属商店面板
    /// 需求：
    /// - VIP 玩家可看到专属折扣商品（5折）
    /// - 显示 VIP 等级和对应的特权
    /// - 特权包括：每日免费刷新一次、优先购买限定商品
    /// - 使用 [Inject] 自动注入依赖（展示指南第 43-72 行的最佳实践）
    /// - 支持工厂动态创建，可注入不同的 VIP 等级配置
    /// </summary>
    public class UIShopPanelVIP : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI vipLevelText;// 显示 VIP 等级的文本
        [SerializeField] private TextMeshProUGUI goldText;// 显示金币数量的文本
        [SerializeField] private TextMeshProUGUI privilegeText;// 显示特权列表的文本
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Button refreshButton;
        [SerializeField] private RectTransform vipItemsRoot;
        [SerializeField] private Button vipItemButtonPrefab;

        // 使用 [Inject] 自动注入，避免手动 Bootstrapper.Resolve
        // 这是 VContainer 在 MonoBehaviour 上的标准写法
        [Inject] private IShopService _shop;
        [Inject] private IEventBus _eventBus;

        private VIPShopConfig _config;
        private CompositeDisposable _disposables = new();
        private readonly CompositeDisposable _vipItemDisposables = new();

        /// <summary>
        /// 工厂注入的 VIP 配置
        /// </summary>
        public void Initialize(VIPShopConfig config)
        {
            _config = config ?? VIPShopConfig.CreateDefault();
            Debug.Log($"[UIShopPanelVIP] VIP 商店初始化 - 等级: {_config.VIPLevel}, 折扣: {_config.DiscountPercentage}%");
        }

        private void Start()
        {
            // [Inject] 自动注入后，_shop 和 _eventBus 已可用
            if (_shop == null || _eventBus == null)
            {
                Debug.LogError("[UIShopPanelVIP] 依赖注入失败，请检查 VContainer 配置");
                return;
            }

            if (_config == null)
            {
                _config = VIPShopConfig.CreateDefault();
            }

            ApplyVIPConfig();
            SubscribeToEvents();
            Debug.Log("[UIShopPanelVIP] Start 初始化完成");
        }

        private void ApplyVIPConfig()
        {
            // 显示 VIP 等级
            if (vipLevelText != null)
            {
                vipLevelText.text = $"VIP {_config.VIPLevel}";
            }

            // 显示特权列表
            if (privilegeText != null)
            {
                string privileges = string.Join("\n", _config.Privileges);
                privilegeText.text = $"特权：\n{privileges}";
            }

            // 根据配置启用/禁用刷新按钮
            if (refreshButton != null)
            {
                refreshButton.interactable = _config.CanRefreshDaily;
            }

            BuildVIPItems();
        }

        private void SubscribeToEvents()
        {
            // 订阅金币变化
            _shop.OnGoldChanged
                .Subscribe(gold => UpdateGoldUI(gold))
                .AddTo(_disposables);

            // 订阅购买事件（如果有的话）
            if (purchaseButton != null)
            {
                purchaseButton.OnClickAsObservable()
                    .Subscribe(_ => OnQuickPurchaseClicked())
                    .AddTo(_disposables);
            }

            if (refreshButton != null)
            {
                refreshButton.OnClickAsObservable()
                    .Subscribe(_ => OnRefreshClicked())
                    .AddTo(_disposables);
            }
        }

        private void BuildVIPItems()
        {
            _vipItemDisposables.Clear();

            if (vipItemsRoot == null || vipItemButtonPrefab == null)
            {
                Debug.LogWarning("[UIShopPanelVIP] 未配置 vipItemsRoot 或 vipItemButtonPrefab");
                return;
            }

            // 清空旧节点
            for (int i = vipItemsRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(vipItemsRoot.GetChild(i).gameObject);
            }

            // 生成 VIP 独占商品按钮（带折扣显示）
            foreach (var item in _config.VIPExclusiveItems)
            {
                var button = Instantiate(vipItemButtonPrefab, vipItemsRoot);
                var label = button.GetComponentInChildren<TextMeshProUGUI>();

                if (label != null)
                {
                    int discountedPrice = (int)(item.OriginalPrice * (100 - _config.DiscountPercentage) / 100f);
                    label.text = $"{item.DisplayName}\n原价: {item.OriginalPrice}  折扣价: {discountedPrice}";
                }

                button.OnClickAsObservable()
                    .Subscribe(_ => OnVIPItemClicked(item))
                    .AddTo(_vipItemDisposables);
            }

            Debug.Log($"[UIShopPanelVIP] 已生成 {_config.VIPExclusiveItems.Length} 个 VIP 独占商品");
        }

        private void UpdateGoldUI(int gold)
        {
            if (goldText != null)
            {
                goldText.text = $"金币: {gold}";
            }
        }

        private async void OnQuickPurchaseClicked()
        {
            // VIP 玩家快速购买第一个 VIP 商品
            if (_config.VIPExclusiveItems.Length > 0)
            {
                var item = _config.VIPExclusiveItems[0];
                int discountedPrice = (int)(item.OriginalPrice * (100 - _config.DiscountPercentage) / 100f);

                bool success = await _shop.PurchaseItem(item.ItemId, discountedPrice);
                _eventBus?.Publish(new ShopPurchaseEvent(item.ItemId, discountedPrice, success));

                if (success)
                {
                    Debug.Log($"[VIP 购买] {item.DisplayName} 成功，折扣价: {discountedPrice}");
                }
            }
        }

        private void OnRefreshClicked()
        {
            if (_config.CanRefreshDaily)
            {
                // 刷新 VIP 商品列表（模拟）
                Debug.Log($"[VIP 刷新] 已刷新商品列表，今日刷新次数已用");
                refreshButton.interactable = false;

                // 实际应用中可能需要调用服务重新加载商品
                _eventBus?.Publish(new VIPShopRefreshEvent(_config.VIPLevel));
            }
        }

        private async void OnVIPItemClicked(VIPItemDefinition item)
        {
            int discountedPrice = (int)(item.OriginalPrice * (100 - _config.DiscountPercentage) / 100f);

            bool success = await _shop.PurchaseItem(item.ItemId, discountedPrice);
            _eventBus?.Publish(new ShopPurchaseEvent(item.ItemId, discountedPrice, success));

            if (success)
            {
                Debug.Log($"[VIP 购买] {item.DisplayName} 成功，原价: {item.OriginalPrice}，折扣价: {discountedPrice}");
            }
            else
            {
                Debug.Log($"[VIP 购买] {item.DisplayName} 失败");
            }
        }

        private void OnDestroy()
        {
            _disposables?.Dispose();
            _vipItemDisposables?.Dispose();
            Debug.Log("[UIShopPanelVIP] 已销毁");
        }
    }

    /// <summary>
    /// VIP 商店配置，工厂通过此类切换不同 VIP 等级的面板
    /// </summary>
    public sealed class VIPShopConfig
    {
        public int VIPLevel { get; }
        public int DiscountPercentage { get; }
        public bool CanRefreshDaily { get; }
        public string[] Privileges { get; }
        public VIPItemDefinition[] VIPExclusiveItems { get; }

        public VIPShopConfig(
            int vipLevel,
            int discountPercentage,
            bool canRefreshDaily,
            string[] privileges,
            VIPItemDefinition[] vipExclusiveItems)
        {
            VIPLevel = vipLevel;
            DiscountPercentage = discountPercentage;
            CanRefreshDaily = canRefreshDaily;
            Privileges = privileges ?? System.Array.Empty<string>();
            VIPExclusiveItems = vipExclusiveItems ?? System.Array.Empty<VIPItemDefinition>();
        }

        public static VIPShopConfig CreateDefault()
        {
            return new VIPShopConfig(
                vipLevel: 1,
                discountPercentage: 5,
                canRefreshDaily: true,
                privileges: new[]
                {
                    "每天 1 次免费刷新",
                    "商品享 5 折优惠",
                    "优先购买限定商品"
                },
                vipExclusiveItems: new[]
                {
                    new VIPItemDefinition("vip_sword", "黄金剑", 500),
                    new VIPItemDefinition("vip_shield", "钻石盾", 600),
                    new VIPItemDefinition("vip_potion", "高级药水", 200)
                });
        }

        public static VIPShopConfig CreateVIP3()
        {
            return new VIPShopConfig(
                vipLevel: 3,
                discountPercentage: 15,
                canRefreshDaily: true,
                privileges: new[]
                {
                    "每天 3 次免费刷新",
                    "商品享 15 折优惠",
                    "优先购买限定商品",
                    "每月赠送 500 金币"
                },
                vipExclusiveItems: new[]
                {
                    new VIPItemDefinition("vip3_legendary_sword", "传奇剑", 1000),
                    new VIPItemDefinition("vip3_legendary_shield", "传奇盾", 1200),
                    new VIPItemDefinition("vip3_elixir", "仙丹", 800),
                    new VIPItemDefinition("vip3_pet_egg", "宠物蛋", 2000)
                });
        }
    }

    /// <summary>
    /// VIP 独占商品定义
    /// </summary>
    public sealed class VIPItemDefinition
    {
        public string ItemId { get; }
        public string DisplayName { get; }
        public int OriginalPrice { get; }

        public VIPItemDefinition(string itemId, string displayName, int originalPrice)
        {
            ItemId = itemId;
            DisplayName = displayName;
            OriginalPrice = originalPrice;
        }
    }

    /// <summary>
    /// VIP 商店刷新事件
    /// </summary>
    public sealed class VIPShopRefreshEvent
    {
        public int VIPLevel { get; }

        public VIPShopRefreshEvent(int vipLevel)
        {
            VIPLevel = vipLevel;
        }
    }
}
