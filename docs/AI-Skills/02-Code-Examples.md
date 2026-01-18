# AFramework 代码示例库 (AI Skill)

> **目标**: 提供可直接参考的代码模板

## 📍 示例代码位置索引

| 功能 | 文件路径 | 关键代码行 |
|------|----------|------------|
| TaskScheduler 完整案例 | `Assets/Scripts/Runtime/Services/Startup/GameStartupController.cs` | 全文 |
| EventBus + UI 驱动 | `Assets/Scripts/Runtime/Services/Startup/StartupProgressUI.cs` | 全文 |
| VContainer 配置 | `Assets/Scripts/Runtime/Core/Bootstrapper.cs` | RegisterServices() |
| DOTween 平滑动画 | `StartupProgressUI.cs` | OnProgressUpdate() |
| 取消令牌管理 | `GameStartupController.cs` | CancelStartup() |

## 1️⃣ TaskScheduler 完整流程

### 场景：游戏启动流程（4阶段）

**文件**: `GameStartupController.cs`

```csharp
public class GameStartupController : MonoBehaviour
{
    private ITaskScheduler _taskScheduler;
    private IEventBus _eventBus;
    private CancellationTokenSource _startupCts;

    void Start()
    {
        _taskScheduler = Bootstrapper.Resolve<ITaskScheduler>();
        _eventBus = Bootstrapper.Resolve<IEventBus>();
        StartGameAsync().Forget();
    }

    private async UniTaskVoid StartGameAsync()
    {
        _startupCts = new CancellationTokenSource();
        
        try
        {
            // 阶段1: 顺序初始化
            await ExecuteInitializationPhase();
            
            // 阶段2: 并行加载资源
            await ExecuteLoadingPhase();
            
            // 阶段3: 顺序准备系统
            await ExecutePreparationPhase();
            
            // 阶段4: 进入主菜单
            await EnterMainMenu();
        }
        catch (OperationCanceledException)
        {
            LogManager.Log("启动流程被取消", LogCategory.Framework);
        }
    }

    // 阶段1: 顺序执行
    private async UniTask ExecuteInitializationPhase()
    {
        PublishProgress("初始化阶段", 0f);
        
        await _taskScheduler.RunSequential(
            CheckVersion,
            InitializeSDK,
            LoadConfig,
            InitializeDatabase
        );
        
        PublishProgress("初始化完成", 0.25f);
    }

    // 阶段2: 并行执行
    private async UniTask ExecuteLoadingPhase()
    {
        PublishProgress("加载资源", 0.25f);
        
        await _taskScheduler.Run(
            LoadUIAssets,
            LoadAudioAssets,
            LoadCharacterAssets
        );
        
        PublishProgress("资源加载完成", 0.75f);
    }

    // 发布进度事件
    private void PublishProgress(string phase, float progress)
    {
        _eventBus.Publish(new GameStartupProgressEvent
        {
            CurrentPhase = phase,
            Progress = progress,
            Message = $"正在执行: {phase}"
        });
    }

    // 取消流程
    public void CancelStartup()
    {
        _startupCts?.Cancel();
        _startupCts?.Dispose();
    }
}
```

**关键点**:
- ✅ 使用 `CancellationTokenSource` 管理取消
- ✅ 顺序任务用 `RunSequential()`
- ✅ 并行任务用 `Run()`
- ✅ 通过 EventBus 发布进度

## 2️⃣ EventBus 驱动 UI

### 场景：进度条 + 加载提示

**文件**: `StartupProgressUI.cs`

```csharp
public class StartupProgressUI : MonoBehaviour
{
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI phaseText;
    [SerializeField] private TextMeshProUGUI tipText;
    
    private IEventBus _eventBus;
    private CompositeDisposable _disposables = new CompositeDisposable();
    private Tweener _progressTweener;

    void Start()
    {
        _eventBus = Bootstrapper.Resolve<IEventBus>();
        
        // 订阅进度事件
        _eventBus.Subscribe<GameStartupProgressEvent>(OnProgressUpdate)
            .AddTo(_disposables);
        
        // 订阅完成事件
        _eventBus.Subscribe<GameStartupCompleteEvent>(OnStartupComplete)
            .AddTo(_disposables);
        
        // 每2秒轮换提示
        Observable.Interval(TimeSpan.FromSeconds(2))
            .Subscribe(_ => RotateTip())
            .AddTo(_disposables);
    }

    private void OnProgressUpdate(GameStartupProgressEvent evt)
    {
        phaseText.text = evt.CurrentPhase;
        
        // 平滑动画
        _progressTweener?.Kill();
        _progressTweener = progressBar.DOValue(evt.Progress, 0.3f)
            .SetEase(Ease.OutCubic);
    }

    private void OnStartupComplete(GameStartupCompleteEvent evt)
    {
        if (evt.Success)
        {
            HideStartupUI();
        }
        else
        {
            tipText.text = $"启动失败: {evt.ErrorMessage}";
        }
    }

    void OnDestroy()
    {
        _disposables?.Dispose();
        _progressTweener?.Kill();
    }
}
```

