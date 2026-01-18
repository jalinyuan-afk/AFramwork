# VContainer 与 MonoBehaviour 集成常见问题

## 问题根源

### ❌ 为什么 UIShopPanelDI 的构造函数注入不工作？

MonoBehaviour 的生命周期由 Unity 引擎管理，VContainer 无法：
1. 自动调用 MonoBehaviour 的构造函数
2. 自动创建 MonoBehaviour 实例
3. 将依赖注入到构造函数参数

```
Unity 创建 MonoBehaviour
  ↓
调用 Awake() 
  ↓
（不经过构造函数，依赖为 null）
  ↓
调用 Start() 
  ↓
NullReferenceException！
```

---

## 解决方案总结

| 方案 | 难度 | 推荐度 | 场景 |
|------|------|--------|------|
| **方案1：IInstantiator 工厂** | ⭐⭐⭐ | ✅✅✅ | 标准做法 |
| **方案2：在 Start 中手动注入** | ⭐ | ✅ | 快速原型 |
| **方案3：使用字段注入属性** | ⭐⭐ | ✅✅ | 折中方案 |
| **方案4：Prefab Factory** | ⭐⭐⭐⭐ | ⭐ | 高级场景 |

---

## 📌 方案 1：使用 IInstantiator（推荐）

VContainer 提供 `IInstantiator` 接口，可以创建并注入 MonoBehaviour：

### 实现步骤

#### 步骤 1：修改 UIShopPanelDI 的构造函数注入为公开字段

```csharp
public class UIShopPanelDI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button purchaseButton;

    // 不用构造函数，而是用公开字段
    // VContainer 会自动注入到这些字段
    [Inject] public IShopService Shop { get; private set; }
    [Inject] public IEventBus EventBus { get; private set; }
    
    private CompositeDisposable _disposables = new();

    private void Start()
    {
        // 现在 Shop 和 EventBus 已经被注入
        Shop.OnGoldChanged.Subscribe(gold => UpdateGoldUI(gold)).AddTo(_disposables);
        
        if (purchaseButton != null)
        {
            purchaseButton.OnClickAsObservable()
                .Subscribe(_ => OnPurchaseButtonClicked())
                .AddTo(_disposables);
        }
    }

    // ... 其他方法
}
```

#### 步骤 2：在注册器中配置

```csharp
public class ShopServiceRegistrar : MonoBehaviour, IServiceRegistrar
{
    public void RegisterServices(IContainerBuilder builder)
    {
        builder.Register<ShopService>(Lifetime.Singleton).As<IShopService>();
        
        // 注册 IInstantiator（用于创建 MonoBehaviour）
        // 这通常由 Bootstrapper 自动注册，无需手动添加
    }
}
```

#### 步骤 3：在场景中创建并注入

```csharp
public class ShopPanelFactory : MonoBehaviour
{
    public void CreateShopPanel()
    {
        // 从 Bootstrapper 获取 IInstantiator
        var instantiator = Bootstrapper.Resolve<IInstantiator>();
        
        // 创建 UIShopPanelDI 实例并自动注入依赖
        var panelGo = new GameObject("ShopPanel");
        var panel = instantiator.CreateInstance<UIShopPanelDI>(panelGo);
        
        // 或者创建从预制体实例化
        var prefab = Resources.Load<UIShopPanelDI>("Prefabs/UIShopPanel");
        var panelInstance = instantiator.Instantiate(prefab);
    }
}
```

---

## 📌 方案 2：在 Start 中手动注入（最简单）

```csharp
public class UIShopPanelDI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button purchaseButton;

    private IShopService _shop;
    private IEventBus _eventBus;
    private CompositeDisposable _disposables = new();

    // ❌ 移除构造函数，改为在 Start 中获取依赖
    
    private void Start()
    {
        // 从 Bootstrapper 容器中解析（简单但不够"依赖注入"）
        _shop = Bootstrapper.Resolve<IShopService>();
        _eventBus = Bootstrapper.Resolve<IEventBus>();

        if (_shop == null)
        {
            Debug.LogError("[UIShopPanelDI] 无法解析 IShopService，请检查注册");
            return;
        }

        // 订阅金币变化
        _shop.OnGoldChanged.Subscribe(gold => UpdateGoldUI(gold)).AddTo(_disposables);

        // 绑定按钮点击事件
        if (purchaseButton != null)
        {
            purchaseButton.OnClickAsObservable()
                .Subscribe(_ => OnPurchaseButtonClicked())
                .AddTo(_disposables);
        }

        Debug.Log("[UIShopPanelDI] 初始化完成");
    }

    // ... 其他方法保持不变
}
```

