using System;
using System.Collections.Generic;
using System.Linq;

namespace TradeGame.Runtime
{
    /// <summary>
    /// 日志分类
    /// </summary>
    [Flags]
    public enum LogCategory
    {
        /// 使用 Flags 特性支持位运算组合
        None = 0,
        System = 1 << 0,        // 系统初始化、生命周期
        UI = 1 << 1,            // UI 交互、显示
        Trade = 1 << 2,         // 交易、商人
        Quest = 1 << 3,         // 任务系统
        Inventory = 1 << 4,     // 库存系统
        Time = 1 << 5,          // 时间推进
        Weather = 1 << 6,       // 天气系统
        Travel = 1 << 7,        // 旅行、移动
        City = 1 << 8,          // 城市交互
        AI = 1 << 9,            // AI 系统
        Event = 1 << 10,        // 事件系统
        Resource = 1 << 11,     // 资源管理
        Save = 1 << 12,         // 存档系统
        Character = 1 << 13,    // 角色系统
        Gameplay = 1 << 14,     // 核心玩法
        Procedure = 1 << 15,    // 流程管理
        Config = 1 << 16,       // 配置管理
        Localization = 1 << 17, // 本地化系统
        DataTable = 1 << 19,    // 数据表管理
        Sound = 1 << 20,        // 音频系统
        Scene = 1 << 21,        // 场景管理
        Setting = 1 << 22,      // 设置系统
        Entity = 1 << 23,      // 实体系统
        FSM = 1 << 24,         // 有限状态机
        All = ~0                // 全部分类
    }

    /// <summary>
    /// 日志分类配置信息
    /// 描述单个分类的元数据（名称、颜色、描述等）
    /// </summary>
    public class LogCategoryInfo
    {
        /// <summary>分类枚举值</summary>
        public LogCategory Category { get; set; }

        /// <summary>显示名称</summary>
        public string Name { get; set; }

        /// <summary>Console 颜色（Unity 富文本）</summary>
        public string Color { get; set; }

        /// <summary>分类描述</summary>
        public string Description { get; set; }

        public LogCategoryInfo(LogCategory category, string name, string color, string description)
        {
            Category = category;
            Name = name;
            Color = color;
            Description = description;
        }
    }

    /// <summary>
    /// 日志分类配置中心
    /// 集中管理所有分类的定义和颜色映射
    /// 新增分类只需在此处添加配置即可
    /// </summary>
    public static class LogCategoryConfig
    {
        /// <summary>
        /// 所有分类的配置列表
        /// 新增分类：在此数组添加新配置即可
        /// </summary>
        private static readonly LogCategoryInfo[] categoryInfos = new[]
        {
            new LogCategoryInfo(LogCategory.System, "System", "#00BFFF", "系统初始化、生命周期"),
            new LogCategoryInfo(LogCategory.UI, "UI", "#FF69B4", "UI 交互、显示"),
            new LogCategoryInfo(LogCategory.Trade, "Trade", "#FFD700", "交易、商人"),
            new LogCategoryInfo(LogCategory.Quest, "Quest", "#9370DB", "任务系统"),
            new LogCategoryInfo(LogCategory.Inventory, "Inventory", "#32CD32", "库存系统"),
            new LogCategoryInfo(LogCategory.Time, "Time", "#FFA500", "时间推进"),
            new LogCategoryInfo(LogCategory.Weather, "Weather", "#87CEEB", "天气系统"),
            new LogCategoryInfo(LogCategory.Travel, "Travel", "#20B2AA", "旅行、移动"),
            new LogCategoryInfo(LogCategory.City, "City", "#FF6347", "城市交互"),
            new LogCategoryInfo(LogCategory.AI, "AI", "#DA70D6", "AI 系统"),
            new LogCategoryInfo(LogCategory.Event, "Event", "#F0E68C", "事件系统"),
            new LogCategoryInfo(LogCategory.Resource, "Resource", "#98FB98", "资源管理"),
            new LogCategoryInfo(LogCategory.Save, "Save", "#8B4513", "存档系统"),
            new LogCategoryInfo(LogCategory.Character, "Character", "#FF4500", "角色系统"),
            new LogCategoryInfo(LogCategory.Gameplay, "Gameplay", "#1E90FF", "核心玩法"),
            new LogCategoryInfo(LogCategory.Procedure, "Procedure", "#32CD32", "流程管理"),
            new LogCategoryInfo(LogCategory.Config, "Config", "#8A2BE2", "配置管理"),
            new LogCategoryInfo(LogCategory.Localization, "Localization", "#FF8C00", "本地化系统"),
            new LogCategoryInfo(LogCategory.DataTable, "DataTable", "#00CED1", "数据表管理"),
            new LogCategoryInfo(LogCategory.Sound, "Sound", "#1E90FF", "音频系统"),
            new LogCategoryInfo(LogCategory.Scene, "Scene", "#FF4500", "场景管理"),
            new LogCategoryInfo(LogCategory.Setting, "Setting", "#20B2AA", "设置系统"),
            new LogCategoryInfo(LogCategory.Entity, "Entity", "#FF69B4", "实体系统"),
        };

        /// <summary>
        /// 颜色映射字典（延迟初始化）
        /// </summary>
        private static Dictionary<LogCategory, string> categoryColors;

        /// <summary>
        /// 分类信息字典（延迟初始化）
        /// </summary>
        private static Dictionary<LogCategory, LogCategoryInfo> categoryInfoDict;

        /// <summary>
        /// 获取所有有效分类（排除 None 和 All）
        /// </summary>
        public static IEnumerable<LogCategoryInfo> GetAllCategories()
        {
            return categoryInfos;
        }

        /// <summary>
        /// 获取分类颜色
        /// </summary>
        public static string GetCategoryColor(LogCategory category)
        {
            EnsureInitialized();
            return categoryColors.TryGetValue(category, out string color) ? color : "#FFFFFF";
        }

        /// <summary>
        /// 获取分类信息
        /// </summary>
        public static LogCategoryInfo GetCategoryInfo(LogCategory category)
        {
            EnsureInitialized();
            return categoryInfoDict.TryGetValue(category, out var info) ? info : null;
        }

        /// <summary>
        /// 获取分类名称
        /// </summary>
        public static string GetCategoryName(LogCategory category)
        {
            var info = GetCategoryInfo(category);
            return info?.Name ?? category.ToString();
        }

        /// <summary>
        /// 获取分类描述
        /// </summary>
        public static string GetCategoryDescription(LogCategory category)
        {
            var info = GetCategoryInfo(category);
            return info?.Description ?? "";
        }

        /// <summary>
        /// 检查是否是单一分类（非组合）
        /// </summary>
        public static bool IsSingleCategory(LogCategory category)
        {
            if (category == LogCategory.None || category == LogCategory.All)
                return false;

            // 检查是否只有一个位被设置
            return (category & (category - 1)) == 0;
        }

        /// <summary>
        /// 获取所有单一分类的枚举值（排除 None 和 All）
        /// </summary>
        public static IEnumerable<LogCategory> GetSingleCategories()
        {
            return categoryInfos.Select(info => info.Category);
        }

        /// <summary>
        /// 延迟初始化字典
        /// </summary>
        private static void EnsureInitialized()
        {
            if (categoryColors == null)
            {
                categoryColors = categoryInfos.ToDictionary(info => info.Category, info => info.Color);
                categoryInfoDict = categoryInfos.ToDictionary(info => info.Category);
            }
        }
    }
}
