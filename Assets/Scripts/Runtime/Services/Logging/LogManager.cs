
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TradeGame.Runtime
{


    /// <summary>
    /// 统一日志管理器
    /// </summary>
    public static class LogManager
    {

        /// 功能：
        /// - 按分类过滤日志（System/UI/Trade/Quest/AI等）
        /// - 按级别过滤（Verbose/Info/Warning/Error）
        /// - 运行时动态开关
        /// - 支持条件编译（仅在编辑器/开发版启用）
        // 当前启用的分类（运行时可修改）
        private static LogCategory enabledCategories = LogCategory.All;
        // 当前最小日志级别（低于此级别的日志将被忽略）
        private static LogLevel minLogLevel = LogLevel.Verbose;
        // 全局开关
        private static bool globalEnabled = true;

        #region 配置方法

        /// <summary>
        /// 设置启用的日志分类
        /// </summary>
        public static void SetEnabledCategories(LogCategory categories)
        {
            enabledCategories = categories;
        }

        /// <summary>
        /// 启用指定分类
        /// </summary>
        public static void EnableCategory(LogCategory category)
        {
            enabledCategories |= category;
        }

        /// <summary>
        /// 禁用指定分类
        /// </summary>
        public static void DisableCategory(LogCategory category)
        {
            enabledCategories &= ~category;
        }

        /// <summary>
        /// 设置最小日志级别
        /// </summary>
        public static void SetMinLogLevel(LogLevel level)
        {
            minLogLevel = level;
        }

        /// <summary>
        /// 全局启用/禁用日志
        /// </summary>
        public static void SetGlobalEnabled(bool enabled)
        {
            globalEnabled = enabled;
        }

        /// <summary>
        /// 检查分类是否启用
        /// </summary>
        public static bool IsCategoryEnabled(LogCategory category)
        {
            return (enabledCategories & category) != 0;
        }

        #endregion

        #region 日志方法

        /// <summary>
        /// 输出详细日志（Verbose）
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Verbose(LogCategory category, string message)
        {
            Log(category, LogLevel.Verbose, message);
        }

        /// <summary>
        /// 输出调试日志（Debug，等同于 Verbose）
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Debug(LogCategory category, string message)
        {
            Log(category, LogLevel.Verbose, message);
        }

        /// <summary>
        /// 输出信息日志（Info）
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Info(LogCategory category, string message)
        {
            Log(category, LogLevel.Info, message);
        }

        /// <summary>
        /// 输出信息日志（Info）- 带 Unity 对象上下文
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Info(LogCategory category, string message, UnityEngine.Object context)
        {
            if (!ShouldLog(category, LogLevel.Info)) return;
            string formatted = FormatMessage(category, LogLevel.Info, message);
            UnityEngine.Debug.Log(formatted, context);
        }

        /// <summary>
        /// 输出警告日志（Warning）
        /// </summary>
        public static void Warning(LogCategory category, string message)
        {
            if (!ShouldLog(category, LogLevel.Warning)) return;
            string formatted = FormatMessage(category, LogLevel.Warning, message);
            UnityEngine.Debug.LogWarning(formatted);
        }

        /// <summary>
        /// 输出警告日志（Warning）- 带 Unity 对象上下文
        /// </summary>
        public static void Warning(LogCategory category, string message, UnityEngine.Object context)
        {
            if (!ShouldLog(category, LogLevel.Warning)) return;
            string formatted = FormatMessage(category, LogLevel.Warning, message);
            UnityEngine.Debug.LogWarning(formatted, context);
        }

        /// <summary>
        /// 输出错误日志（Error）
        /// </summary>
        public static void Error(LogCategory category, string message)
        {
            if (!ShouldLog(category, LogLevel.Error)) return;
            string formatted = FormatMessage(category, LogLevel.Error, message);
            UnityEngine.Debug.LogError(formatted);
        }

        /// <summary>
        /// 输出错误日志（Error）- 带 Unity 对象上下文
        /// </summary>
        public static void Error(LogCategory category, string message, UnityEngine.Object context)
        {
            if (!ShouldLog(category, LogLevel.Error)) return;
            string formatted = FormatMessage(category, LogLevel.Error, message);
            UnityEngine.Debug.LogError(formatted, context);
        }

        /// <summary>
        /// 通用日志方法
        /// </summary>
        private static void Log(LogCategory category, LogLevel level, string message)
        {
            if (!ShouldLog(category, level)) return;
            string formatted = FormatMessage(category, level, message);
            UnityEngine.Debug.Log(formatted);
        }

        /// <summary>
        /// 检查是否应该输出日志
        /// </summary>
        private static bool ShouldLog(LogCategory category, LogLevel level)
        {
            if (!globalEnabled) return false;
            if (level < minLogLevel) return false;
            if ((enabledCategories & category) == 0) return false;
            return true;
        }

        /// <summary>
        /// 格式化日志消息
        /// </summary>
        private static string FormatMessage(LogCategory category, LogLevel level, string message)
        {
            string levelIcon = GetLevelIcon(level);
            string categoryName = LogCategoryConfig.GetCategoryName(category);
            string color = LogCategoryConfig.GetCategoryColor(category);

            return $"{levelIcon} <color={color}>[{categoryName}]</color> {message}";
        }

        /// <summary>
        /// 获取日志级别图标
        /// </summary>
        private static string GetLevelIcon(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Verbose: return "🔍";
                case LogLevel.Info: return "ℹ️";
                case LogLevel.Warning: return "⚠️";
                case LogLevel.Error: return "❌";
                default: return "";
            }
        }

        #endregion

        #region 预设配置

        /// <summary>
        /// 预设：仅显示重要信息（Warning + Error）
        /// </summary>
        public static void PresetImportantOnly()
        {
            SetMinLogLevel(LogLevel.Warning);
            SetEnabledCategories(LogCategory.All);
        }

        /// <summary>
        /// 预设：仅显示系统核心日志
        /// </summary>
        public static void PresetCoreOnly()
        {
            SetMinLogLevel(LogLevel.Info);
            SetEnabledCategories(LogCategory.System | LogCategory.Quest | LogCategory.Trade);
        }

        /// <summary>
        /// 预设：调试模式（显示所有）
        /// </summary>
        public static void PresetDebugAll()
        {
            SetMinLogLevel(LogLevel.Verbose);
            SetEnabledCategories(LogCategory.All);
        }

        /// <summary>
        /// 预设：静默模式（仅错误）
        /// </summary>
        public static void PresetSilent()
        {
            SetMinLogLevel(LogLevel.Error);
            SetEnabledCategories(LogCategory.All);
        }

        /// <summary>
        /// 预设：发布版（禁用所有日志）
        /// </summary>
        public static void PresetRelease()
        {
            SetGlobalEnabled(false);
        }

        #endregion

        #region 调试工具

        /// <summary>
        /// 打印当前日志配置
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void PrintCurrentConfig()
        {
            LogManager.Info(LogCategory.Trade, "==== LogManager 配置 ====");
            LogManager.Info(LogCategory.Trade, $"全局启用: {globalEnabled}");
            LogManager.Info(LogCategory.Trade, $"最小级别: {minLogLevel}");
            LogManager.Info(LogCategory.Trade, $"启用分类: {enabledCategories}");
        }

        #endregion
    }
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Verbose = 0,    // 详细信息（开发调试用）
        Info = 1,       // 一般信息
        Warning = 2,    // 警告
        Error = 3       // 错误
    }


}