**✅ 优点**：
- 简单直接
- 无需修改注册逻辑
- 立即可用

**❌ 缺点**：
- 不是真正的依赖注入（Service Locator 模式）
- 难以测试（无法注入 Mock 对象）

---

## 📌 方案 3：属性字段注入

```csharp
using VContainer;

public class UIShopPanelDI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button purchaseButton;

    // 通过 [Inject] 特性进行字段注入
    [Inject] private IShopService _shop;
    [Inject] private IEventBus _eventBus;
    
    private CompositeDisposable _disposables = new();

    // ❌ 不需要构造函数
    
    private void Start()
    {
        // _shop 和 _eventBus 已经被自动注入
        _shop.OnGoldChanged.Subscribe(gold => UpdateGoldUI(gold)).AddTo(_disposables);

        if (purchaseButton != null)
        {
            purchaseButton.OnClickAsObservable()
                .Subscribe(_ => OnPurchaseButtonClicked())
                .AddTo(_disposables);
        }
    }

    // ... 其他方法
}
```

**关键点**：
- 需要在 Bootstrapper 中配置 `ObjectResolver` 以支持属性注入
- 最新版本 VContainer 默认支持

---

## 🎯 正确的容器构建流程

### 完整示例

#### 1. Bootstrapper 配置（核心）

```csharp
public class Bootstrapper : LifetimeScope
{
    [SerializeField] private List<GameObject> _serviceRegistrars = new();

    protected override void Configure(IContainerBuilder builder)
    {
        // 注册框架服务
        RegisterFrameworkServices(builder);
        
        // 注册业务服务
        RegisterManualServices(builder);
        
        // 调用服务注册器
        InvokeServiceRegistrars(builder);
    }

    private void RegisterFrameworkServices(IContainerBuilder builder)
    {
        // 注册 EventBus
        builder.Register<EventBus>(Lifetime.Singleton).As<IEventBus>();
        
        // 注册 TaskScheduler
        builder.Register<TaskScheduler>(Lifetime.Singleton).As<ITaskScheduler>();
        
        // ✅ 关键：注册 IInstantiator（自动支持 MonoBehaviour 注入）
        builder.RegisterInstance<IObjectResolver>(builder);
    }

    private void RegisterManualServices(IContainerBuilder builder)
    {
        builder.Register<ShopService>(Lifetime.Singleton).As<IShopService>();
    }

    private void InvokeServiceRegistrars(IContainerBuilder builder)
    {
        if (_serviceRegistrars.Count > 0)
        {
            foreach (var go in _serviceRegistrars)
            {
                var registrar = go.GetComponent<IServiceRegistrar>();
                if (registrar != null)
                {
                    registrar.RegisterServices(builder);
                }
            }
        }
        else
        {
            var registrars = FindObjectsOfType<MonoBehaviour>()
                .OfType<IServiceRegistrar>();
            
            foreach (var registrar in registrars)
            {
                registrar.RegisterServices(builder);
            }
        }
    }

    public static T Resolve<T>()
    {
        var scope = FindObjectOfType<Bootstrapper>();
        return scope.Container.Resolve<T>();
    }
}
```

#### 2. 配置注册器

```csharp
public class ShopServiceRegistrar : MonoBehaviour, IServiceRegistrar
{
    public void RegisterServices(IContainerBuilder builder)
    {
        // 注册服务
        builder.Register<ShopService>(Lifetime.Singleton).As<IShopService>();
        
        // 注册 UI（可选，如果想通过容器创建）
        // builder.RegisterComponentInNewPrefab<UIShopPanelDI>(
        //     "Prefabs/ShopPanel", 
        //     Lifetime.Scoped
        // );
    }
}
```

#### 3. 在 Start 中获取依赖

```csharp
public class UIShopPanelDI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button purchaseButton;

    private IShopService _shop;
    private IEventBus _eventBus;
    private CompositeDisposable _disposables = new();

    private void Start()
    {
        // ✅ 方式 1：从 Bootstrapper 解析（简单）
        _shop = Bootstrapper.Resolve<IShopService>();
        _eventBus = Bootstrapper.Resolve<IEventBus>();

        // 检查是否成功获取
        if (_shop == null || _eventBus == null)
        {
            Debug.LogError("[UIShopPanelDI] 依赖解析失败");
            return;
        }

        SubscribeToEvents();
        Debug.Log("[UIShopPanelDI] 初始化完成");
    }

    private void SubscribeToEvents()
    {
        _shop.OnGoldChanged
            .Subscribe(gold => UpdateGoldUI(gold))
            .AddTo(_disposables);

        if (purchaseButton != null)
        {
            purchaseButton.OnClickAsObservable()
                .Subscribe(_ => OnPurchaseButtonClicked())
                .AddTo(_disposables);
        }
    }

    private void OnDestroy()
    {
        _disposables?.Dispose();
    }

    private void UpdateGoldUI(int gold)
    {
        if (goldText != null)
        {
            goldText.text = $"金币: {gold}";
        }
    }

    public async void OnPurchaseButtonClicked()
    {
        bool success = await _shop.PurchaseItem("sword", 1);
        if (success)
        {
            Debug.Log("✓ 购买成功");
        }
    }
}
```

