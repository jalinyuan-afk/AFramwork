# UniTask 异步任务学习文档

围绕项目内的 `ITaskScheduler` 与 UniTask 使用方式，帮助在游戏逻辑中编写可取消、可跟踪的异步流程。

## 核心位置
- 任务调度接口： [Assets/Scripts/Runtime/Framework/Async/ITaskScheduler.cs](Assets/Scripts/Runtime/Framework/Async/ITaskScheduler.cs)
- 默认实现： [Assets/Scripts/Runtime/Framework/Async/TaskScheduler.cs](Assets/Scripts/Runtime/Framework/Async/TaskScheduler.cs)
- 扩展方法： [Assets/TradeGame/Scripts/Runtime/Framework/Async/TaskSchedulerExtensions.cs](Assets/TradeGame/Scripts/Runtime/Framework/Async/TaskSchedulerExtensions.cs)

## 基本用法
```csharp
// 通过依赖注入获取
private readonly ITaskScheduler _scheduler;

// 运行带取消令牌的任务
UniTask loadTask = _scheduler.Run(async ct =>
{
    await UniTask.Delay(500, cancellationToken: ct);
    await LoadAssetsAsync(ct);
}, "LoadAssets");

// 顺序执行一组任务
_scheduler.RunSequential(new Func<CancellationToken, UniTask>[]
{
    ct => CheckVersion(ct),
    ct => InitializeSDK(ct),
    ct => WarmUpUI(ct)
});

// 延迟任务或 Fire-and-forget
_scheduler.RunDelayed(TimeSpan.FromSeconds(2), ct => PlayIntro(ct));
_scheduler.RunFireAndForget(ct => TrackAnalytics(ct));
```

## 取消与清理
- 所有内部异步调用都应传递上游 `CancellationToken`。
- 长耗时逻辑定期检查 `ct.IsCancellationRequested` 并提前退出。
- 在 `Dispose` 或场景切换时调用 `_scheduler.CancelAll()` 视需求终止任务。

## 编写可测试的异步代码
- 返回 `UniTask` 以便单元测试直接 `await`。
- 避免在任务内部依赖全局单例；通过构造函数注入依赖，或在测试中提供替身实现。
- 使用 `RunWithId` 获取任务 ID，验证取消与完成状态。

## 常见模式
- **并行加载 + UI 进度**：启动多个任务并发布进度事件，由 UI 订阅显示。
- **节流操作**：与 UniRx 结合使用，在事件订阅内调用异步方法并使用令牌取消旧任务。
- **主线程更新**：默认 UniTask 在当前上下文恢复，必要时使用 `UniTask.SwitchToMainThread()` 确保操作 UI。

## 反模式提醒
- 不要在异步方法中使用 `Task.Run` 处理与 Unity 主线程交互的逻辑。
- 避免遗漏取消令牌，尤其是场景切换或对象销毁时的持久任务。
- Fire-and-forget 仅用于可安全忽略的操作，重要任务应返回 `UniTask` 并被等待。
