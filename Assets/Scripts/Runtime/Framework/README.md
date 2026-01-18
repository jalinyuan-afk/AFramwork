# TradeGame 框架使用指南

基于 VContainer + UniRx + UniTask 构建的独立游戏框架，专为模拟经营、跑商、角色养成、沙盒模拟等类型游戏设计。

## 核心特性

- **依赖注入 (VContainer)**：统一管理游戏服务生命周期
- **响应式事件 (UniRx)**：基于观察者模式的事件总线
- **异步任务 (UniTask)**：高性能异步任务调度
- **模块化设计**：各系统解耦，易于扩展

## 快速开始

### 1. 安装依赖

确保项目中已安装以下包：
- **VContainer** (已通过 manifest.json 添加)
- **UniRx** (已存在于 Plugins 文件夹)
- **UniTask** (已存在于 Plugins 文件夹)

### 2. 初始化框架

在场景中创建 GameObject 并添加 `Bootstrapper` 组件：

```csharp
// 自动初始化（默认）
// 或通过代码手动初始化
Bootstrapper bootstrapper = FindObjectOfType<Bootstrapper>();
bootstrapper.Build();
```

### 3. 使用依赖注入

#### 注册服务

实现 `IServiceRegistrar` 接口：

```csharp
public class MyServiceRegistrar : MonoBehaviour, IServiceRegistrar
{
    public void RegisterServices(IContainerBuilder builder)
    {
        builder.Register<MyService>(Lifetime.Singleton).As<IMyService>();
    }
}
```

#### 解析服务

```csharp
// 通过静态方法
var configService = Bootstrapper.Resolve<IConfigService>();

// 通过构造函数注入（推荐）
public class MySystem
{
    private readonly IEventBus _eventBus;
    private readonly ITaskScheduler _taskScheduler;

    public MySystem(IEventBus eventBus, ITaskScheduler taskScheduler)
    {
        _eventBus = eventBus;
        _taskScheduler = taskScheduler;
    }
}
```

### 4. 使用事件总线

#### 发布事件

```csharp
IEventBus eventBus = Bootstrapper.Resolve<IEventBus>();

// 发布简单事件
eventBus.Publish(new PlayerLevelUpEvent { Level = 10 });

// 发布带数据的事件
eventBus.Publish("金币数量", 100);
```

#### 订阅事件

```csharp
// 自动订阅（返回 IDisposable，记得销毁）
IDisposable subscription = eventBus.Subscribe<PlayerLevelUpEvent>(e =>
{
    Debug.Log($"玩家升级到 {e.Level} 级");
});

// 带条件的订阅
subscription = eventBus.Subscribe<PlayerLevelUpEvent>(
    e => Debug.Log($"升级到 {e.Level}"),
    e => e.Level > 5
);

// 取消订阅
subscription.Dispose();
```

### 5. 使用异步任务调度器

```csharp
ITaskScheduler scheduler = Bootstrapper.Resolve<ITaskScheduler>();

// 执行异步任务
scheduler.Run(async ct =>
{
    await UniTask.Delay(1000, cancellationToken: ct);
    Debug.Log("任务完成");
}, "我的任务");

// 延迟执行
scheduler.RunDelayed(TimeSpan.FromSeconds(2), async ct =>
{
    Debug.Log("2秒后执行");
});

// 顺序执行多个任务
scheduler.RunSequential(new Func<CancellationToken, UniTask>[]
{
    async ct => await LoadResources(ct),
    async ct => await InitializeUI(ct),
    async ct => await StartGame(ct)
});



### 存档服务 (基于 EasySave3)

根据用户需求，存档服务应基于 EasySave3 实现。示例接口：

```csharp
public interface ISaveService
{
    UniTask SaveAsync<T>(string key, T data);
    UniTask<T> LoadAsync<T>(string key);
    UniTask<bool> DeleteAsync(string key);
}

// 实现示例见 Assets/TradeGame/Scripts/Runtime/Framework/Services/EasySave3SaveService.cs
```

## 目录结构
Assets/TradeGame/Scripts/Runtime/Framework/
├── Async/
│   └── TaskScheduler.cs ← ITaskScheduler (异步任务调度器接口)
├── DependencyInjection/
│   ├── Bootstrapper.cs ← Bootstrapper (容器引导程序：负责初始化VContainer容器并注册全局服务)
│   └── IServiceRegistrar.cs ← IServiceRegistrar (服务注册器接口：允许模块化注册服务到VContainer容器)
└── Reactive/
    └── EventBus.cs ← IEventBus (事件总线接口：基于UniRx的发布-订阅模式)

## 扩展指南

### 添加新服务

1. 定义接口 `IMyService`
2. 实现类 `MyService`
3. 注册到容器：
   - 在 `Bootstrapper.RegisterFrameworkServices` 中注册
   - 或通过 `IServiceRegistrar` 注册

## 最佳实践

1. **始终使用依赖注入**：避免使用单例模式，通过构造函数注入依赖
2. **事件驱动架构**：使用事件总线解耦系统间通信
3. **异步优先**：使用 UniTask 替代协程和回调
4. **插件化设计**：将功能模块设计为可插拔的插件
5. **错误处理**：所有异步操作都应处理取消和异常

## 故障排除

### 容器构建失败

- 检查所有注册的服务是否有循环依赖
- 确保所有需要的服务都已注册
- 查看控制台日志中的详细错误信息

### 事件总线内存泄漏

- 确保订阅在对象销毁时取消
- 使用 `CompositeDisposable` 管理多个订阅
- 避免在事件处理中进行耗时操作

### 异步任务未执行

- 检查 CancellationToken 是否正确传递
- 确保任务调度器已正确初始化
- 使用 `Debug.Log` 跟踪任务执行流程

