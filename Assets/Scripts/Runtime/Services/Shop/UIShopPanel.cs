using UnityEngine;
using UnityEngine.UI;
using TradeGame.Runtime.Framework;
using UniRx;
using TMPro;
namespace TradeGame.Runtime
{
    public class UIShopPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private Button purchaseButton;

        private IShopService _shop;
        private IEventBus _eventBus;
        private CompositeDisposable _disposables = new();

        // 通过构造函数注入（如果 UIShopPanel 由容器创建）
        // 或通过 Bootstrapper.Resolve 获取（对于 MonoBehaviour）
        private void Start()
        {
            _shop = Bootstrapper.Resolve<IShopService>();
            _eventBus = Bootstrapper.Resolve<IEventBus>();

            // 订阅金币变化
            _shop.OnGoldChanged.Subscribe(gold => UpdateGoldUI(gold)).AddTo(_disposables);

            // 绑定按钮点击事件
            if (purchaseButton != null)
            {
                purchaseButton.OnClickAsObservable()
                    .Subscribe(_ => OnPurchaseButtonClicked())
                    .AddTo(_disposables);
            }
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }

        private void UpdateGoldUI(int gold)
        {
            if (goldText != null)
            {
                goldText.text = $"金币: {gold}";
            }
            Debug.Log($"更新UI - 当前金币: {gold}");
        }

        public async void OnPurchaseButtonClicked()
        {
            bool success = await _shop.PurchaseItem("sword", 1);
            if (success)
            {
                Debug.Log("购买成功！");
            }
            else
            {
                Debug.Log("购买失败！");
            }
        }
    }
}