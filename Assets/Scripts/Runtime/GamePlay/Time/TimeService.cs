using System;
using UnityEngine;
using TradeGame.Runtime.Framework;

namespace TradeGame.Runtime
{
    /// <summary>
    /// 时间系统
    /// 职责：全局时间推进、年月日和时间段管理
    /// 
    /// 时间规则：
    /// - 一年 = 12个月
    /// - 一月 = 30天（固定）
    /// - 一天 = 4个时间段（上午、中午、下午、晚上）
    /// 
    /// 时间推进方式：
    /// 1. 城市外（野外）：实时推进，按配置的秒数自动推进时间段
    ///    - 默认：5秒 = 1个时间段，20秒 = 1天
    /// 2. 城市内：活动驱动推进，每个活动推进指定的时间段数
    ///    - 进入商店：1个时间段
    ///    - 住宿：4个时间段（1天）或更多
    /// </summary>
    public class TimeService : GameSystemBase, ITimeService
    {
        public override bool CanBePaused => true;

        private IEventBus _eventBus;
        private IDisposable _subDayChanged;
        private IDisposable _subTimePeriodChanged;

        #region 配置参数

        // 初始时间配置
        [Header("初始时间设置")]
        public int startYear = 1250;
        public int startMonth = 3;
        public int startDay = 15;
        public TimePeriod startTimePeriod = TimePeriod.Morning;

        // 时间流速配置（城市外）
        [Header("城市外时间流速")]
        [Tooltip("野外移动时，多少秒推进1个时间段（默认5秒 = 1个时间段，20秒 = 1天）")]
        public float secondsPerTimePeriod = 5f;

        // 调试
        [Header("调试")]
        public bool showDebugInfo = true;
        public bool showTimePeriodChange = true;  // 是否显示时间段变化
        public bool showDayChange = true;          // 是否显示日期变化

        #endregion

        #region 私有字段

        // 当前游戏时间
        private GameTime currentTime;

        // 城市外时间累积器（用于实时推进）
        private float worldTimeAccumulator = 0f;

        // 系统依赖
        // private WeatherSystem weatherSystem;

        #endregion

        #region GameSystemBase 实现

        protected override void OnInitialize()
        {
            // 初始化游戏时间
            currentTime = new GameTime(startYear, startMonth, startDay, startTimePeriod);
            worldTimeAccumulator = 0f;

            // 事件总线（可选）：订阅外部时间事件或用于发布本系统产生的时间事件
            _eventBus = ServiceLocator.Get<IEventBus>();
            if (_eventBus != null)
            {
                _subDayChanged = _eventBus.Subscribe<DayChangedEvent>(OnDayChanged);
                _subTimePeriodChanged = _eventBus.Subscribe<TimePeriodChangedEvent>(OnTimePeriodChanged);
            }

            if (showDebugInfo)
            {
                LogManager.Info(LogCategory.Time, $"⏰ TimeSystem 初始化: {currentTime.GetDateString()}");
            }
        }

        protected override void OnAllSystemsReady()
        {
            // 获取依赖的系统
            //weatherSystem = SystemManager.Instance.GetSystem<WeatherSystem>();

            // TODO: SaveSystem - 从存档加载时间数据
            if (showDebugInfo)
            {
                LogManager.Info(LogCategory.Time, "⏰ TimeSystem 就绪");
            }
        }

        protected override void OnShutdown()
        {

            // 取消订阅事件
            try { _subDayChanged?.Dispose(); } catch { }
            try { _subTimePeriodChanged?.Dispose(); } catch { }

            if (showDebugInfo)
            {
                LogManager.Info(LogCategory.Time, "⏰ TimeSystem 已关闭");
            }
        }

        #endregion

        #region 城市外时间推进（实时）

        /// <summary>
        /// 更新野外时间（城市外移动时调用）
        /// 按配置的秒数自动推进时间段
        /// </summary>
        /// <param name="deltaTime">增量时间（秒）</param>
        public void UpdateWorldTime(float deltaTime)
        {
            worldTimeAccumulator += deltaTime;

            // 检查是否达到一个时间段
            while (worldTimeAccumulator >= secondsPerTimePeriod)
            {
                worldTimeAccumulator -= secondsPerTimePeriod;
                AdvanceTimePeriod(1);
            }
        }

        #endregion

        #region 城市内时间推进（活动驱动）

