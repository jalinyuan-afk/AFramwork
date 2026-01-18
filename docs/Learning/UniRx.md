# UniRx 响应式编程学习文档

结合框架内的事件总线，说明如何在项目中使用 UniRx 构建解耦的事件流与 UI 更新。

## 核心位置
- 事件总线接口与实现： [Assets/Scripts/Runtime/Framework/Reactive/EventBus.cs](Assets/Scripts/Runtime/Framework/Reactive/EventBus.cs)
- 事件基类：`GameEvent` / `GameEvent<TData>`，支持携带数据与时间戳。

## 发布与订阅模式
```csharp
// 解析事件总线（推荐通过构造函数注入）
private readonly IEventBus _eventBus;

// 发布强类型事件
_eventBus.Publish(new PlayerLevelUpEvent(5));

// 发布带数据的事件（扩展方法自动包装）
_eventBus.Publish("玩家金币变化", 1200);

// 订阅并管理生命周期
private CompositeDisposable _disposables = new();

void Start()
{
    _eventBus.Subscribe<PlayerLevelUpEvent>(e => ShowLevel(e.Data))
        .AddTo(_disposables);

    _eventBus.Subscribe<int>("玩家金币变化", gold => UpdateGold(gold))
        .AddTo(_disposables);
}

void OnDestroy()
{
    _disposables.Dispose();
}
```

## 进阶 Rx 操作符示例
```csharp
// 节流：1 秒内只响应最后一次事件
_eventBus.AsObservable<PlayerLevelUpEvent>()
         .Throttle(TimeSpan.FromSeconds(1))
         .Subscribe(e => ShowLevel(e.Data))
         .AddTo(_disposables);

// 过滤：只响应等级大于 10 的升级
_eventBus.AsObservable<PlayerLevelUpEvent>()
         .Where(e => e.Data > 10)
         .Subscribe(e => Announce(e.Data))
         .AddTo(_disposables);

// 合并 UI 输入与定时器
Observable.CombineLatest(_attackStream, Observable.Timer(TimeSpan.FromSeconds(5)))
          .Subscribe(_ => TriggerSkill())
          .AddTo(_disposables);
```

## 最佳实践
- **生命周期管理**：所有订阅返回的 `IDisposable` 必须托管到 `CompositeDisposable` 并在合适时机释放。
- **事件粒度**：保持事件细粒度，避免“大而全”事件类型；按需拆分为多个独立事件。
- **避免阻塞**：订阅回调应保持轻量，耗时操作转到异步（使用 `ITaskScheduler`）。
- **线程与主线程**：Unity 相关操作放在主线程；必要时使用 `ObserveOnMainThread()`。
- **测试**：在单元测试中可直接对 `EventBus` 进行发布与订阅，验证事件流是否按预期触发。

## 常见排错
- 收不到事件：确认事件类型完全匹配（泛型参数也需一致），或是否在释放前意外 `Dispose`。
- 内存泄漏：检查是否遗漏 `Dispose`，或订阅存活时间超出对象生命周期。
- 过量事件：使用 `Throttle`/`Sample`/`DistinctUntilChanged` 降噪，或在发布端增加判重逻辑。
