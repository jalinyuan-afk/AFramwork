namespace TradeGame.Runtime
{
    public struct DayChangedEvent
    {
        public int Year;
        public int Month;
        public int Day;

        public DayChangedEvent(int year, int month, int day)
        {
            Year = year;
            Month = month;
            Day = day;
        }
    }

    public struct TimePeriodChangedEvent
    {
        public TimePeriod Period;       // 当前时间段
        public GameTime CurrentTime;    // 完整的游戏时间

        public TimePeriodChangedEvent(TimePeriod period, GameTime currentTime)
        {
            Period = period;
            CurrentTime = currentTime;
        }
    }
}