        /// <summary>
        /// 推进时间段（城市内活动调用）
        /// 用于城市内各种活动推进时间
        /// </summary>
        /// <param name="periods">推进的时间段数（默认1）</param>
        /// <param name="activityName">活动名称（用于调试日志）</param>
        public void AdvanceTimePeriod(int periods = 1, string activityName = "")
        {
            if (periods <= 0) return;

            TimePeriod oldPeriod = currentTime.period;
            int oldDay = currentTime.day;
            int oldMonth = currentTime.month;
            int oldYear = currentTime.year;

            // 推进时间段
            for (int i = 0; i < periods; i++)
            {
                currentTime.period = (TimePeriod)(((int)currentTime.period + 1) % 4);

                // 如果回到上午，说明跨天了
                if (currentTime.period == TimePeriod.Morning)
                {
                    currentTime.AdvanceDays(1);

                    // 触发天气变化（每天零点）
                    // weatherSystem?.RollNextDayWeather();

                    // 检查是否跨月
                    if (currentTime.month != oldMonth)
                    {
                        //  EventBus.Publish(new MonthChangedEvent(currentTime.year, currentTime.month));

                        if (showDayChange)
                        {
                            LogManager.Info(LogCategory.Time, $"📅 新的月份: {currentTime.year}年{currentTime.month}月");
                        }
                    }

                    // 检查是否跨年
                    if (currentTime.year != oldYear)
                    {
                        // EventBus.Publish(new YearChangedEvent(currentTime.year));

                        if (showDayChange)
                        {
                            LogManager.Info(LogCategory.Time, $"🎊 新的年份: {currentTime.year}年");
                        }
                    }

                    // 发布日期变化事件
                    _eventBus?.Publish(new DayChangedEvent(currentTime.year, currentTime.month, currentTime.day));

                    if (showDayChange)
                    {
                        LogManager.Info(LogCategory.Time, $"📅 新的一天: {currentTime.GetDateString()}");
                    }
                }

                // 发布时间段变化事件
                if (showTimePeriodChange)
                {
                    string activity = string.IsNullOrEmpty(activityName) ? "" : $"（{activityName}）";
                    LogManager.Info(LogCategory.Time, $"⏰ 时间推进: {currentTime.GetDateString()} {activity}");
                }
            }

            _eventBus?.Publish(new TimePeriodChangedEvent(currentTime.period, currentTime.Clone()));
        }

        /// <summary>
        /// 直接推进天数（用于特殊事件，如住宿多天）
        /// </summary>
        /// <param name="days">推进的天数</param>
        /// <param name="activityName">活动名称（用于调试日志）</param>
        public void AdvanceDays(int days, string activityName = "")
        {
            if (days <= 0) return;

            // 推进整天 = 推进 days * 4 个时间段
            AdvanceTimePeriod(days * 4, activityName);
        }

        #endregion

        #region 查询接口

        /// <summary>
        /// 获取当前游戏时间（完整对象）
        /// </summary>
        public GameTime GetCurrentTime()
        {
            return currentTime.Clone();
        }

        /// <summary>
        /// 获取当前年份
        /// </summary>
        public int GetCurrentYear()
        {
            return currentTime.year;
        }

        /// <summary>
        /// 获取当前月份
        /// </summary>
        public int GetCurrentMonth()
        {
            return currentTime.month;
        }

        /// <summary>
        /// 获取当前日期
        /// </summary>
        public int GetCurrentDay()
        {
            return currentTime.day;
        }

        /// <summary>
        /// 获取当前时间段
        /// </summary>
        public TimePeriod GetCurrentTimePeriod()
        {
            return currentTime.period;
        }

        /// <summary>
        /// 获取总天数（从游戏开始算起）
        /// </summary>
        public int GetTotalDays()
        {
            return currentTime.totalDays;
        }

        /// <summary>
        /// 获取时间字符串（用于UI显示）
        /// 示例：1250年3月15日 上午
        /// </summary>
        public string GetTimeString()
        {
            return currentTime.GetDateString();
        }

        /// <summary>
        /// 获取简短时间字符串
        /// 示例：1250/3/15 上午
        /// </summary>
        public string GetShortTimeString()
        {
            return currentTime.GetShortDateString();
        }

        #endregion



        /// <summary>
        /// 设置时间（用于读档）
        /// </summary>
        public void SetTime(GameTime time)
        {
            if (time == null)
            {
                LogManager.Error(LogCategory.Time, "❌ SetTime: 时间数据为空");
                return;
            }

            currentTime = time.Clone();
            worldTimeAccumulator = 0f;

            // 发布事件通知其他系统
            _eventBus?.Publish(new TimePeriodChangedEvent(currentTime.period, currentTime.Clone()));
            _eventBus?.Publish(new DayChangedEvent(currentTime.year, currentTime.month, currentTime.day));

            if (showDebugInfo)
            {
                LogManager.Info(LogCategory.Time, $"⏰ 时间已设置: {currentTime.GetDateString()}");
            }
        }

        /// <summary>
        /// 重置时间到指定日期
        /// </summary>
        public void ResetTime(int year, int month, int day, TimePeriod period)
        {
            currentTime.Reset(year, month, day, period);
            worldTimeAccumulator = 0f;

            if (showDebugInfo)
            {
                LogManager.Info(LogCategory.Time, $"⏰ 时间已重置: {currentTime.GetDateString()}");
            }
        }




        #region 事件处理器

        private void OnDayChanged(DayChangedEvent e)
        {
            if (showDayChange)
            {
                LogManager.Info(LogCategory.Time, $"📅 新的一天: {e.Year}年{e.Month}月{e.Day}日");
            }
        }

        private void OnTimePeriodChanged(TimePeriodChangedEvent e)
        {
            if (showTimePeriodChange)
            {
                LogManager.Verbose(LogCategory.Time, $"⏰ 时间段变化: {e.Period}");
            }
        }

        #endregion
    }
}
