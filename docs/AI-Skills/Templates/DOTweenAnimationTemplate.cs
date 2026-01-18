using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;

namespace AFramework.Templates
{
    /// <summary>
    /// DOTween 动画模板
    /// 
    /// 功能：
    /// - Transform 动画（移动、旋转、缩放）
    /// - UI 动画（Slider、Image、CanvasGroup）
    /// - Tweener 管理（Kill、Pause、Play）
    /// - 链式调用（SetEase、SetDelay、OnComplete）
    /// - 转换为 UniTask
    /// </summary>
    public class DOTweenAnimationTemplate : MonoBehaviour
    {
        #region Transform 动画

        /// <summary>
        /// 移动动画
        /// </summary>
        public void MoveAnimation()
        {
            // 移动到目标位置
            transform.DOMove(new Vector3(0, 5, 0), 1f)
                .SetEase(Ease.OutCubic);

            // 移动到相对位置
            transform.DOMoveX(10f, 1f);
            transform.DOMoveY(5f, 1f);

            // 局部坐标移动
            transform.DOLocalMove(new Vector3(0, 2, 0), 1f);
        }

        /// <summary>
        /// 旋转动画
        /// </summary>
        public void RotateAnimation()
        {
            // 旋转到目标角度
            transform.DORotate(new Vector3(0, 180, 0), 1f);

            // 局部旋转
            transform.DOLocalRotate(new Vector3(0, 90, 0), 1f);
        }

        /// <summary>
        /// 缩放动画
        /// </summary>
        public void ScaleAnimation()
        {
            // 缩放到目标大小
            transform.DOScale(new Vector3(2, 2, 2), 1f);

            // 缩放到统一大小
            transform.DOScale(1.5f, 1f);

            // 缩放单个轴
            transform.DOScaleX(2f, 1f);
        }

        #endregion

        #region UI 动画

        [SerializeField] private Slider slider;
        [SerializeField] private Image image;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI text;

        private Tweener _sliderTweener;
        private Tweener _fadeTweener;

        /// <summary>
        /// Slider 值动画（重要：避免冲突）
        /// </summary>
        public void SliderAnimation(float targetValue)
        {
            // 重要：先 Kill 当前动画，避免冲突
            _sliderTweener?.Kill();

            // 创建新动画
            _sliderTweener = slider.DOValue(targetValue, 0.3f)
                .SetEase(Ease.OutCubic);
        }

        /// <summary>
        /// Image 颜色动画
        /// </summary>
        public void ImageColorAnimation()
        {
            image.DOColor(Color.red, 1f);

            // 渐变到透明
            image.DOFade(0f, 0.5f);
        }

        /// <summary>
        /// CanvasGroup 淡入淡出
        /// </summary>
        public void FadeAnimation()
        {
            // 淡入
            _fadeTweener?.Kill();
            _fadeTweener = canvasGroup.DOFade(1f, 0.5f);

            // 淡出
            // _fadeTweener?.Kill();
            // _fadeTweener = canvasGroup.DOFade(0f, 0.5f);
        }

        /// <summary>
        /// Text 文本动画
        /// </summary>
        public void TextAnimation()
        {
            // 逐字显示
            text.DOText("Hello World", 2f);

            // 追加文本
            text.DOText("...", 1f, true, ScrambleMode.None);
        }

        #endregion

        #region Tweener 管理

        private Tweener _moveTweener;

        /// <summary>
        /// 保存 Tweener 引用并控制
        /// </summary>
        public void TweenerControlExample()
        {
            // 创建并保存引用
            _moveTweener = transform.DOMove(Vector3.up * 5, 2f);

            // 暂停
            _moveTweener.Pause();

            // 继续
            _moveTweener.Play();

            // 重启
            _moveTweener.Restart();

            // 杀死
            _moveTweener.Kill();
        }

        #endregion

        #region 链式调用

        /// <summary>
        /// 链式设置参数
        /// </summary>
        public void ChainedCallsExample()
        {
            transform.DOMove(new Vector3(0, 5, 0), 1f)
                .SetEase(Ease.OutCubic)           // 缓动曲线
                .SetDelay(0.5f)                   // 延迟开始
                .SetLoops(2, LoopType.Yoyo)       // 循环次数和类型
                .OnStart(() => Debug.Log("开始"))  // 开始回调
                .OnUpdate(() => Debug.Log("更新")) // 更新回调
                .OnComplete(() => Debug.Log("完成")); // 完成回调
        }

