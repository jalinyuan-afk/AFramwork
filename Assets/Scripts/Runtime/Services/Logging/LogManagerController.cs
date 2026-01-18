using UnityEngine;

namespace TradeGame.Runtime
{
    /// <summary>
    /// LogManager 运行时控制面板
    /// </summary>
    public class LogManagerController : MonoBehaviour
    {
        /// 职责：读取 LoggingConfig 配置并应用到 LogManager
        /// 配置管理交给 ScriptableObject，本组件只负责应用
        [Header("配置引用")]
        [SerializeField] private LoggingConfig loggingConfig;

        [Header("运行时覆盖")]
        [Tooltip("运行时是否覆盖配置（用于临时调试）")]
        [SerializeField] private bool overrideConfig = false;

        [SerializeField] private bool overrideGlobalEnabled = true;
        [SerializeField] private LogLevel overrideMinLogLevel = LogLevel.Verbose;
        [SerializeField] private LogCategory overrideCategories = LogCategory.All;

        [Header("启动行为")]
        [SerializeField] private bool applyOnStart = true;
        [SerializeField] private bool applyPresetOnStart = false;
        [SerializeField] private LoggingConfig.LogPreset startPreset = LoggingConfig.LogPreset.CoreOnly;

        private void Start()
        {
            if (applyPresetOnStart && loggingConfig != null)
            {
                loggingConfig.ApplyPreset(startPreset);
            }

            if (applyOnStart)
            {
                ApplySettings();
            }
        }

        private void OnValidate()
        {
            // Inspector修改时实时应用
            if (Application.isPlaying)
            {
                ApplySettings();
            }
        }

        /// <summary>
        /// 应用配置到 LogManager
        /// </summary>
        [ContextMenu("应用配置")]
        public void ApplySettings()
        {
            if (overrideConfig)
            {
                // 使用运行时覆盖值
                LogManager.SetGlobalEnabled(overrideGlobalEnabled);
                LogManager.SetMinLogLevel(overrideMinLogLevel);
                LogManager.SetEnabledCategories(overrideCategories);
                Debug.Log("[LogManagerController] 已应用运行时覆盖配置");
            }
            else if (loggingConfig != null)
            {
                // 使用配置资源
                LogManager.SetGlobalEnabled(loggingConfig.GlobalEnabled);
                LogManager.SetMinLogLevel(loggingConfig.MinLogLevel);
                LogManager.SetEnabledCategories(loggingConfig.GetEnabledCategories());
                LogManager.Info(LogCategory.System, $"[LogManagerController] 已应用配置：{loggingConfig.ConfigId}");
            }
            else
            {
                LogManager.Warning(LogCategory.System, "[LogManagerController] 未设置 LoggingConfig，使用默认配置");
                LogManager.SetEnabledCategories(LogCategory.All);
                LogManager.SetMinLogLevel(LogLevel.Info);
            }
        }

        #region 快捷操作

        [ContextMenu("预设/调试全部")]
        public void PresetDebugAll()
        {
            if (loggingConfig != null)
            {
                loggingConfig.ApplyPreset(LoggingConfig.LogPreset.DebugAll);
                ApplySettings();
            }
        }

        [ContextMenu("预设/核心系统")]
        public void PresetCoreOnly()
        {
            if (loggingConfig != null)
            {
                loggingConfig.ApplyPreset(LoggingConfig.LogPreset.CoreOnly);
                ApplySettings();
            }
        }

        [ContextMenu("预设/仅重要")]
        public void PresetImportantOnly()
        {
            if (loggingConfig != null)
            {
                loggingConfig.ApplyPreset(LoggingConfig.LogPreset.ImportantOnly);
                ApplySettings();
            }
        }

        [ContextMenu("预设/静默")]
        public void PresetSilent()
        {
            if (loggingConfig != null)
            {
                loggingConfig.ApplyPreset(LoggingConfig.LogPreset.Silent);
                ApplySettings();
            }
        }

        [ContextMenu("打印当前配置")]
        public void PrintConfig()
        {
            LogManager.PrintCurrentConfig();
        }

        #endregion
    }
}


