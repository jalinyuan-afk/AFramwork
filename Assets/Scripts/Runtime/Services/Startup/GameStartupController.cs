using UnityEngine;
using TradeGame.Runtime.Framework;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UniRx;
using VContainer;

namespace TradeGame.Runtime.Services.Startup
{
    /// <summary>
    /// 游戏启动控制器 - 任务调度器使用案例
    /// 演示如何使用 TaskScheduler 编排游戏启动流程
    /// </summary>
    public class GameStartupController : MonoBehaviour
    {
        [Header("启动配置")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private float delayStartSeconds = 0.5f;

        [Inject] private ITaskScheduler _taskScheduler;
        [Inject] private IEventBus _eventBus;
        private CompositeDisposable _disposables = new();

        // 用于取消整个启动流程
        private CancellationTokenSource _startupCts;

        private void Start()
        {
            // 从容器获取依赖
            // _taskScheduler = Bootstrapper.Resolve<ITaskScheduler>();
            // _eventBus = Bootstrapper.Resolve<IEventBus>();

            LogManager.Info(LogCategory.System, "[GameStartup] 游戏启动控制器初始化完成");

            if (autoStart)
            {
                // 延迟启动，给 Unity 引擎初始化时间
                _taskScheduler.RunDelayed(TimeSpan.FromSeconds(delayStartSeconds), async ct =>
                {
                    await StartGameAsync(ct);
                }, "DelayExecute_GameStart");
            }

            // 监听启动进度事件
            _eventBus.Subscribe<GameStartupProgressEvent>(OnStartupProgress).AddTo(_disposables);
            _eventBus.Subscribe<GameStartupCompleteEvent>(OnStartupComplete).AddTo(_disposables);
        }

        /// <summary>
        /// 手动启动游戏（用于调试）
        /// </summary>
        [ContextMenu("Start Game")]
        public async void StartGameManually()
        {
            await StartGameAsync(CancellationToken.None);
        }

        /// <summary>
        /// 游戏启动主流程
        /// </summary>
        private async UniTask StartGameAsync(CancellationToken ct)
        {
            _startupCts = new CancellationTokenSource();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _startupCts.Token);

            LogManager.Info(LogCategory.System, "[GameStartup] ========== 游戏启动流程开始 ==========");

            try
            {
                // 阶段 1：顺序执行初始化任务（必须按顺序）
                await ExecuteInitializationPhase(linkedCts.Token);

                // 阶段 2：并行加载资源（可以同时进行）
                await ExecuteLoadingPhase(linkedCts.Token);

                // 阶段 3：系统准备（顺序执行）
                await ExecutePreparationPhase(linkedCts.Token);

                // 阶段 4：进入主菜单
                await EnterMainMenu(linkedCts.Token);

                // 发布启动完成事件
                _eventBus.Publish(new GameStartupCompleteEvent(true));
                LogManager.Info(LogCategory.System, "[GameStartup] ========== 游戏启动流程完成 ==========");
            }
            catch (OperationCanceledException)
            {
                LogManager.Warning(LogCategory.System, "[GameStartup] 游戏启动流程被取消");
                _eventBus.Publish(new GameStartupCompleteEvent(false));
            }
            catch (Exception ex)
            {
                LogManager.Error(LogCategory.System, $"[GameStartup] 游戏启动失败: {ex.Message}");
                _eventBus.Publish(new GameStartupCompleteEvent(false));
            }
            finally
            {
                linkedCts?.Dispose();
                _startupCts?.Dispose();
                _startupCts = null;
            }
        }

        /// <summary>
        /// 阶段 1：初始化阶段（顺序执行）
        /// </summary>
        private async UniTask ExecuteInitializationPhase(CancellationToken ct)
        {
            LogManager.Info(LogCategory.System, "[GameStartup] >>> 阶段 1：初始化阶段");

            await _taskScheduler.RunSequential(new Func<CancellationToken, UniTask>[]
            {
                async ct => await CheckVersion(ct),
                async ct => await InitializeSDK(ct),
                async ct => await LoadGameConfig(ct),
                async ct => await InitializeDatabase(ct)
            }, "InitializationPhase");

            PublishProgress("初始化完成", 0.25f);
        }

