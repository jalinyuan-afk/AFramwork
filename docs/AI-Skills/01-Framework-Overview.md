# AFramework 框架总览 (AI Skill)

> **目标**: 让 AI 快速理解项目架构和技术选型

## 🏗️ 架构分层

```
┌─────────────────────────────────────┐
│      业务层 (Business Layer)        │
│  - ProcedureManager (流程管理)      │
│  - GameStartupController (启动流程) │
└─────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────┐
│      工具层 (Tool Layer)            │
│  - TaskScheduler (任务调度)         │
│  - EventBus (事件总线)              │
│  - LogManager (日志系统)            │
└─────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────┐
│      基础设施层 (Infrastructure)     │
│  - VContainer (依赖注入)            │
│  - UniTask (异步编程)               │
│  - UniRx (响应式编程)               │
│  - DOTween (动画系统)               │
└─────────────────────────────────────┘
```

## 📦 核心依赖

### VContainer v1.x
- **用途**: 依赖注入容器
- **配置文件**: `Bootstrapper.cs`
- **API 示例**:
  ```csharp
  // 注册服务
  builder.Register<ITaskScheduler, TaskScheduler>(Lifetime.Singleton);
  
  // 解析服务
  var service = Bootstrapper.Resolve<ITaskScheduler>();
  ```

### UniTask (Cysharp.Threading.Tasks)
- **用途**: Unity 优化的异步编程
- **替代**: Coroutine
- **API 示例**:
  ```csharp
  public async UniTask LoadAsync()
  {
      await UniTask.Delay(1000);
      await SceneManager.LoadSceneAsync("Main").ToUniTask();
  }
  ```

### UniRx
- **用途**: 响应式扩展
- **常用场景**: 事件流、定时器、订阅管理
- **API 示例**:
  ```csharp
  Observable.Timer(TimeSpan.FromSeconds(2))
      .Subscribe(_ => RotateTip())
      .AddTo(_disposables);
  ```

### DOTween
- **用途**: 补间动画
- **命名空间**: `DG.Tweening`
- **API 示例**:
  ```csharp
  _progressTweener?.Kill();
  _progressTweener = progressBar.DOValue(targetValue, 0.3f)
      .SetEase(Ease.OutCubic);
  ```

## 🔧 核心服务接口

### ITaskScheduler
- **作用**: 任务调度和执行管理
- **方法**:
  - `RunSequential(params Func<UniTask>[] tasks)` - 顺序执行
  - `Run(params Func<UniTask>[] tasks)` - 并行执行
  - `CancelTask(CancellationTokenSource cts)` - 取消任务

### IEventBus
- **作用**: 事件发布订阅
- **方法**:
  - `Publish<T>(T eventData)` - 发布事件
  - `Subscribe<T>(Action<T> handler)` - 订阅事件（返回 IDisposable）

### LogManager
- **作用**: 统一日志管理
- **方法**: `Log(string message, LogCategory category)`
- **日志分类**: Framework, Gameplay, Network, UI, Audio

## 🎯 设计原则

### 1. 分层职责
- **工具层**: 提供通用能力（如何执行）
- **业务层**: 实现具体逻辑（执行什么）

### 2. 依赖注入
- 所有服务通过 VContainer 注册和解析
- 避免单例模式，使用 `Lifetime.Singleton`

### 3. 异步优先
- 使用 UniTask 替代 Coroutine
- 所有耗时操作都异步化

### 4. 事件驱动
- UI 通过 EventBus 订阅业务事件
- 避免 UI 直接调用业务逻辑

### 5. 资源管理
- UniRx 订阅使用 `CompositeDisposable` 管理
- DOTween 动画使用 Tweener 引用管理
- 提供 `OnDestroy()` 清理代码

## 📂 目录结构

```
Assets/Scripts/Runtime/
├── Core/
│   └── Bootstrapper.cs          # VContainer 配置入口
├── Services/
│   ├── Startup/
│   │   ├── GameStartupController.cs   # 启动流程案例
│   │   └── StartupProgressUI.cs       # UI 事件驱动案例
│   └── ...
└── ...
```

## 💡 AI 使用提示

当需要创建新功能时：
1. **先查找类似示例** - 参考 `GameStartupController.cs`
2. **确定分层** - 是工具层还是业务层？
3. **选择工具** - TaskScheduler、EventBus、UniTask 等
4. **遵循规范** - 命名、异步、清理
5. **参考文档** - 查看 `README_TaskScheduler案例.md`
