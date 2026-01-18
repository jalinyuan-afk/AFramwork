using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TradeGame.Runtime.Framework
{
    /// <summary>
    /// 任务调度器扩展方法
    /// </summary>
    public static class TaskSchedulerExtensions
    {
        /// <summary>
        /// 执行异步任务，忽略结果
        /// </summary>
        public static void RunFireAndForget(this ITaskScheduler scheduler, Func<CancellationToken, UniTask> taskFactory, string taskName = null)
        {
            scheduler.Run(taskFactory, taskName).Forget();
        }

        /// <summary>
        /// 延迟执行任务
        /// </summary>
        public static UniTask RunDelayed(this ITaskScheduler scheduler, TimeSpan delay, Func<CancellationToken, UniTask> taskFactory, string taskName = null, CancellationToken cancellationToken = default)
        {
            return scheduler.Run(async ct =>
            {
                await UniTask.Delay(delay, cancellationToken: ct);
                await taskFactory(ct);
            }, taskName, cancellationToken);
        }

        /// <summary>
        /// 顺序执行多个任务
        /// </summary>
        public static UniTask RunSequential(this ITaskScheduler scheduler, IEnumerable<Func<CancellationToken, UniTask>> tasks, string taskName = null, CancellationToken cancellationToken = default)
        {
            return scheduler.Run(async ct =>
            {
                foreach (var task in tasks)
                {
                    await task(ct);
                }
            }, taskName, cancellationToken);
        }
    }
}