using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AFramework.Templates
{
    /// <summary>
    /// TaskScheduler 使用模板
    /// 参考文件: Assets/Scripts/Runtime/Services/Startup/GameStartupController.cs
    /// 
    /// 功能：
    /// - 顺序执行任务（RunSequential）
    /// - 并行执行任务（Run）
    /// - 取消令牌管理（CancellationTokenSource）
    /// - 事件发布（EventBus）
    /// </summary>
    public class TaskSchedulerTemplate : MonoBehaviour
    {
        #region 依赖注入

        private ITaskScheduler _taskScheduler;
        private IEventBus _eventBus;

        void Start()
        {
            // 通过 VContainer 解析服务
            _taskScheduler = Bootstrapper.Resolve<ITaskScheduler>();
            _eventBus = Bootstrapper.Resolve<IEventBus>();

            // 启动异步流程
            ExecuteTaskFlowAsync().Forget();
        }

        #endregion

        #region 取消令牌管理

        private CancellationTokenSource _flowCts;

        /// <summary>
        /// 取消任务流程
        /// </summary>
        public void CancelFlow()
        {
            _flowCts?.Cancel();
            _flowCts?.Dispose();
            _flowCts = null;
        }

        #endregion

        #region 主流程

        private async UniTaskVoid ExecuteTaskFlowAsync()
        {
            _flowCts = new CancellationTokenSource();

            try
            {
                // 阶段1: 顺序执行任务
                await ExecuteSequentialPhase();

                // 阶段2: 并行执行任务
                await ExecuteParallelPhase();

                // 阶段3: 完成
                OnFlowComplete();
            }
            catch (OperationCanceledException)
            {
                LogManager.Log("任务流程被取消", LogCategory.Framework);
            }
            catch (Exception ex)
            {
                LogManager.LogError($"任务流程失败: {ex.Message}", LogCategory.Framework);
            }
        }

        #endregion

        #region 阶段1: 顺序执行

        /// <summary>
        /// 顺序执行任务（串行）
        /// 适用场景：有依赖关系的任务，如 初始化 → 配置 → 连接
        /// </summary>
        private async UniTask ExecuteSequentialPhase()
        {
            PublishProgress("顺序执行阶段", 0f);

            // 使用 RunSequential 顺序执行
            await _taskScheduler.RunSequential(
                Task1_Initialize,
                Task2_LoadConfig,
                Task3_Connect
            );

            PublishProgress("顺序执行完成", 0.33f);
        }

        private async UniTask Task1_Initialize()
        {
            LogManager.Log("执行任务1: 初始化", LogCategory.Framework);
            await UniTask.Delay(1000, cancellationToken: _flowCts.Token);
        }

        private async UniTask Task2_LoadConfig()
        {
            LogManager.Log("执行任务2: 加载配置", LogCategory.Framework);
            await UniTask.Delay(1000, cancellationToken: _flowCts.Token);
        }

        private async UniTask Task3_Connect()
        {
            LogManager.Log("执行任务3: 连接服务器", LogCategory.Framework);
            await UniTask.Delay(1000, cancellationToken: _flowCts.Token);
        }

        #endregion

        #region 阶段2: 并行执行

        /// <summary>
        /// 并行执行任务
        /// 适用场景：无依赖关系的任务，如 加载UI + 加载音频 + 加载角色
        /// </summary>
        private async UniTask ExecuteParallelPhase()
        {
            PublishProgress("并行执行阶段", 0.33f);

            // 使用 Run 并行执行（自动用 UniTask.WhenAll）
            await _taskScheduler.Run(
                Task4_LoadUI,
                Task5_LoadAudio,
                Task6_LoadCharacter
            );

            PublishProgress("并行执行完成", 0.66f);
        }

        private async UniTask Task4_LoadUI()
        {
            LogManager.Log("并行任务1: 加载UI", LogCategory.UI);
            await UniTask.Delay(2000, cancellationToken: _flowCts.Token);
        }

        private async UniTask Task5_LoadAudio()
        {
            LogManager.Log("并行任务2: 加载音频", LogCategory.Audio);
            await UniTask.Delay(1500, cancellationToken: _flowCts.Token);
        }

        private async UniTask Task6_LoadCharacter()
        {
            LogManager.Log("并行任务3: 加载角色", LogCategory.Gameplay);
            await UniTask.Delay(1800, cancellationToken: _flowCts.Token);
        }

        #endregion

        #region 事件发布

        /// <summary>
        /// 发布进度事件（供 UI 订阅）
        /// </summary>
        private void PublishProgress(string phase, float progress)
        {
            _eventBus.Publish(new TaskProgressEvent
            {
                CurrentPhase = phase,
                Progress = progress,
                Timestamp = DateTime.Now
            });
        }

        private void OnFlowComplete()
        {
            PublishProgress("流程完成", 1f);
            LogManager.Log("所有任务执行完成", LogCategory.Framework);
        }

        #endregion

        #region 生命周期

        void OnDestroy()
        {
            // 清理取消令牌
            CancelFlow();
        }

        #endregion
    }

    #region 事件定义

    /// <summary>
    /// 任务进度事件
    /// </summary>
    public class TaskProgressEvent
    {
        public string CurrentPhase { get; set; }
        public float Progress { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}