        /// <summary>
        /// 阶段 2：资源加载阶段（并行执行）
        /// </summary>
        private async UniTask ExecuteLoadingPhase(CancellationToken ct)
        {
            LogManager.Info(LogCategory.System, "[GameStartup] >>> 阶段 2：资源加载阶段");

            // 并行加载多个资源
            var tasks = new[]
            {
                _taskScheduler.Run(async ct => await LoadUIAssets(ct), "LoadAssets_UI", ct),
                _taskScheduler.Run(async ct => await LoadAudioAssets(ct), "LoadAssets_Audio", ct),
                _taskScheduler.Run(async ct => await LoadCharacterAssets(ct), "LoadAssets_Character", ct)
            };

            // 等待所有并行任务完成
            await UniTask.WhenAll(tasks);

            PublishProgress("资源加载完成", 0.6f);
        }

        /// <summary>
        /// 阶段 3：系统准备阶段（顺序执行）
        /// </summary>
        private async UniTask ExecutePreparationPhase(CancellationToken ct)
        {
            LogManager.Info(LogCategory.System, "[GameStartup] >>> 阶段 3：系统准备阶段");

            await _taskScheduler.RunSequential(new Func<CancellationToken, UniTask>[]
            {
                async ct => await InitializeAudioSystem(ct),
                async ct => await InitializeUISystem(ct),
                async ct => await CheckCloudSave(ct),
                async ct => await PreloadCriticalData(ct)
            }, "PreparationPhase");

            PublishProgress("系统准备完成", 0.9f);
        }

        /// <summary>
        /// 阶段 4：进入主菜单
        /// </summary>
        private async UniTask EnterMainMenu(CancellationToken ct)
        {
            LogManager.Info(LogCategory.System, "[GameStartup] >>> 阶段 4：进入主菜单");

            await _taskScheduler.Run(async ct =>
            {
                // 加载主菜单场景
                await LoadMainMenuScene(ct);
                // 播放背景音乐
                await PlayMenuBGM(ct);
            }, "EnterMainMenu", ct);

            PublishProgress("启动完成", 1.0f);
        }

        #region 具体任务实现（模拟）

        private async UniTask CheckVersion(CancellationToken ct)
        {
            LogManager.Info(LogCategory.System, "[Task] 检查版本...");
            await UniTask.Delay(500, cancellationToken: ct);
            LogManager.Info(LogCategory.System, "[Task] ✓ 版本检查完成");
        }

        private async UniTask InitializeSDK(CancellationToken ct)
        {
            LogManager.Info(LogCategory.System, "[Task] 初始化 SDK...");
            await UniTask.Delay(800, cancellationToken: ct);

            // 模拟 SDK 可能失败的情况
            if (UnityEngine.Random.value < 0.1f) // 10% 失败率
            {
                throw new Exception("SDK 初始化失败");
            }

            LogManager.Info(LogCategory.System, "[Task] ✓ SDK 初始化完成");
        }

        private async UniTask LoadGameConfig(CancellationToken ct)
        {
            LogManager.Info(LogCategory.System, "[Task] 加载游戏配置...");
            await UniTask.Delay(600, cancellationToken: ct);
            LogManager.Info(LogCategory.System, "[Task] ✓ 游戏配置加载完成");
        }

        private async UniTask InitializeDatabase(CancellationToken ct)
        {
            LogManager.Info(LogCategory.System, "[Task] 初始化数据库...");
            await UniTask.Delay(400, cancellationToken: ct);
            LogManager.Info(LogCategory.System, "[Task] ✓ 数据库初始化完成");
        }

        private async UniTask LoadUIAssets(CancellationToken ct)
        {
            LogManager.Info(LogCategory.System, "[Task] 加载 UI 资源...");
            await UniTask.Delay(1200, cancellationToken: ct);
            LogManager.Info(LogCategory.System, "[Task] ✓ UI 资源加载完成");
        }

        private async UniTask LoadAudioAssets(CancellationToken ct)
        {
            LogManager.Info(LogCategory.System, "[Task] 加载音频资源...");
            await UniTask.Delay(1000, cancellationToken: ct);
            LogManager.Info(LogCategory.System, "[Task] ✓ 音频资源加载完成");
        }