        /// <summary>
        /// 常用缓动曲线
        /// </summary>
        public void EaseTypesExample()
        {
            // 线性
            transform.DOMove(Vector3.zero, 1f).SetEase(Ease.Linear);

            // 缓入缓出
            transform.DOMove(Vector3.zero, 1f).SetEase(Ease.InOutQuad);
            transform.DOMove(Vector3.zero, 1f).SetEase(Ease.OutCubic);

            // 弹性
            transform.DOMove(Vector3.zero, 1f).SetEase(Ease.OutElastic);

            // 弹跳
            transform.DOMove(Vector3.zero, 1f).SetEase(Ease.OutBounce);
        }

        #endregion

        #region 序列动画

        /// <summary>
        /// Sequence - 顺序执行动画
        /// </summary>
        public void SequenceExample()
        {
            Sequence sequence = DOTween.Sequence();

            sequence.Append(transform.DOMoveY(2f, 1f));      // 第1个动画
            sequence.Append(transform.DORotate(Vector3.up * 180, 1f)); // 第2个动画
            sequence.Append(transform.DOScale(2f, 1f));      // 第3个动画
            sequence.OnComplete(() => Debug.Log("序列完成"));
        }

        /// <summary>
        /// 并行动画（Join）
        /// </summary>
        public void JoinExample()
        {
            Sequence sequence = DOTween.Sequence();

            sequence.Append(transform.DOMoveY(2f, 1f));
            sequence.Join(transform.DORotate(Vector3.up * 180, 1f)); // 同时执行
            sequence.Join(transform.DOScale(2f, 1f));                // 同时执行
        }

        /// <summary>
        /// 插入动画（Insert）
        /// </summary>
        public void InsertExample()
        {
            Sequence sequence = DOTween.Sequence();

            sequence.Append(transform.DOMoveY(2f, 2f));
            sequence.Insert(1f, transform.DORotate(Vector3.up * 180, 1f)); // 在1秒时开始
        }

        #endregion

        #region 转换为 UniTask

        /// <summary>
        /// 异步等待动画完成
        /// </summary>
        public async UniTask AsyncAnimationExample()
        {
            // 等待移动完成
            await transform.DOMove(new Vector3(0, 5, 0), 1f).ToUniTask();

            LogManager.Log("移动完成", LogCategory.UI);

            // 等待缩放完成
            await transform.DOScale(2f, 1f).ToUniTask();

            LogManager.Log("缩放完成", LogCategory.UI);
        }

        /// <summary>
        /// 淡入淡出（异步）
        /// </summary>
        public async UniTask FadeInOutAsync()
        {
            // 淡入
            _fadeTweener?.Kill();
            _fadeTweener = canvasGroup.DOFade(1f, 0.5f);
            await _fadeTweener.ToUniTask();

            // 等待
            await UniTask.Delay(1000);

            // 淡出
            _fadeTweener?.Kill();
            _fadeTweener = canvasGroup.DOFade(0f, 0.5f);
            await _fadeTweener.ToUniTask();
        }

        #endregion

        #region 特殊效果

        /// <summary>
        /// 震动效果
        /// </summary>
        public void ShakeEffect()
        {
            // 震动位置
            transform.DOShakePosition(0.5f, strength: 1f, vibrato: 10);

            // 震动旋转
            transform.DOShakeRotation(0.5f, strength: 90f);

            // 震动缩放
            transform.DOShakeScale(0.5f, strength: 0.5f);
        }

        /// <summary>
        /// 打孔效果（Punch）
        /// </summary>
        public void PunchEffect()
        {
            // 打孔位置
            transform.DOPunchPosition(Vector3.up, 0.5f, vibrato: 10);

            // 打孔旋转
            transform.DOPunchRotation(new Vector3(0, 180, 0), 0.5f);

            // 打孔缩放
            transform.DOPunchScale(Vector3.one * 0.5f, 0.5f);
        }

        /// <summary>
        /// 路径动画
        /// </summary>
        public void PathAnimation()
        {
            Vector3[] path = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(2, 2, 0),
                new Vector3(4, 0, 0),
                new Vector3(6, 2, 0)
            };

            transform.DOPath(path, 3f, PathType.CatmullRom)
                .SetEase(Ease.Linear);
        }

        #endregion

        #region 全局设置

        void Awake()
        {
            // DOTween 全局设置（在游戏启动时调用一次）
            DOTween.Init(recycleAllByDefault: true, useSafeMode: true);
            DOTween.defaultEaseType = Ease.OutCubic;
        }

        #endregion

        #region 生命周期

        void OnDestroy()
        {
            // 清理所有 Tweener
            _sliderTweener?.Kill();
            _fadeTweener?.Kill();
            _moveTweener?.Kill();

            // 或者杀死所有动画
            DOTween.Kill(transform);
        }

        #endregion
    }
}