**关键点**:
- ✅ 使用 `CompositeDisposable` 管理订阅
- ✅ `_progressTweener?.Kill()` 避免动画冲突
- ✅ `OnDestroy()` 清理资源

## 3️⃣ VContainer 服务注册

### 场景：依赖注入配置

**文件**: `Bootstrapper.cs`

```csharp
public class Bootstrapper : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // 注册单例服务
        builder.Register<ITaskScheduler, TaskScheduler>(Lifetime.Singleton);
        builder.Register<IEventBus, EventBus>(Lifetime.Singleton);
        builder.Register<ILogManager, LogManager>(Lifetime.Singleton);
        
        // 注册临时服务
        builder.Register<IAssetLoader, AssetLoader>(Lifetime.Transient);
    }
    
    // 全局解析服务
    public static T Resolve<T>()
    {
        return Parent.Container.Resolve<T>();
    }
}
```

## 4️⃣ DOTween 动画模式

### 模式1: 进度条平滑过渡

```csharp
private Tweener _progressTweener;

void UpdateProgress(float targetValue)
{
    _progressTweener?.Kill();  // 必须！避免冲突
    _progressTweener = DOTween.To(
        () => progressBar.value,
        x => progressBar.value = x,
        targetValue,
        0.3f
    ).SetEase(Ease.OutCubic);
}
```

### 模式2: UI 淡入淡出

```csharp
private Tweener _fadeTweener;

async UniTask FadeOut()
{
    _fadeTweener?.Kill();
    _fadeTweener = canvasGroup.DOFade(0f, 0.5f);
    await _fadeTweener.ToUniTask();
}
```

## 5️⃣ 取消令牌管理

### 模式1: 基础取消

```csharp
private CancellationTokenSource _cts;

async UniTask DoWork()
{
    _cts = new CancellationTokenSource();
    
    try
    {
        await LongRunningTask(_cts.Token);
    }
    catch (OperationCanceledException)
    {
        Debug.Log("任务已取消");
    }
}

void Cancel()
{
    _cts?.Cancel();
    _cts?.Dispose();
}
```

### 模式2: 超时取消

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await DoWorkAsync(cts.Token);
```

## 6️⃣ UniRx 常用模式

### 模式1: 定时器

```csharp
Observable.Timer(TimeSpan.FromSeconds(2))
    .Subscribe(_ => DoSomething())
    .AddTo(_disposables);
```

### 模式2: 每帧更新

```csharp
Observable.EveryUpdate()
    .Subscribe(_ => UpdateLogic())
    .AddTo(_disposables);
```

### 模式3: 条件触发

```csharp
this.UpdateAsObservable()
    .Where(_ => Input.GetKeyDown(KeyCode.Space))
    .Subscribe(_ => Jump())
    .AddTo(_disposables);
```

## 7️⃣ 资源清理模板

```csharp
public class MyComponent : MonoBehaviour
{
    private CompositeDisposable _disposables = new CompositeDisposable();
    private Tweener _tweener;
    private CancellationTokenSource _cts;

    void OnDestroy()
    {
        // 清理订阅
        _disposables?.Dispose();
        
        // 清理动画
        _tweener?.Kill();
        
        // 清理令牌
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
```

## 🔍 快速查找指南

**需要...**
- ✅ 顺序执行任务 → 参考 `GameStartupController.ExecuteInitializationPhase()`
- ✅ 并行执行任务 → 参考 `GameStartupController.ExecuteLoadingPhase()`
- ✅ 事件驱动 UI → 参考 `StartupProgressUI.OnProgressUpdate()`
- ✅ 平滑动画 → 参考 `StartupProgressUI._progressTweener` 模式
- ✅ 取消任务 → 参考 `GameStartupController.CancelStartup()`
- ✅ 定时器 → 参考 `StartupProgressUI.RotateTip()`

## 💡 AI 使用建议

1. **复制模板** - 直接复制对应模式的代码
2. **修改细节** - 改变任务逻辑、事件类型
3. **保持结构** - 不要删除清理代码
4. **检查依赖** - 确认 using 引用完整
