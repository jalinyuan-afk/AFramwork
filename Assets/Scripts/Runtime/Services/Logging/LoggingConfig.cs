using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TradeGame.Runtime
{
    /// <summary>
    /// 日志系统配置（完整版）
    /// 支持所有LogCategory分类的可视化配置
    /// </summary>
    [CreateAssetMenu(fileName = "LoggingConfig", menuName = "TradeGame/Configs/日志配置")]
    public class LoggingConfig : GameConfig
    {
        [Header("全局设置")]
        [SerializeField] private bool _globalEnabled = true;
        [SerializeField] private LogLevel _minLogLevel = LogLevel.Info;

        [Header("分类开关（动态生成）")]
        [SerializeField] private List<CategoryToggleData> _categoryToggles = new List<CategoryToggleData>();

        [Header("输出设置")]
        [SerializeField] private bool _logToFile = false;
        [SerializeField] private string _logFilePath = "Logs/game.log";
        [SerializeField] private bool _includeStackTrace = true;
        [SerializeField] private bool _includeTimestamp = true;

        [Header("快捷预设")]
        [SerializeField] private LogPreset _defaultPreset = LogPreset.CoreOnly;

        /// <summary>
        /// 分类开关数据（可序列化）
        /// </summary>
        [System.Serializable]
        public class CategoryToggleData
        {
            public LogCategory category;
            public bool enabled;
            [HideInInspector] public string displayName;

            public CategoryToggleData(LogCategory cat, bool en, string name)
            {
                category = cat;
                enabled = en;
                displayName = name;
            }
        }

        public enum LogPreset
        {
            DebugAll,      // 全部启用 + Verbose
            CoreOnly,      // 核心系统 + Info
            ImportantOnly, // 全部 + Warning
            Silent         // 仅Error
        }

        // 属性访问
        public bool GlobalEnabled => _globalEnabled;
        public LogLevel MinLogLevel => _minLogLevel;
        public bool LogToFile => _logToFile;
        public string LogFilePath => _logFilePath;
        public bool IncludeStackTrace => _includeStackTrace;
        public bool IncludeTimestamp => _includeTimestamp;
        public LogPreset DefaultPreset => _defaultPreset;

        /// <summary>
        /// 获取启用的分类组合
        /// </summary>
        public LogCategory GetEnabledCategories()
        {
            LogCategory result = LogCategory.None;

            foreach (var toggle in _categoryToggles)
            {
                if (toggle.enabled)
                {
                    result |= toggle.category;
                }
            }

            return result;
        }

        /// <summary>
        /// 应用预设配置
        /// </summary>
        public void ApplyPreset(LogPreset preset)
        {
            switch (preset)
            {
                case LogPreset.DebugAll:
                    SetAllCategories(true);
                    _minLogLevel = LogLevel.Verbose;
                    break;

                case LogPreset.CoreOnly:
                    SetCoreCategories();
                    _minLogLevel = LogLevel.Info;
                    break;

                case LogPreset.ImportantOnly:
                    SetAllCategories(true);
                    _minLogLevel = LogLevel.Warning;
                    break;

                case LogPreset.Silent:
                    SetAllCategories(false);
                    _minLogLevel = LogLevel.Error;
                    break;
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// 设置所有分类
        /// </summary>
        private void SetAllCategories(bool enabled)
        {
            foreach (var toggle in _categoryToggles)
            {
                toggle.enabled = enabled;
            }
        }

        /// <summary>
        /// 设置核心分类
        /// </summary>
        private void SetCoreCategories()
        {
            foreach (var toggle in _categoryToggles)
            {
                toggle.enabled = IsCoreCategory(toggle.category);
            }
        }

        /// <summary>
        /// 判断是否为核心分类
        /// </summary>
        private bool IsCoreCategory(LogCategory category)
        {
            return category switch
            {
                LogCategory.System => true,
                LogCategory.Quest => true,
                LogCategory.Trade => true,
                LogCategory.Scene => true,
                LogCategory.Procedure => true,
                LogCategory.Config => true,
                _ => false
            };
        }

        /// <summary>
        /// 获取默认启用状态
        /// </summary>
        private bool GetDefaultEnabled(LogCategory category)
        {
            // 核心分类默认启用
            return IsCoreCategory(category);
        }

        public override void OnConfigLoaded()
        {
            base.OnConfigLoaded();
            Debug.Log($"[LoggingConfig] Loaded: Level={_minLogLevel}, Categories={GetEnabledCategories()}, Count={_categoryToggles.Count}");
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor下自动同步所有分类
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            SyncCategories();
        }

        /// <summary>
        /// 同步分类列表（从LogCategoryConfig）
        /// </summary>
        private void SyncCategories()
        {
            var allCategories = LogCategoryConfig.GetAllCategories().ToList();

            // 保存现有配置
            var existingStates = _categoryToggles.ToDictionary(
                t => t.category,
                t => t.enabled
            );

            // 重建列表
            _categoryToggles.Clear();

            foreach (var info in allCategories)
            {
                bool enabled = existingStates.TryGetValue(info.Category, out bool state)
                    ? state
                    : GetDefaultEnabled(info.Category);

                _categoryToggles.Add(new CategoryToggleData(
                    info.Category,
                    enabled,
                    $"{info.Name} - {info.Description}"
                ));
            }
        }
#endif
    }
}
