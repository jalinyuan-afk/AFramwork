# AFramework API 参考手册 (AI Skill)

> **目标**: 快速查找 API 调用方式

## 🔌 核心 API

### ITaskScheduler

**位置**: `Assets/Scripts/Runtime/Core/Interfaces/ITaskScheduler.cs`  
**解析**: `Bootstrapper.Resolve<ITaskScheduler>()`

#### 方法列表

```csharp
// 顺序执行任务（串行）
UniTask RunSequential(params Func<UniTask>[] tasks);

// 并行执行任务
UniTask Run(params Func<UniTask>[] tasks);

// 带 ID 执行任务（已废弃，使用 CancellationTokenSource 代替）
// UniTask<int> RunWithId(Func<UniTask> task);

// 取消任务（传入 CancellationTokenSource）
void CancelTask(CancellationTokenSource cts);
```

#### 使用示例

```csharp
// 1. 顺序执行
await _taskScheduler.RunSequential(
    async () => await Task1(),
    async () => await Task2(),
    async () => await Task3()
);

// 2. 并行执行
await _taskScheduler.Run(
    async () => await LoadUI(),
    async () => await LoadAudio(),
    async () => await LoadCharacter()
);

// 3. 取消任务（推荐方式）
var cts = new CancellationTokenSource();
var task = DoWorkAsync(cts.Token);
_taskScheduler.CancelTask(cts);
```

---

### IEventBus

**位置**: `Assets/Scripts/Runtime/Core/Interfaces/IEventBus.cs`  
**解析**: `Bootstrapper.Resolve<IEventBus>()`

#### 方法列表

```csharp
// 发布事件
void Publish<T>(T eventData) where T : class;

// 订阅事件（返回 IDisposable）
IDisposable Subscribe<T>(Action<T> handler) where T : class;
```

#### 使用示例

```csharp
// 1. 定义事件类
public class GameStartupProgressEvent
{
    public string CurrentPhase { get; set; }
    public float Progress { get; set; }
    public string Message { get; set; }
}

// 2. 发布事件
_eventBus.Publish(new GameStartupProgressEvent
{
    CurrentPhase = "初始化",
    Progress = 0.25f,
    Message = "正在初始化SDK"
});

// 3. 订阅事件
_eventBus.Subscribe<GameStartupProgressEvent>(evt => 
{
    Debug.Log($"{evt.CurrentPhase}: {evt.Progress}");
}).AddTo(_disposables);
```

---

### LogManager

**位置**: `Assets/Scripts/Runtime/Core/LogManager.cs`  
**静态类**: 无需解析

#### 方法列表

```csharp
// 记录日志
static void Log(string message, LogCategory category = LogCategory.Framework);

// 记录警告
static void LogWarning(string message, LogCategory category = LogCategory.Framework);

// 记录错误
static void LogError(string message, LogCategory category = LogCategory.Framework);
```

#### LogCategory 枚举

```csharp
public enum LogCategory
{
    Framework,   // 框架层
    Gameplay,    // 游戏逻辑
    Network,     // 网络通信
    UI,          // 界面系统
    Audio        // 音频系统
}
```

#### 使用示例

```csharp
LogManager.Log("任务调度开始", LogCategory.Framework);
LogManager.LogWarning("资源加载超时", LogCategory.UI);
LogManager.LogError("网络连接失败", LogCategory.Network);
```

---

### Bootstrapper (VContainer)

**位置**: `Assets/Scripts/Runtime/Core/Bootstrapper.cs`

#### 方法列表

```csharp
// 解析服务
public static T Resolve<T>();

// 尝试解析服务
public static bool TryResolve<T>(out T service);
```

#### 使用示例

```csharp
// 1. 解析服务
var taskScheduler = Bootstrapper.Resolve<ITaskScheduler>();
var eventBus = Bootstrapper.Resolve<IEventBus>();

// 2. 安全解析
if (Bootstrapper.TryResolve<ICustomService>(out var service))
{
    service.DoSomething();
}
```

---

## 🎨 UniTask API

**命名空间**: `Cysharp.Threading.Tasks`

### 常用方法

```csharp
// 延迟执行
await UniTask.Delay(1000); // 毫秒
await UniTask.Delay(TimeSpan.FromSeconds(2));

// 等待下一帧
await UniTask.Yield();
await UniTask.NextFrame();

// 场景加载
await SceneManager.LoadSceneAsync("Main").ToUniTask();

// 资源加载
var op = Resources.LoadAsync<GameObject>("Prefab");
await op.ToUniTask();

// 并行等待
await UniTask.WhenAll(task1, task2, task3);

// 任意完成
await UniTask.WhenAny(task1, task2, task3);

// 忘记等待（不阻塞）
DoWorkAsync().Forget();
```

