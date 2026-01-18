using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TradeGame.Runtime.Framework
{
    /// <summary>
    /// 被跟踪的任务
    /// </summary>
    internal class TrackedTask : IDisposable
    {
        private readonly TaskScheduler _scheduler;
        public int Id { get; }
        public string Name { get; }
        public CancellationTokenSource LinkedCts { get; }
        public UniTaskCompletionSource CompletionSource { get; }
        public UniTask CompletionTask => CompletionSource.Task;

        public TrackedTask(TaskScheduler scheduler, int id, string name, CancellationTokenSource linkedCts)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            Id = id;
            Name = name ?? $"Task_{id}";
            LinkedCts = linkedCts;
            CompletionSource = new UniTaskCompletionSource();
        }

        public async UniTaskVoid Start(Func<CancellationToken, UniTask> taskFactory)
        {
            try
            {
                await taskFactory(LinkedCts.Token);
                CompletionSource.TrySetResult();
            }
            catch (OperationCanceledException)
            {
                CompletionSource.TrySetCanceled();
            }
            catch (Exception e)
            {
                Debug.LogError($"[TaskScheduler] 任务 '{Name}' 失败: {e}");
                CompletionSource.TrySetException(e);
            }
            finally
            {
                // 立即清理任务，避免内存泄漏
                _scheduler.RemoveTask(Id);
            }
        }

        public void Cancel()
        {
            LinkedCts?.Cancel();
        }

        public void Dispose()
        {
            LinkedCts?.Dispose();
        }
    }
}