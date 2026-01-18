# GitHub Copilot Instructions for AFramework

## 项目概述

AFramework 是一个基于 Unity 的游戏开发框架，整合了现代化的依赖注入、异步编程和响应式编程模式。

## 核心技术栈

### 1. **依赖注入 (VContainer)**
- **用途**: 管理所有服务的生命周期和依赖关系
- **使用方式**: 
  ```csharp
  // 获取服务实例
  var taskScheduler = Bootstrapper.Resolve<ITaskScheduler>();
  var eventBus = Bootstrapper.Resolve<IEventBus>();
  ```
- **注册位置**: `Assets/Scripts/Runtime/Core/Bootstrapper.cs`

### 2. **异步编程 (UniTask)**
- **命名空间**: `Cysharp.Threading.Tasks`
- **使用场景**: 所有异步操作（加载资源、网络请求、场景切换）
- **示例**:
  ```csharp
  public async UniTask LoadAssetAsync()
  {
      await UniTask.Delay(1000);
      // 异步操作
  }
  ```

### 3. **响应式编程 (UniRx)**
- **命名空间**: `UniRx`
- **使用场景**: 事件流、定时器、可取消的订阅
- **示例**:
  ```csharp
  Observable.EveryUpdate()
      .Subscribe(_ => UpdateLogic())
      .AddTo(_disposables);
  ```

### 4. **任务调度 (TaskScheduler)**
- **接口**: `ITaskScheduler`
- **核心方法**:
  - `RunSequential()` - 顺序执行任务
  - `Run()` - 并行执行任务
  - `CancelTask()` - 取消任务
- **示例位置**: `Assets/Scripts/Runtime/Services/Startup/GameStartupController.cs`

### 5. **事件总线 (EventBus)**
- **接口**: `IEventBus`
- **核心方法**:
  - `Publish<T>()` - 发布事件
  - `Subscribe<T>()` - 订阅事件
- **使用示例**:
  ```csharp
  _eventBus.Publish(new GameStartupProgressEvent
  {
      CurrentPhase = "初始化",
      Progress = 0.25f
  });
  ```

### 6. **日志系统 (LogManager)**
- **用法**: `LogManager.Log("消息", LogCategory.Framework);`
- **日志级别**: Debug, Info, Warning, Error

## 架构模式

### 服务层设计
- **位置**: `Assets/Scripts/Runtime/Services/`
- **命名规范**: `I{ServiceName}` (接口) + `{ServiceName}` (实现)
- **生命周期**: 通过 VContainer 管理

### 启动流程示例
参考 `GameStartupController.cs`:
1. **初始化阶段** - 顺序执行（版本检查 → SDK 初始化 → 配置加载）
2. **加载阶段** - 并行执行（UI + 音频 + 角色资源）
3. **准备阶段** - 系统初始化
4. **进入主菜单**

### UI 事件驱动模式
参考 `StartupProgressUI.cs`:
- 通过 EventBus 订阅事件更新 UI
- 使用 UniRx 的 `CompositeDisposable` 管理订阅生命周期
- DOTween 实现流畅动画

## 代码约定

### 命名规范
- **接口**: `I` 前缀 (如 `ITaskScheduler`)
- **私有字段**: `_` 前缀 (如 `_taskScheduler`)
- **异步方法**: `Async` 后缀 (如 `LoadAssetAsync`)
- **事件类**: `Event` 后缀 (如 `GameStartupProgressEvent`)

### 任务命名
- **格式**: `动词_对象_修饰符`
- **示例**: 
  - `LoadScene_Gameplay`
  - `FetchData_UserProfile`
  - `InitializeSDK_ThirdParty`

### 取消令牌管理
- 使用 `CancellationTokenSource` 而非 Task ID
- 示例:
  ```csharp
  private CancellationTokenSource _startupCts;
  
  public async UniTask StartAsync()
  {
      _startupCts = new CancellationTokenSource();
      await DoWorkAsync(_startupCts.Token);
  }
  
  public void Cancel()
  {
      _startupCts?.Cancel();
  }
  ```

### DOTween 动画管理
- 保存 Tweener 引用避免冲突
- 创建新动画前调用 `Kill()`
- 示例:
  ```csharp
  private Tweener _progressTweener;
  
  void UpdateProgress(float value)
  {
      _progressTweener?.Kill();
      _progressTweener = DOTween.To(
          () => progressBar.value,
          x => progressBar.value = x,
          value,
          0.3f
      );
  }
  ```

## 常见模式

### 1. 顺序任务链
```csharp
await _taskScheduler.RunSequential(
    () => CheckVersion(),
    () => InitializeSDK(),
    () => LoadConfig()
);
```

### 2. 并行任务
```csharp
await _taskScheduler.Run(
    () => LoadUIAssets(),
    () => LoadAudioAssets(),
    () => LoadCharacterAssets()
);
```

### 3. 事件驱动更新
```csharp
_eventBus.Subscribe<GameStartupProgressEvent>(OnProgressUpdate)
    .AddTo(_disposables);
```

### 4. 资源清理
```csharp
private CompositeDisposable _disposables = new CompositeDisposable();

void OnDestroy()
{
    _disposables?.Dispose();
    _progressTweener?.Kill();
}
```

## 重要参考文件

| 文件 | 用途 |
|------|------|
| `Assets/Scripts/Runtime/Core/Bootstrapper.cs` | 依赖注入配置 |
| `Assets/Scripts/Runtime/Services/Startup/GameStartupController.cs` | TaskScheduler 完整案例 |
| `Assets/Scripts/Runtime/Services/Startup/StartupProgressUI.cs` | EventBus + DOTween 案例 |
| `README_TaskScheduler案例.md` | TaskScheduler 详细文档 |

## AI 编程指导原则

1. **优先查找示例代码** - 在 `GameStartupController.cs` 中查找类似模式
2. **遵循架构分层** - 工具层 (TaskScheduler) vs 业务层 (ProcedureManager)
3. **使用 UniTask 而非 Coroutine** - 所有异步操作
4. **通过 EventBus 解耦** - UI 不直接依赖业务逻辑
5. **管理生命周期** - CancellationTokenSource + CompositeDisposable

## 快速检查清单

生成代码前检查：
- [ ] 是否使用 VContainer 解析依赖？
- [ ] 异步方法是否使用 UniTask？
- [ ] 是否通过 EventBus 发布/订阅事件？
- [ ] 是否正确管理 Tweener 生命周期？
- [ ] 是否添加资源清理代码？
- [ ] 命名是否符合项目规范？
