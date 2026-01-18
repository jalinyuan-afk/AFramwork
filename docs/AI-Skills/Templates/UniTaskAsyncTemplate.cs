using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AFramework.Templates
{
    /// <summary>
    /// UniTask 异步编程模板
    /// 
    /// 功能：
    /// - 异步方法定义（UniTask、UniTaskVoid）
    /// - 延迟执行（Delay）
    /// - 场景加载（LoadSceneAsync）
    /// - 资源加载（Resources.LoadAsync）
    /// - 取消令牌（CancellationToken）
    /// - 并行等待（WhenAll、WhenAny）
    /// </summary>
    public class UniTaskAsyncTemplate : MonoBehaviour
    {
        #region 基础异步方法

        /// <summary>
        /// 返回 UniTask 的异步方法（需要 await）
        /// 使用场景：需要等待结果的异步操作
        /// </summary>
        public async UniTask<int> LoadDataAsync()
        {
            LogManager.Log("开始加载数据", LogCategory.Framework);

            // 模拟耗时操作
            await UniTask.Delay(1000);

            return 100; // 返回结果
        }

        /// <summary>
        /// 返回 UniTaskVoid 的异步方法（Fire-and-Forget）
        /// 使用场景：不需要等待结果的异步操作
        /// </summary>
        public async UniTaskVoid InitializeAsync()
        {
            LogManager.Log("开始初始化", LogCategory.Framework);

            await UniTask.Delay(500);

            LogManager.Log("初始化完成", LogCategory.Framework);
        }

        #endregion

        #region 延迟执行

        /// <summary>
        /// 延迟执行（毫秒）
        /// </summary>
        public async UniTask DelayInMilliseconds()
        {
            await UniTask.Delay(1000); // 1秒
        }

        /// <summary>
        /// 延迟执行（TimeSpan）
        /// </summary>
        public async UniTask DelayInTimeSpan()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(2));
        }

        /// <summary>
        /// 等待下一帧
        /// </summary>
        public async UniTask WaitNextFrame()
        {
            await UniTask.Yield();
            // 或者
            await UniTask.NextFrame();
        }

        #endregion

        #region 场景加载

        /// <summary>
        /// 加载场景（异步）
        /// </summary>
        public async UniTask LoadSceneExample()
        {
            LogManager.Log("开始加载场景", LogCategory.Framework);

            // 方式1: 直接转换
            await SceneManager.LoadSceneAsync("MainScene").ToUniTask();

            // 方式2: 带进度回调
            var operation = SceneManager.LoadSceneAsync("MainScene");
            while (!operation.isDone)
            {
                float progress = operation.progress;
                LogManager.Log($"加载进度: {progress:P0}", LogCategory.Framework);
                await UniTask.Yield();
            }

            LogManager.Log("场景加载完成", LogCategory.Framework);
        }

        #endregion

        #region 资源加载

        /// <summary>
        /// 加载资源（异步）
        /// </summary>
        public async UniTask<GameObject> LoadPrefabAsync(string path)
        {
            LogManager.Log($"开始加载预制体: {path}", LogCategory.Framework);

            var request = Resources.LoadAsync<GameObject>(path);
            await request.ToUniTask();

            return request.asset as GameObject;
        }

        #endregion

        #region 取消令牌

        private CancellationTokenSource _cts;

        /// <summary>
        /// 支持取消的异步方法
        /// </summary>
        public async UniTask LongRunningTaskAsync(CancellationToken ct)
        {
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    // 检查取消
                    ct.ThrowIfCancellationRequested();

                    LogManager.Log($"进度: {i}/100", LogCategory.Framework);

                    // 延迟时传递 token
                    await UniTask.Delay(100, cancellationToken: ct);
                }
            }
            catch (OperationCanceledException)
            {
                LogManager.Log("任务被取消", LogCategory.Framework);
                throw; // 重新抛出异常
            }
        }

        /// <summary>
        /// 启动可取消任务
        /// </summary>
        public void StartCancellableTask()
        {
            _cts = new CancellationTokenSource();
            LongRunningTaskAsync(_cts.Token).Forget();
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        public void CancelTask()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        /// <summary>
        /// 超时取消
        /// </summary>
        public async UniTask TaskWithTimeout()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            try
            {
                await LongRunningTaskAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                LogManager.Log("任务超时", LogCategory.Framework);
            }
        }

        #endregion

        #region 并行等待

        /// <summary>
        /// 等待所有任务完成（WhenAll）
        /// </summary>
        public async UniTask WaitAllTasksAsync()
        {
            var task1 = LoadDataAsync();
            var task2 = LoadDataAsync();
            var task3 = LoadDataAsync();

            // 等待所有任务完成
            var results = await UniTask.WhenAll(task1, task2, task3);

            LogManager.Log($"所有任务完成，结果: {string.Join(", ", results)}", LogCategory.Framework);
        }

        /// <summary>
        /// 等待任意任务完成（WhenAny）
        /// </summary>
        public async UniTask WaitAnyTaskAsync()
        {
            var task1 = UniTask.Delay(1000);
            var task2 = UniTask.Delay(2000);
            var task3 = UniTask.Delay(500);

            // 等待最快完成的任务
            var (winnerIndex, _) = await UniTask.WhenAny(task1, task2, task3);

            LogManager.Log($"任务 {winnerIndex} 最先完成", LogCategory.Framework);
        }

        #endregion

        #region 异常处理

        /// <summary>
        /// 异常处理示例
        /// </summary>
        public async UniTaskVoid AsyncWithExceptionHandling()
        {
            try
            {
                await RiskyOperationAsync();
            }
            catch (OperationCanceledException)
            {
                LogManager.Log("操作被取消", LogCategory.Framework);
            }
            catch (Exception ex)
            {
                LogManager.LogError($"异常: {ex.Message}", LogCategory.Framework);
            }
        }

        private async UniTask RiskyOperationAsync()
        {
            await UniTask.Delay(1000);
            throw new Exception("模拟异常");
        }

        #endregion

        #region Fire-and-Forget

        /// <summary>
        /// 不等待结果（Fire-and-Forget）
        /// </summary>
        void Start()
        {
            // 方式1: 使用 Forget()
            InitializeAsync().Forget();

            // 方式2: 使用 UniTaskVoid
            DoSomethingAsync().Forget();
        }

        private async UniTask DoSomethingAsync()
        {
            await UniTask.Delay(1000);
            LogManager.Log("完成", LogCategory.Framework);
        }

        #endregion

        #region 生命周期

        void OnDestroy()
        {
            // 清理取消令牌
            _cts?.Cancel();
            _cts?.Dispose();
        }

        #endregion
    }
}