        private async UniTask LoadCharacterAssets(CancellationToken ct)
        {
            LogManager.Info(LogCategory.System, "[Task] 加载角色资源...");
            await UniTask.Delay(1500, cancellationToken: ct);
            LogManager.Info(LogCategory.System, "[Task] ✓ 角色资源加载完成");
        }

        private async UniTask InitializeAudioSystem(CancellationToken ct)
        {
            LogManager.Info(LogCategory.System, "[Task] 初始化音频系统...");
            await UniTask.Delay(300, cancellationToken: ct);
            LogManager.Info(LogCategory.System, "[Task] ✓ 音频系统初始化完成");
        }

        private async UniTask InitializeUISystem(CancellationToken ct)
        {
            LogManager.Info(LogCategory.System, "[Task] 初始化 UI 系统...");
            await UniTask.Delay(400, cancellationToken: ct);
            LogManager.Info(LogCategory.System, "[Task] ✓ UI 系统初始化完成");
        }

        private async UniTask CheckCloudSave(CancellationToken ct)
        {
            LogManager.Info(LogCategory.System, "[Task] 检查云存档...");
            await UniTask.Delay(700, cancellationToken: ct);
            LogManager.Info(LogCategory.System, "[Task] ✓ 云存档检查完成");
        }

        private async UniTask PreloadCriticalData(CancellationToken ct)
        {
            LogManager.Info(LogCategory.System, "[Task] 预加载关键数据...");
            await UniTask.Delay(500, cancellationToken: ct);
            LogManager.Info(LogCategory.System, "[Task] ✓ 关键数据预加载完成");
        }

        private async UniTask LoadMainMenuScene(CancellationToken ct)
        {
            LogManager.Info(LogCategory.System, "[Task] 加载主菜单场景...");
            await UniTask.Delay(800, cancellationToken: ct);
            LogManager.Info(LogCategory.System, "[Task] ✓ 主菜单场景加载完成");
        }

        private async UniTask PlayMenuBGM(CancellationToken ct)
        {
            LogManager.Info(LogCategory.System, "[Task] 播放主菜单音乐...");
            await UniTask.Delay(200, cancellationToken: ct);
            LogManager.Info(LogCategory.System, "[Task] ✓ 主菜单音乐播放完成");
        }

        #endregion

        #region 辅助方法

        private void PublishProgress(string message, float progress)
        {
            _eventBus.Publish(new GameStartupProgressEvent(message, progress));
            LogManager.Info(LogCategory.System, $"[GameStartup] 进度：{message} ({progress:P0})");
        }

        private void OnStartupProgress(GameStartupProgressEvent e)
        {
            // UI 可以订阅这个事件来更新进度条
            Debug.Log($"[Progress] {e.Message}: {e.Progress:P0}");
        }

        private void OnStartupComplete(GameStartupCompleteEvent e)
        {
            if (e.Success)
            {
                Debug.Log("<color=green>[Complete] 游戏启动成功！</color>");
            }
            else
            {
                Debug.Log("<color=red>[Complete] 游戏启动失败！</color>");
            }
        }

        /// <summary>
        /// 取消所有启动任务（用于调试）
        /// </summary>
        [ContextMenu("Cancel Startup")]
        public void CancelStartup()
        {
            _startupCts?.Cancel();
            LogManager.Warning(LogCategory.System, "[GameStartup] 启动流程已手动取消");
        }

        #endregion

        private void OnDestroy()
        {
            _disposables?.Dispose();
            LogManager.Info(LogCategory.System, "[GameStartup] 游戏启动控制器已销毁");
        }
    }

    #region 事件定义

    /// <summary>
    /// 游戏启动进度事件
    /// </summary>
    public class GameStartupProgressEvent
    {
        public string Message { get; }
        public float Progress { get; } // 0.0 ~ 1.0

        public GameStartupProgressEvent(string message, float progress)
        {
            Message = message;
            Progress = progress;
        }
    }

    /// <summary>
    /// 游戏启动完成事件
    /// </summary>
    public class GameStartupCompleteEvent
    {
        public bool Success { get; }

        public GameStartupCompleteEvent(bool success)
        {
            Success = success;
        }
    }

    #endregion
}
