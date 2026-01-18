using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TradeGame.Runtime
{
    /// <summary>
    /// ScriptableObject配置基类
    /// </summary>
    public abstract class GameConfig : ScriptableObject
    {    /// 所有游戏配置都应继承此类
         /// <summary>
         /// 配置ID，用于唯一标识配置
         /// </summary>
        [SerializeField]
        private string _configId;

        /// <summary>
        /// 配置描述
        /// </summary>
        [SerializeField, TextArea(2, 5)]
        private string _description;

        public string ConfigId => _configId;
        public string Description => _description;

        /// <summary>
        /// 配置加载后的初始化
        /// </summary>
        public virtual void OnConfigLoaded() { }

        /// <summary>
        /// 配置验证
        /// </summary>
        public virtual bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(_configId))
            {
                errorMessage = "ConfigId cannot be empty";
                return false;
            }

            return true;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            // 自动补全 ConfigId，避免新建 .asset 忘记填写导致加载失败。
            if (string.IsNullOrEmpty(_configId))
            {
                // 用资源名做默认 ID（如需全局唯一，可改成 GUID 或加前缀）。
                _configId = name;
                EditorUtility.SetDirty(this);
            }

            // 编辑器下自动验证
            if (!Validate(out string error))
            {
                Debug.LogWarning($"Config validation failed: {error}", this);
            }
        }
#endif
    }
}
