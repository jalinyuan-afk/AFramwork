using UnityEngine;
using UnityEngine.UI;
using TradeGame.Runtime.Framework;
using UniRx;
using TMPro;

namespace TradeGame.Runtime
{
    public class UIShopPanelDI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI goldText;// 显示金币数量的文本
        [SerializeField] private TextMeshProUGUI titleText;// 商店标题文本
        [SerializeField] private RectTransform itemsRoot;// 商品按钮的父节点
        [SerializeField] private Button itemButtonPrefab;// 商品按钮预制体
        [SerializeField] private GameObject vipBadge;// VIP 徽章

        private IShopService _shop;
        private IEventBus _eventBus;
        private ShopPanelConfig _config;
        private CompositeDisposable _disposables = new();
        private readonly CompositeDisposable _itemButtonDisposables = new();



        /// <summary>
        /// 公开的初始化方法，用于工厂模式或手动依赖注入
        /// </summary>
        public void Initialize(IShopService shop, IEventBus eventBus, ShopPanelConfig config)
        {
            _shop = shop ?? throw new System.ArgumentNullException(nameof(shop));
            _eventBus = eventBus ?? throw new System.ArgumentNullException(nameof(eventBus));
            _config = config ?? ShopPanelConfig.CreateDefault();
            Debug.Log("[UIShopPanelDI] 通过 Initialize 方法注入依赖成功");
        }

        private void Start()
        {
            // 如果已通过 Initialize 方法注入，则跳过从容器获取
            if (_shop != null && _eventBus != null)
            {
                Debug.Log("[UIShopPanelDI] 依赖已通过 Initialize 注入，跳过 Bootstrapper.Resolve");
                ApplyConfig();
                SubscribeToEvents();
                return;
            }
            // ✅ 临时修复：从 Bootstrapper 获取依赖
            if (_shop == null)
            {
                _shop = Bootstrapper.Resolve<IShopService>();
            }
            if (_eventBus == null)
            {
                _eventBus = Bootstrapper.Resolve<IEventBus>();
            }

            if (_config == null)
            {
                _config = ShopPanelConfig.CreateDefault();
            }

            ApplyConfig();
            SubscribeToEvents();
            Debug.Log("[UIShopPanelDI] 初始化完成");
        }

        private void SubscribeToEvents()
        {
            _shop.OnGoldChanged.Subscribe(gold => UpdateGoldUI(gold)).AddTo(_disposables);

        }

        private void OnDestroy()
        {
            _disposables?.Dispose();
            _itemButtonDisposables?.Dispose();
            Debug.Log("[UIShopPanelDI] 已销毁，订阅已自动释放");
        }

        private void UpdateGoldUI(int gold)
        {
            if (goldText != null)
            {
                goldText.text = $"金币: {gold}";
            }
            Debug.Log($"[UI更新] 当前金币: {gold}");
        }



        /// <summary>
        /// 根据注入的配置动态构建 UI。这里展示工厂模式的价值：
        /// - 不同平台/玩家等级切换不同标题和徽章
        /// - 根据配置动态生成商品按钮
        /// - 支持测试时注入自定义数据
        /// </summary>
        private void ApplyConfig()
        {
            if (titleText != null)
            {
                titleText.text = _config.Title;
            }

            if (vipBadge != null)
            {
                vipBadge.SetActive(_config.ShowVipBadge);
            }

            BuildItemButtons();
        }

        private void BuildItemButtons()
        {
            _itemButtonDisposables.Clear();

            if (itemsRoot == null || itemButtonPrefab == null)
            {
                Debug.LogWarning("[UIShopPanelDI] 未配置 itemsRoot 或 itemButtonPrefab，跳过动态商品构建");
                return;
            }

            // 清空旧节点
            for (int i = itemsRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(itemsRoot.GetChild(i).gameObject);
            }

            foreach (var item in _config.Items)
            {
                var button = Instantiate(itemButtonPrefab, itemsRoot);
                var label = button.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = item.DisplayName;
                }

                button.OnClickAsObservable()
                    .Subscribe(_ => OnDynamicItemClicked(item))
                    .AddTo(_itemButtonDisposables);
            }
        }

        private async void OnDynamicItemClicked(ShopItemDefinition item)
        {
            bool success = await _shop.PurchaseItem(item.ItemId, item.Price);
            _eventBus?.Publish(new ShopPurchaseEvent(item.ItemId, item.Price, success));

            if (success)
            {
                Debug.Log($"购买 {item.DisplayName} 成功，价格: {item.Price}");
            }
            else
            {
                Debug.Log($"购买 {item.DisplayName} 失败，价格: {item.Price}");
            }
        }
    }

    /// <summary>
    /// 工厂可注入的配置对象，用于切换不同的 UI 版本和商品列表。
    /// </summary>
    public sealed class ShopPanelConfig
    {
        public string Title { get; }
        public bool ShowVipBadge { get; }
        public ShopItemDefinition[] Items { get; }

        public ShopPanelConfig(string title, bool showVipBadge, ShopItemDefinition[] items)
        {
            Title = title;
            ShowVipBadge = showVipBadge;
            Items = items ?? System.Array.Empty<ShopItemDefinition>();
        }

        public static ShopPanelConfig CreateDefault()
        {
            return new ShopPanelConfig(
                title: "基础商店",
                showVipBadge: false,
                items: new[]
                {
                    new ShopItemDefinition("sword", "铁剑", 100),
                    new ShopItemDefinition("shield", "圆盾", 120)
                });
        }
    }

    /// <summary>
    /// 商品定义，支持由工厂注入不同的列表。
    /// </summary>
    public sealed class ShopItemDefinition
    {
        public string ItemId { get; }
        public string DisplayName { get; }
        public int Price { get; }

        public ShopItemDefinition(string itemId, string displayName, int price)
        {
            ItemId = itemId;
            DisplayName = displayName;
            Price = price;
        }
    }

    /// <summary>
    /// 购买事件，供 EventBus 使用。
    /// </summary>
    public sealed class ShopPurchaseEvent
    {
        public string ItemId { get; }
        public int Price { get; }
        public bool Success { get; }

        public ShopPurchaseEvent(string itemId, int price, bool success)
        {
            ItemId = itemId;
            Price = price;
            Success = success;
        }
    }
}
