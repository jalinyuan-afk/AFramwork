namespace TradeGame.Runtime
{
    /// <summary>
    /// 游戏系统接口
    /// 所有游戏系统必须实现此接口，以便统一管理生命周期
    /// </summary>
    public interface IGameSystem
    {
        #region 生命周期方法

        /// <summary>
        /// 系统初始化
        /// 在所有系统创建后、按优先级顺序调用
        /// 用于替代 Awake/Start，确保初始化顺序可控
        /// </summary>
        void Initialize();

        /// <summary>
        /// 所有系统初始化完成后调用
        /// 此时可以安全地访问其他系统
        /// </summary>
        void OnSystemsReady();

        /// <summary>
        /// 系统清理
        /// 场景切换或游戏退出时调用
        /// </summary>
        void Shutdown();

        #endregion

        #region 暂停/恢复

        /// <summary>
        /// 系统是否已暂停
        /// </summary>
        bool IsPaused { get; set; }

        /// <summary>
        /// 系统是否可以被暂停
        /// 某些关键系统（如存档系统）可能不允许暂停
        /// </summary>
        bool CanBePaused { get; }

        /// <summary>
        /// 系统暂停时调用
        /// 用于保存状态、释放资源等
        /// </summary>
        void OnPause();

        /// <summary>
        /// 系统恢复时调用
        /// 用于恢复状态、重新初始化等
        /// </summary>
        void OnResume();

        #endregion
    }
}
