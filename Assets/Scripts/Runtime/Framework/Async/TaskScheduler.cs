using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TradeGame.Runtime.Framework
{
    /// <summary>
    /// 基于UniTask的任务调度器实现
    /// </summary>
    public class TaskScheduler : ITaskScheduler, IDisposable
    {
        private readonly Dictionary<int, TrackedTask> _activeTasks = new();
        private int _nextTaskId = 1;
        private CancellationTokenSource _globalCts;
        private readonly object _lock = new object();

        public TaskScheduler()
        {
            _globalCts = new CancellationTokenSource();
        }

        /// <summary>
        /// 当前活跃任务数量
        /// </summary>
        public int ActiveTaskCount => _activeTasks.Count;

        /// <summary>
        /// 执行异步任务
        /// </summary>
        public UniTask Run(Func<CancellationToken, UniTask> taskFactory, string taskName = null, CancellationToken cancellationToken = default)
        {
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_globalCts.Token, cancellationToken);
            var taskId = _nextTaskId++;
            var trackedTask = new TrackedTask(this, taskId, taskName, linkedCts);
            lock (_lock)
            {
                _activeTasks.Add(taskId, trackedTask);
            }

            // 启动任务
            trackedTask.Start(taskFactory).Forget();

            // 返回一个等待任务完成的UniTask
            return trackedTask.CompletionTask;
        }

        /// <summary>
        /// 执行异步任务，返回任务ID
        /// </summary>
        public int RunWithId(Func<CancellationToken, UniTask> taskFactory, string taskName = null, CancellationToken cancellationToken = default)
        {
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_globalCts.Token, cancellationToken);
            var taskId = _nextTaskId++;
            var trackedTask = new TrackedTask(this, taskId, taskName, linkedCts);
            lock (_lock)
            {
                _activeTasks.Add(taskId, trackedTask);
            }

            trackedTask.Start(taskFactory).Forget();
            return taskId;
        }

        /// <summary>
        /// 取消指定ID的任务
        /// </summary>
        public bool CancelTask(int taskId)
        {
            lock (_lock)
            {
                if (_activeTasks.TryGetValue(taskId, out var task))
                {
                    task.Cancel();
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 取消所有任务
        /// </summary>
        public void CancelAll()
        {
            _globalCts?.Cancel();
            _globalCts?.Dispose();
            _globalCts = new CancellationTokenSource();
        }

        /// <summary>
        /// 等待所有任务完成
        /// </summary>
        public async UniTask WaitAll()
        {
            List<UniTask> tasks;
            lock (_lock)
            {
                tasks = _activeTasks.Values.Select(t => t.CompletionTask).ToList();
            }
            await UniTask.WhenAll(tasks);
        }


        /// <summary>
        /// 移除已完成的任务（由TrackedTask调用）
        /// </summary>
        internal void RemoveTask(int taskId)
        {
            lock (_lock)
            {
                if (_activeTasks.TryGetValue(taskId, out var task))
                {
                    _activeTasks.Remove(taskId);
                    task.Dispose();
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            CancelAll();
            _globalCts?.Dispose();

            lock (_lock)
            {
                foreach (var task in _activeTasks.Values)
                {
                    task.Dispose();
                }
                _activeTasks.Clear();
            }

        }
        /// <summary>
        /// 全局单例实例（可选）
        /// </summary>
        public static TaskScheduler Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            Instance = new TaskScheduler();
        }
    }


}