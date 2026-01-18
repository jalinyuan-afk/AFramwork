# AFramework / TradeGame 概览

基于 **VContainer**（依赖注入）+ **UniRx**（响应式事件）+ **UniTask**（异步任务）的 Unity 游戏框架，面向模拟经营、跑商、养成与沙盒类项目。本文档用于全局导航与快速上手。

## 快速开始
- 打开项目，首场景放置 `Bootstrapper` 组件并保持 `autoRun` 开启；需要延迟初始化时调用 `Build()`。
- 在 Inspector 的 `_serviceRegistrars` 配置模块化注册器，或让框架自动扫描场景中的 `IServiceRegistrar`。
- 通过依赖注入获取核心服务：`IEventBus`（事件总线）、`ITaskScheduler`（任务调度），避免直接依赖单例。
- UI/游戏系统使用 UniRx 订阅事件更新，长耗时逻辑交给 UniTask 与调度器执行。

## 主要目录
- 框架代码与指南： [Assets/Scripts/Runtime/Framework](Assets/Scripts/Runtime/Framework)
- 深度解析指南： [Assets/Scripts/Runtime/Framework/TradeGame 框架深度解析与使用指南.md](Assets/Scripts/Runtime/Framework/TradeGame%20%E6%A1%86%E6%9E%B6%E6%B7%B1%E5%BA%A6%E8%A7%A3%E6%9E%90%E4%B8%8E%E4%BD%BF%E7%94%A8%E6%8C%87%E5%8D%97.md)
- 核心模块示例与 API： [Assets/Scripts/Runtime/Framework/README.md](Assets/Scripts/Runtime/Framework/README.md)
- AI 辅助与模板： [docs/AI-Skills](docs/AI-Skills)

## 核心模块一览
- **依赖注入 (VContainer)**：`Bootstrapper` 管理容器，`IServiceRegistrar` 模块化注册，支持 `Singleton/Scoped/Transient` 生命周期。
- **响应式事件 (UniRx)**：`IEventBus` 基于 Subject 管理发布/订阅，使用 `CompositeDisposable` 管理生命周期，支持条件订阅与 Rx 操作符。
- **异步任务 (UniTask)**：`ITaskScheduler` 跟踪、取消、顺序/并行执行任务，扩展方法提供 `RunDelayed`、`RunSequential`、`RunFireAndForget` 等。

## 学习与实践文档
- 依赖注入： [docs/Learning/VContainer.md](docs/Learning/VContainer.md)
- 响应式编程： [docs/Learning/UniRx.md](docs/Learning/UniRx.md)
- 异步任务： [docs/Learning/UniTask.md](docs/Learning/UniTask.md)

## 任务与事件协作示例
1. 在启动流程中使用 `ITaskScheduler.RunSequential` 顺序执行版本检查、SDK 初始化、配置加载。
2. 在每一步完成后通过 `IEventBus.Publish` 推送进度事件，UI 侧以 UniRx 订阅并更新进度条。
3. 场景切换或对象销毁时，通过 `CompositeDisposable.Dispose` 释放订阅，并根据需要 `CancelAll` 终止未完成任务。

## 推荐实践
- 优先构造函数注入，MonoBehaviour 无法注入时再使用 `Bootstrapper.Resolve<T>()` 兜底。
- 所有异步调用传递上游 `CancellationToken`，并在耗时循环内检查取消。
- 订阅回调保持轻量，耗时工作放入异步任务，必要时使用 `ObserveOnMainThread()` 与 `UniTask.SwitchToMainThread()`。
- 为任务与事件命名，便于调试与日志。
