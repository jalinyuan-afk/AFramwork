using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TradeGame.Runtime.Framework
{
    /// <summary>
    /// 异步任务调度器接口
    /// </summary>
    public interface ITaskScheduler
    {
        /// <summary>
        /// 执行异步任务，并跟踪其生命周期
        /// </summary>
        UniTask Run(Func<CancellationToken, UniTask> taskFactory, string taskName = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行异步任务，返回任务ID
        /// </summary>
        int RunWithId(Func<CancellationToken, UniTask> taskFactory, string taskName = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 取消指定ID的任务
        /// </summary>
        bool CancelTask(int taskId);

        /// <summary>
        /// 取消所有任务
        /// </summary>
        void CancelAll();

        /// <summary>
        /// 等待所有任务完成
        /// </summary>
        UniTask WaitAll();

        /// <summary>
        /// 获取当前活跃任务数量
        /// </summary>
        int ActiveTaskCount { get; }
    }
}