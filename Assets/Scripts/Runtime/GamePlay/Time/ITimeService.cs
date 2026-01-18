using System;

namespace TradeGame.Runtime
{
    /// <summary>
    /// 抽象时间服务接口，供其他系统通过依赖注入或 ServiceLocator 使用
    /// </summary>
    public interface ITimeService
    {
        GameTime GetCurrentTime();
        int GetCurrentYear();
        int GetCurrentMonth();
        int GetCurrentDay();
        TimePeriod GetCurrentTimePeriod();
        int GetTotalDays();
        string GetTimeString();
        string GetShortTimeString();

        void UpdateWorldTime(float deltaTime);
        void AdvanceTimePeriod(int periods = 1, string activityName = "");
        void AdvanceDays(int days, string activityName = "");
        void SetTime(GameTime time);
        void ResetTime(int year, int month, int day, TimePeriod period);
    }
}