---

## 🔄 UniRx API

**命名空间**: `UniRx`

### Observable 常用方法

```csharp
// 定时器（延迟执行）
Observable.Timer(TimeSpan.FromSeconds(2))
    .Subscribe(_ => DoSomething())
    .AddTo(_disposables);

// 间隔执行
Observable.Interval(TimeSpan.FromSeconds(1))
    .Subscribe(_ => UpdateEverySecond())
    .AddTo(_disposables);

// 每帧更新
Observable.EveryUpdate()
    .Subscribe(_ => UpdateLogic())
    .AddTo(_disposables);

// 条件过滤
this.UpdateAsObservable()
    .Where(_ => Input.GetKeyDown(KeyCode.Space))
    .Subscribe(_ => Jump())
    .AddTo(_disposables);

// 延迟执行
Observable.ReturnUnit()
    .Delay(TimeSpan.FromSeconds(1))
    .Subscribe(_ => DelayedAction())
    .AddTo(_disposables);
```

### CompositeDisposable

```csharp
private CompositeDisposable _disposables = new CompositeDisposable();

void Start()
{
    // 所有订阅添加到 _disposables
    Observable.EveryUpdate()
        .Subscribe(_ => {})
        .AddTo(_disposables);
}

void OnDestroy()
{
    // 一次性清理所有订阅
    _disposables?.Dispose();
}
```

---

## 🎬 DOTween API

**命名空间**: `DG.Tweening`

### 常用补间

```csharp
// Transform 移动
transform.DOMove(new Vector3(0, 5, 0), 1f);

// UI Slider 值变化
slider.DOValue(1f, 0.5f);

// CanvasGroup 透明度
canvasGroup.DOFade(0f, 0.3f);

// Image 颜色
image.DOColor(Color.red, 1f);

// Text 内容
text.DOText("Hello World", 2f);

// 链式调用
transform.DOMove(targetPos, 1f)
    .SetEase(Ease.OutCubic)
    .SetDelay(0.5f)
    .OnComplete(() => Debug.Log("完成"));
```

### Tweener 管理（重要！）

```csharp
private Tweener _tweener;

void UpdateValue(float target)
{
    // 必须先 Kill，避免冲突
    _tweener?.Kill();
    
    _tweener = slider.DOValue(target, 0.3f);
}

void OnDestroy()
{
    _tweener?.Kill();
}
```

### 转换为 UniTask

```csharp
await transform.DOMove(targetPos, 1f).ToUniTask();
await canvasGroup.DOFade(0f, 0.5f).ToUniTask();
```

---

## 🛡️ CancellationTokenSource

**命名空间**: `System.Threading`

### 基础用法

```csharp
private CancellationTokenSource _cts;

async UniTask DoWork()
{
    _cts = new CancellationTokenSource();
    
    try
    {
        await LongTask(_cts.Token);
    }
    catch (OperationCanceledException)
    {
        Debug.Log("任务取消");
    }
}

void Cancel()
{
    _cts?.Cancel();
    _cts?.Dispose();
}
```

### 超时取消

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await DoWorkAsync(cts.Token);
```

### 传递令牌

```csharp
async UniTask LongTask(CancellationToken ct)
{
    for (int i = 0; i < 100; i++)
    {
        ct.ThrowIfCancellationRequested();
        await UniTask.Delay(100, cancellationToken: ct);
    }
}
```

---

## 📋 快速查找表

| 需求 | API | 示例文件 |
|------|-----|----------|
| 顺序执行任务 | `ITaskScheduler.RunSequential()` | GameStartupController.cs |
| 并行执行任务 | `ITaskScheduler.Run()` | GameStartupController.cs |
| 发布事件 | `IEventBus.Publish()` | GameStartupController.cs |
| 订阅事件 | `IEventBus.Subscribe()` | StartupProgressUI.cs |
| 记录日志 | `LogManager.Log()` | 全局可用 |
| 延迟执行 | `UniTask.Delay()` | GameStartupController.cs |
| 平滑动画 | `DOTween.To()` | StartupProgressUI.cs |
| 定时器 | `Observable.Timer()` | StartupProgressUI.cs |
| 取消任务 | `CancellationTokenSource.Cancel()` | GameStartupController.cs |

## 💡 AI 使用提示

1. **复制完整代码** - 包括 using 命名空间
2. **检查返回值** - UniTask vs void vs UniTaskVoid
3. **管理生命周期** - 订阅用 AddTo()，动画用 Kill()
4. **异常处理** - 捕获 OperationCanceledException
