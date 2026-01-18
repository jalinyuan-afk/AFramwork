using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Cysharp.Threading.Tasks;

namespace AFramework.Templates
{
    /// <summary>
    /// EventBus 驱动 UI 模板
    /// 参考文件: Assets/Scripts/Runtime/Services/Startup/StartupProgressUI.cs
    /// 
    /// 功能：
    /// - 订阅事件更新 UI（EventBus）
    /// - 平滑动画（DOTween）
    /// - 定时器（UniRx）
    /// - 资源清理（CompositeDisposable）
    /// </summary>
    public class EventBusUITemplate : MonoBehaviour
    {
        #region UI 组件

        [Header("进度显示")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private TextMeshProUGUI progressText;

        [Header("提示文本")]
        [SerializeField] private TextMeshProUGUI tipText;
        [SerializeField]
        private List<string> loadingTips = new List<string>
        {
            "正在加载资源...",
            "即将完成...",
            "准备就绪..."
        };

        [Header("动画设置")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.3f;

        #endregion

        #region 依赖注入

        private IEventBus _eventBus;

        void Start()
        {
            _eventBus = Bootstrapper.Resolve<IEventBus>();

            // 订阅事件
            SubscribeEvents();

            // 启动定时器
            StartTipRotation();

            // 显示 UI
            ShowUI();
        }

        #endregion

        #region 事件订阅管理

        private CompositeDisposable _disposables = new CompositeDisposable();

        /// <summary>
        /// 订阅所有事件
        /// </summary>
        private void SubscribeEvents()
        {
            // 订阅进度事件
            _eventBus.Subscribe<TaskProgressEvent>(OnProgressUpdate)
                .AddTo(_disposables);

            // 订阅完成事件
            _eventBus.Subscribe<TaskCompleteEvent>(OnTaskComplete)
                .AddTo(_disposables);

            // 订阅错误事件
            _eventBus.Subscribe<TaskErrorEvent>(OnTaskError)
                .AddTo(_disposables);
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 处理进度更新事件
        /// </summary>
        private void OnProgressUpdate(TaskProgressEvent evt)
        {
            // 更新阶段文本
            phaseText.text = evt.CurrentPhase;

            // 更新进度文本
            progressText.text = $"{(evt.Progress * 100):F0}%";

            // 平滑更新进度条（重要：先 Kill 避免冲突）
            UpdateProgressBar(evt.Progress);

            LogManager.Log($"进度更新: {evt.CurrentPhase} - {evt.Progress:P0}", LogCategory.UI);
        }

        /// <summary>
        /// 处理完成事件
        /// </summary>
        private void OnTaskComplete(TaskCompleteEvent evt)
        {
            phaseText.text = "完成";
            progressText.text = "100%";

            // 延迟隐藏 UI
            HideUIAsync().Forget();
        }

        /// <summary>
        /// 处理错误事件
        /// </summary>
        private void OnTaskError(TaskErrorEvent evt)
        {
            phaseText.text = "错误";
            tipText.text = $"错误: {evt.ErrorMessage}";
            tipText.color = Color.red;

            LogManager.LogError($"任务错误: {evt.ErrorMessage}", LogCategory.Framework);
        }

        #endregion

        #region DOTween 动画管理

        private Tweener _progressTweener;
        private Tweener _fadeTweener;

        /// <summary>
        /// 更新进度条（平滑动画）
        /// </summary>
        private void UpdateProgressBar(float targetValue)
        {
            // 重要：先 Kill 当前动画，避免冲突
            _progressTweener?.Kill();

            // 创建新动画
            _progressTweener = DOTween.To(
                () => progressBar.value,
                x => progressBar.value = x,
                targetValue,
                0.3f
            ).SetEase(Ease.OutCubic);
        }

        /// <summary>
        /// 显示 UI（淡入）
        /// </summary>
        private void ShowUI()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(true);

            _fadeTweener?.Kill();
            _fadeTweener = canvasGroup.DOFade(1f, fadeInDuration);
        }

        /// <summary>
        /// 隐藏 UI（淡出）
        /// </summary>
        private async UniTaskVoid HideUIAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1));

            _fadeTweener?.Kill();
            _fadeTweener = canvasGroup.DOFade(0f, fadeOutDuration);

            await _fadeTweener.ToUniTask();
            canvasGroup.gameObject.SetActive(false);
        }

        #endregion

        #region UniRx 定时器

        private int _currentTipIndex = 0;

        /// <summary>
        /// 启动提示轮换定时器
        /// </summary>
        private void StartTipRotation()
        {
            // 每 2 秒轮换一次提示
            Observable.Interval(TimeSpan.FromSeconds(2))
                .Subscribe(_ => RotateTip())
                .AddTo(_disposables);
        }

        /// <summary>
        /// 轮换提示文本
        /// </summary>
        private void RotateTip()
        {
            if (loadingTips.Count == 0) return;

            _currentTipIndex = (_currentTipIndex + 1) % loadingTips.Count;
            tipText.text = loadingTips[_currentTipIndex];
        }

        #endregion

        #region 生命周期清理

        void OnDestroy()
        {
            // 清理所有订阅
            _disposables?.Dispose();

            // 清理所有动画
            _progressTweener?.Kill();
            _fadeTweener?.Kill();
        }

        #endregion
    }

    #region 事件定义

    public class TaskProgressEvent
    {
        public string CurrentPhase { get; set; }
        public float Progress { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class TaskCompleteEvent
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class TaskErrorEvent
    {
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }
    }

    #endregion
}
