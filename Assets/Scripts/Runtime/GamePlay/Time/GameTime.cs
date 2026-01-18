using UnityEngine;

namespace TradeGame.Runtime
{
    /// <summary>
    /// 游戏时间数据（用于存档和时间管理）
    /// 设计：
    /// - 一年 = 12个月
    /// - 一月 = 30天（固定）
    /// - 一天 = 4个时间段（上午、中午、下午、晚上）
    /// </summary>
    [System.Serializable]
    public class GameTime : System.IComparable<GameTime>
    {
        // 年月日
        public int year;
        public int month;   // 1-12
        public int day;     // 1-30

        // 时间段
        public TimePeriod period;

        // 总天数（方便计算时间间隔）
        public int totalDays;

        /// <summary>
        /// 默认构造函数（初始时间：1年1月1日上午）
        /// </summary>
        public GameTime()
        {
            year = 1;
            month = 1;
            day = 1;
            period = TimePeriod.Morning;
            totalDays = 0;
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public GameTime(int year, int month, int day, TimePeriod period)
        {
            this.year = year;
            this.month = month;
            this.day = day;
            this.period = period;
            this.totalDays = CalculateTotalDays(year, month, day);
        }

        /// <summary>
        /// 拷贝构造函数
        /// </summary>
        public GameTime(GameTime other)
        {
            this.year = other.year;
            this.month = other.month;
            this.day = other.day;
            this.period = other.period;
            this.totalDays = other.totalDays;
        }

        /// <summary>
        /// 计算总天数（从第1年1月1日开始）
        /// </summary>
        private int CalculateTotalDays(int year, int month, int day)
        {
            // (year - 1) * 360 + (month - 1) * 30 + (day - 1)
            return (year - 1) * 360 + (month - 1) * 30 + (day - 1);
        }

        /// <summary>
        /// 获取时间字符串（用于显示）
        /// 示例：1250年3月15日 上午
        /// </summary>
        public string GetDateString()
        {
            return $"{year}年{month}月{day}日 {GetPeriodName()}";
        }

        /// <summary>
        /// 获取简短时间字符串
        /// 示例：1250/3/15 上午
        /// </summary>
        public string GetShortDateString()
        {
            return $"{year}/{month}/{day} {GetPeriodName()}";
        }

        /// <summary>
        /// 获取时间段名称
        /// </summary>
        public string GetPeriodName()
        {
            switch (period)
            {
                case TimePeriod.Morning: return "上午";
                case TimePeriod.Noon: return "中午";
                case TimePeriod.Afternoon: return "下午";
                case TimePeriod.Night: return "晚上";
                default: return "未知";
            }
        }

        /// <summary>
        /// 推进时间段
        /// </summary>
        /// <param name="periods">推进的时间段数</param>
        public void AdvancePeriods(int periods)
        {
            for (int i = 0; i < periods; i++)
            {
                // 推进一个时间段
                period = (TimePeriod)(((int)period + 1) % 4);

                // 如果回到上午，说明跨天了
                if (period == TimePeriod.Morning)
                {
                    AdvanceDays(1);
                }
            }
        }

        /// <summary>
        /// 推进天数
        /// </summary>
        public void AdvanceDays(int days)
        {
            day += days;
            totalDays += days;

            // 处理跨月
            while (day > 30)
            {
                day -= 30;
                month++;

                // 处理跨年
                if (month > 12)
                {
                    month -= 12;
                    year++;
                }
            }
        }

        /// <summary>
        /// 比较两个时间（返回天数差）
        /// </summary>
        public int DaysDifference(GameTime other)
        {
            return this.totalDays - other.totalDays;
        }

        /// <summary>
        /// 实现 IComparable 接口（用于时间比较和排序）
        /// 返回值：< 0 表示 this < other，0 表示相等，> 0 表示 this > other
        /// </summary>
        public int CompareTo(GameTime other)
        {
            if (other == null) return 1;

            // 先比较总天数
            int dayDiff = this.totalDays - other.totalDays;
            if (dayDiff != 0) return dayDiff;

            // 天数相同，比较时间段
            return (int)this.period - (int)other.period;
        }

        /// <summary>
        /// 判断是否在指定时间之后
        /// </summary>
        public bool IsAfter(GameTime other)
        {
            return CompareTo(other) > 0;
        }

        /// <summary>
        /// 判断是否在指定时间之前
        /// </summary>
        public bool IsBefore(GameTime other)
        {
            return CompareTo(other) < 0;
        }

        /// <summary>
        /// 克隆当前时间
        /// </summary>
        public GameTime Clone()
        {
            return new GameTime(this);
        }

        /// <summary>
        /// 重置时间
        /// </summary>
        public void Reset(int year, int month, int day, TimePeriod period)
        {
            this.year = year;
            this.month = month;
            this.day = day;
            this.period = period;
            this.totalDays = CalculateTotalDays(year, month, day);
        }
    }
}
