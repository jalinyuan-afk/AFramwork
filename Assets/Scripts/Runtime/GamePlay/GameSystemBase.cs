namespace TradeGame.Runtime
{
    /// <summary>
    /// 游戏系统抽象基类
    /// 提供 IGameSystem 的默认实现，简化系统开发
    /// </summary>
    public abstract class GameSystemBase : IGameSystem
    {
        #region 初始化状态

        /// <summary>
        /// 系统是否已初始化
        /// </summary>
        public bool IsInitialized { get; private set; }

        #endregion

        #region 生命周期方法

        /// <summary>
        /// 初始化系统（由子类实现具体逻辑）
        /// </summary>
        protected abstract void OnInitialize();

        /// <summary>
        /// 所有系统就绪时调用（由子类实现具体逻辑）
        /// </summary>
        protected abstract void OnAllSystemsReady();

        /// <summary>
        /// 关闭系统（由子类实现具体逻辑）
        /// </summary>
        protected abstract void OnShutdown();

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized)
            {
                UnityEngine.Debug.LogWarning($"⚠️ {GetType().Name} 已经初始化过了，跳过重复初始化");
                return;
            }

            OnInitialize();
            IsInitialized = true;
        }

        /// <summary>
        /// 所有系统就绪
        /// </summary>
        public void OnSystemsReady()
        {
            if (!IsInitialized)
            {
                UnityEngine.Debug.LogWarning($"⚠️ {GetType().Name} 未初始化就调用 OnSystemsReady");
                return;
            }

            OnAllSystemsReady();
        }

        /// <summary>
        /// 关闭系统（由SystemManager调用）
        /// </summary>
        public void Shutdown()
        {
            OnShutdown();
            IsInitialized = false;
        }

        #endregion


        #region 暂停/恢复

        /// <summary>
        /// 系统是否已暂停
        /// </summary>
        public bool IsPaused { get; set; }

        /// <summary>
        /// 系统是否可以被暂停（默认为 true）
        /// 子类可以重写以禁止暂停
        /// </summary>
        public virtual bool CanBePaused => true;

        /// <summary>
        /// 系统暂停时调用（默认空实现）
        /// 子类可以重写以实现暂停逻辑
        /// </summary>
        public virtual void OnPause()
        {
            // 默认空实现
        }

        /// <summary>
        /// 系统恢复时调用（默认空实现）
        /// 子类可以重写以实现恢复逻辑
        /// </summary>
        public virtual void OnResume()
        {
            // 默认空实现
        }

        #endregion
    }
}
