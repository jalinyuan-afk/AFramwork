# VContainer 依赖注入学习文档

面向本项目的 VContainer 使用笔记，结合 `Bootstrapper` 和 `IServiceRegistrar` 的实际代码，帮助在 Unity 场景中稳定地完成依赖注入。

## 核心概念
- `Bootstrapper`：位于 [Assets/Scripts/Runtime/Framework/DependencyInjection/Bootstrapper.cs](Assets/Scripts/Runtime/Framework/DependencyInjection/Bootstrapper.cs)，继承 `LifetimeScope`，负责容器构建与核心服务注册。
- `IServiceRegistrar`：位于 [Assets/Scripts/Runtime/Framework/DependencyInjection/IServiceRegistrar.cs](Assets/Scripts/Runtime/Framework/DependencyInjection/IServiceRegistrar.cs)，用于模块化注册服务，常由场景中的 MonoBehaviour 持有。
- 生命周期：`Singleton` 全局唯一、`Scoped` 每个作用域一份、`Transient` 每次解析创建新实例。

## 初始化与注册
1. **场景配置**：在首个场景放置 `Bootstrapper` 组件，`autoRun` 开启即可自动构建；若需延迟构建，调用 `Build()`。
2. **注册框架服务**：`Bootstrapper.RegisterFrameworkServices` 内已注册事件总线、任务调度等核心服务。
3. **模块化注册**：通过 `IServiceRegistrar` 扩展：
```csharp
public class ShopServiceRegistrar : MonoBehaviour, IServiceRegistrar
{
    public void RegisterServices(IContainerBuilder builder)
    {
        builder.Register<ShopService>(Lifetime.Singleton).As<IShopService>();
    }
}
```
在 Inspector 的 `_serviceRegistrars` 列表添加该组件，或留空让 `Bootstrapper` 自动查找场景中的实现。
4. **安装器 (IInstaller)**：若需复用一组注册逻辑，可实现 `IInstaller` 并放入 `_additionalInstallers`。

## 解析与注入模式
- **构造函数注入（推荐）**：
```csharp
public class InventorySystem
{
    private readonly IEventBus _eventBus;
    private readonly ITaskScheduler _scheduler;

    public InventorySystem(IEventBus eventBus, ITaskScheduler scheduler)
    {
        _eventBus = eventBus;
        _scheduler = scheduler;
    }
}
```
- **MonoBehaviour 注入**：让组件由容器创建，或在 Awake 使用 `[Inject]` 标记字段/方法。
- **静态解析（仅兜底）**：`Bootstrapper.Resolve<T>()`/`TryResolve<T>` 仅在无法注入的场景下使用。

## 常用注册写法
```csharp
// 单例服务
builder.Register<AudioService>(Lifetime.Singleton).As<IAudioService>();

// 工厂或 Lazy 解决轻微循环依赖
builder.Register<BuffService>(Lifetime.Singleton).As<IBuffService>();
builder.RegisterFactory<EnemyFactory>(Lifetime.Singleton);

// 配置脚本化对象
builder.RegisterInstance(gameSettings).As<IGameSettings>();
```

## 场景与作用域建议
- UI、系统管理类：`Singleton`
- 每局/每关状态：`Scoped` 子作用域或按需创建的 `LifetimeScope`
- 临时对象或算法：`Transient`

## 调试与排错
- 构建失败时检查是否有循环依赖，必要时改用 `Lazy<T>` 或 `Func<T>`。
- 确认所有被解析的接口都已注册；在 `Configure` 中按模块分段注册，便于审计。
- 在编辑器模式下，可在 `Bootstrapper` 上勾选日志或自行添加日志输出。

## 代码规范速查
- 接口使用 `I` 前缀，异步方法使用 `Async` 后缀。
- 优先依赖注入，避免单例全局状态；MonoBehaviour 无法注入时再使用 `Resolve`。
- 资源清理交给拥有者（如 `IDisposable`，或在 `OnDestroy` 主动释放）。