---

## ⚠️ 容器构建时的注意事项

### 1️⃣ Bootstrapper 必须先初始化

```csharp
// ❌ 错误：Bootstrapper 还未初始化
void Start()
{
    var service = Bootstrapper.Resolve<IShopService>();  // null！
}

// ✅ 正确：等待 Bootstrapper 初始化完成
void Start()
{
    var bootstrapper = FindObjectOfType<Bootstrapper>();
    if (bootstrapper != null && bootstrapper.IsBuilt)  // 检查是否已构建
    {
        var service = Bootstrapper.Resolve<IShopService>();
    }
}
```

### 2️⃣ MonoBehaviour 构造函数永远不会被 VContainer 调用

```csharp
// ❌ 这个构造函数永远不会被调用
public UIShopPanelDI(IShopService shop, IEventBus eventBus)
{
    // 永远不会执行！
}

// ✅ 改用字段注入或在 Start 中手动获取
[Inject] private IShopService _shop;  // 自动注入
```

### 3️⃣ 注册顺序很重要

```csharp
// ❌ 错误：依赖关系反向
builder.Register<ShopService>().As<IShopService>();  // 先注册实现
builder.Register<EventBus>().As<IEventBus>();        // ShopService 依赖 IEventBus

// ✅ 正确：确保依赖已先注册
builder.Register<EventBus>().As<IEventBus>();        // 先注册依赖
builder.Register<ShopService>().As<IShopService>();  // 再注册使用它的服务
```

### 4️⃣ 生命周期设置要匹配

```csharp
// ❌ 可能出现问题
builder.Register<ShopService>(Lifetime.Transient)      // 每次创建新实例
    .As<IShopService>();

// ✅ 单例服务应该用 Singleton
builder.Register<ShopService>(Lifetime.Singleton)      // 全局单例
    .As<IShopService>();
```

### 5️⃣ 循环依赖会导致错误

```csharp
// ❌ 循环依赖
public class ServiceA
{
    public ServiceA(ServiceB b) { }  // 依赖 B
}

public class ServiceB
{
    public ServiceB(ServiceA a) { }  // B 又依赖 A
}

// ✅ 解决方案：使用 Lazy<T>
public class ServiceA
{
    private readonly Lazy<ServiceB> _b;
    public ServiceA(Lazy<ServiceB> b) { _b = b; }
}
```

---

## 📋 检查清单

使用容器构建时，检查以下几点：

- [ ] Bootstrapper 已添加到场景中
- [ ] Bootstrapper 的 `autoRun` 已勾选（或已手动调用 `Build()`）
- [ ] ShopServiceRegistrar 已添加到场景中
- [ ] 所有依赖都已注册（`builder.Register<T>()...`）
- [ ] MonoBehaviour 中**不使用构造函数注入**
- [ ] 在 Start() 中使用 `Bootstrapper.Resolve<T>()` 获取依赖
- [ ] 添加 null 检查，防止依赖未注册
- [ ] 在 OnDestroy() 中释放所有订阅（`_disposables.Dispose()`）

---

## 总结

### 关键要点

| 概念 | 说明 |
|------|------|
| **MonoBehaviour 限制** | Unity 生命周期，无法自动调用构造函数 |
| **推荐方案** | 在 Start() 中使用 `Bootstrapper.Resolve<T>()` |
| **最佳实践** | 用 `[Inject]` 属性字段结合 `IInstantiator` |
| **常见错误** | 构造函数注入、null 检查不足、注册顺序错误 |
| **调试技巧** | 添加日志检查 Bootstrapper 初始化时机 |

### 快速修复

如果遇到 NullReferenceException：

1. **检查 Bootstrapper 是否初始化**
   ```csharp
   Debug.Log($"Bootstrapper 已构建: {Bootstrapper != null}");
   ```

2. **检查服务是否注册**
   ```csharp
   var shop = Bootstrapper.Resolve<IShopService>();
   Debug.Log($"IShopService: {(shop == null ? "未注册" : "已注册")}");
   ```

3. **添加 null 检查**
   ```csharp
   if (_shop == null)
   {
       Debug.LogError("IShopService 为 null，请检查容器配置");
       return;
   }
   ```
