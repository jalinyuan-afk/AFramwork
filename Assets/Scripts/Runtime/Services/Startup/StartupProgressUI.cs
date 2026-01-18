using UnityEngine;
using UnityEngine.UI;
using TradeGame.Runtime.Framework;
using UniRx;
using TMPro;
using DG.Tweening;

namespace TradeGame.Runtime.Services.Startup
{
    /// <summary>
    /// 启动进度 UI 面板
    /// 展示如何订阅启动事件并更新 UI
    /// </summary>
    public class StartupProgressUI : MonoBehaviour
    {
        [Header("UI 组件")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI tipText;
        [SerializeField] private GameObject loadingSpinner;

        [Header("提示文本")]
        [SerializeField]
        private string[] loadingTips = new[]
        {
            "正在初始化游戏系统...",
            "正在加载游戏资源...",
            "正在连接服务器...",
            "即将进入游戏..."
        };

        private IEventBus _eventBus;
        private CompositeDisposable _disposables = new();
        private int _currentTipIndex = 0;
        private Tweener _progressTweener; // 进度条动画引用

        private void Start()
        {
            _eventBus = Bootstrapper.Resolve<IEventBus>();

            // 订阅启动进度事件
            _eventBus.Subscribe<GameStartupProgressEvent>(OnProgressUpdate)
                .AddTo(_disposables);

            // 订阅启动完成事件
            _eventBus.Subscribe<GameStartupCompleteEvent>(OnStartupComplete)
                .AddTo(_disposables);

            // 初始化 UI
            InitializeUI();

            // 每 2 秒切换一次提示文本
            Observable.Interval(System.TimeSpan.FromSeconds(2))
                .TakeUntilDestroy(this)
                .Subscribe(_ => RotateTip())
                .AddTo(_disposables);
        }

        private void InitializeUI()
        {
            if (progressBar != null)
            {
                progressBar.value = 0f;
            }

            if (progressText != null)
            {
                progressText.text = "0%";
            }

            if (statusText != null)
            {
                statusText.text = "正在启动游戏...";
            }

            if (loadingSpinner != null)
            {
                loadingSpinner.SetActive(true);
            }

            UpdateTipText();
        }

        private void OnProgressUpdate(GameStartupProgressEvent e)
        {
            // 更新进度条
            if (progressBar != null)
            {
                // 先停止之前的动画，避免冲突
                _progressTweener?.Kill();

                // 创建平滑过渡动画
                _progressTweener = DOTween.To(() => progressBar.value, x => progressBar.value = x, e.Progress, 0.3f)
                    .SetEase(Ease.OutCubic);
            }

            // 更新进度文本
            if (progressText != null)
            {
                progressText.text = $"{e.Progress:P0}";
            }

            // 更新状态文本
            if (statusText != null)
            {
                statusText.text = e.Message;
            }

            LogManager.Info(LogCategory.UI, $"[StartupUI] 更新进度: {e.Message} ({e.Progress:P0})");
        }

        private void OnStartupComplete(GameStartupCompleteEvent e)
        {
            if (e.Success)
            {
                if (statusText != null)
                {
                    statusText.text = "启动完成！";
                    statusText.color = Color.green;
                }

                if (loadingSpinner != null)
                {
                    loadingSpinner.SetActive(false);
                }

                // 延迟 1 秒后隐藏启动界面
                Observable.Timer(System.TimeSpan.FromSeconds(1))
                    .Subscribe(_ => HideStartupUI())
                    .AddTo(_disposables);

                LogManager.Info(LogCategory.UI, "[StartupUI] 游戏启动成功，准备进入主界面");
            }
            else
            {
                if (statusText != null)
                {
                    statusText.text = "启动失败，请重试";
                    statusText.color = Color.red;
                }

                if (loadingSpinner != null)
                {
                    loadingSpinner.SetActive(false);
                }

                LogManager.Error(LogCategory.UI, "[StartupUI] 游戏启动失败");
            }
        }

        private void RotateTip()
        {
            if (loadingTips == null || loadingTips.Length == 0) return;

            _currentTipIndex = (_currentTipIndex + 1) % loadingTips.Length;
            UpdateTipText();
        }

        private void UpdateTipText()
        {
            if (tipText != null && loadingTips != null && loadingTips.Length > 0)
            {
                tipText.text = loadingTips[_currentTipIndex];
            }
        }

        private void HideStartupUI()
        {
            // 停止进度条动画
            _progressTweener?.Kill();

            // 淡出动画
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0f, 0.5f)
                    .OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                gameObject.SetActive(false);
            }

            LogManager.Info(LogCategory.UI, "[StartupUI] 启动界面已隐藏");
        }

        private void OnDestroy()
        {
            _progressTweener?.Kill();
            _disposables?.Dispose();
        }
    }
}
