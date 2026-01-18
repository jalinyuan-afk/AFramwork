using UnityEngine;
using TradeGame.Runtime.Framework;
using UniRx;

namespace TradeGame.Runtime
{
    /// <summary>
    /// 演示如何通过容器正确创建和注入依赖到 MonoBehaviour
    /// 这是 VContainer 与 MonoBehaviour 集成的标准做法
    /// </summary>
    public class ShopUIFactory : MonoBehaviour
    {
        /// <summary>
        /// 方案 1：最简单 - 在 Start 中手动获取依赖并创建 UI
        /// ✅ 推荐用于快速开发
        /// </summary>
        public void CreateShopPanelSimple()
        {
            Debug.Log("[ShopUIFactory] 方案 1：简单创建");

            // 第一步：从 Bootstrapper 容器获取依赖
            var shopService = Bootstrapper.Resolve<IShopService>();
            var eventBus = Bootstrapper.Resolve<IEventBus>();

            // 验证依赖是否成功获取
            if (shopService == null || eventBus == null)
            {
                Debug.LogError("[ShopUIFactory] 容器中未找到所需服务，请检查 Bootstrapper 配置");
                return;
            }

            // 第二步：创建 UI GameObject
            var panelGo = new GameObject("ShopPanel");
            var panel = panelGo.AddComponent<UIShopPanelDI>();

            // 第三步：通过公开方法或字段手动设置依赖
            // （如果 UIShopPanelDI 提供了 public setter）

            Debug.Log("[ShopUIFactory] ✓ ShopPanel 创建完成");
        }

        /// <summary>
        /// 方案 2：使用专用工厂方法
        /// ✅ 推荐用于生产代码
        /// </summary>
        public UIShopPanelDI CreateShopPanelWithFactory(ShopPanelConfig config = null)
        {
            Debug.Log("[ShopUIFactory] 方案 2：工厂方法创建");

            // 获取依赖
            var shopService = Bootstrapper.Resolve<IShopService>();
            var eventBus = Bootstrapper.Resolve<IEventBus>();

            if (shopService == null || eventBus == null)
            {
                Debug.LogError("[ShopUIFactory] 服务解析失败");
                return null;
            }

            // 创建实例
            var panelGo = new GameObject("ShopPanel");
            var panel = panelGo.AddComponent<UIShopPanelDI>();

            // 通过构造函数手动设置（需要提供公开方法）
            panel.Initialize(shopService, eventBus, config ?? ShopPanelConfig.CreateDefault());

            Debug.Log("[ShopUIFactory] ✓ ShopPanel 工厂创建完成");
            return panel;
        }

        /// <summary>
        /// 方案 3：从预制体创建并注入依赖
        /// ✅ 推荐用于 UI 资源管理
        /// </summary>
        public UIShopPanelDI CreateShopPanelFromPrefab(ShopPanelConfig config = null)
        {
            Debug.Log("[ShopUIFactory] 方案 3：从预制体创建");

            // 获取依赖
            var shopService = Bootstrapper.Resolve<IShopService>();
            var eventBus = Bootstrapper.Resolve<IEventBus>();

            if (shopService == null || eventBus == null)
            {
                Debug.LogError("[ShopUIFactory] 服务解析失败");
                return null;
            }

            // 加载预制体
            var prefab = Resources.Load<UIShopPanelDI>("Prefabs/UIShopPanel");
            if (prefab == null)
            {
                Debug.LogError("[ShopUIFactory] 预制体加载失败: Prefabs/UIShopPanel");
                return null;
            }

            // 从预制体实例化
            var panel = Instantiate(prefab);

            // 注入依赖
            panel.Initialize(shopService, eventBus, config ?? ShopPanelConfig.CreateDefault());

            Debug.Log("[ShopUIFactory] ✓ ShopPanel 从预制体创建完成");
            return panel;
        }
    }
}
