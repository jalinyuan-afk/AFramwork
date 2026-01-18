using System;
using UniRx;
using UnityEngine;

namespace AFramework.Templates
{
    /// <summary>
    /// UniRx Observable 模板
    /// 
    /// 功能：
    /// - 定时器（Timer、Interval）
    /// - 事件流（EveryUpdate、EveryFixedUpdate）
    /// - 条件过滤（Where）
    /// - 订阅管理（CompositeDisposable）
    /// - Subject 事件总线
    /// </summary>
    public class UniRxObservableTemplate : MonoBehaviour
    {
        #region 订阅管理

        private CompositeDisposable _disposables = new CompositeDisposable();

        #endregion

        #region 定时器

        /// <summary>
        /// 延迟执行（一次性）
        /// </summary>
        void TimerExample()
        {
            Observable.Timer(TimeSpan.FromSeconds(2))
                .Subscribe(_ =>
                {
                    LogManager.Log("2秒后执行", LogCategory.Framework);
                })
                .AddTo(_disposables);
        }

        /// <summary>
        /// 间隔执行（重复）
        /// </summary>
        void IntervalExample()
        {
            Observable.Interval(TimeSpan.FromSeconds(1))
                .Subscribe(count =>
                {
                    LogManager.Log($"每1秒执行一次，当前计数: {count}", LogCategory.Framework);
                })
                .AddTo(_disposables);
        }

        /// <summary>
        /// 初始延迟 + 间隔执行
        /// </summary>
        void TimerWithIntervalExample()
        {
            Observable.Timer(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1))
                .Subscribe(count =>
                {
                    LogManager.Log($"3秒后开始，每1秒执行: {count}", LogCategory.Framework);
                })
                .AddTo(_disposables);
        }

        #endregion

        #region 帧更新

        /// <summary>
        /// 每帧更新
        /// </summary>
        void EveryUpdateExample()
        {
            Observable.EveryUpdate()
                .Subscribe(_ =>
                {
                    // 每帧执行的逻辑
                    UpdateLogic();
                })
                .AddTo(_disposables);
        }

        /// <summary>
        /// 每物理帧更新
        /// </summary>
        void EveryFixedUpdateExample()
        {
            Observable.EveryFixedUpdate()
                .Subscribe(_ =>
                {
                    // 每物理帧执行的逻辑
                    PhysicsLogic();
                })
                .AddTo(_disposables);
        }

        /// <summary>
        /// 每帧后期更新
        /// </summary>
        void EveryLateUpdateExample()
        {
            Observable.EveryLateUpdate()
                .Subscribe(_ =>
                {
                    // 每帧后期执行的逻辑
                    LateUpdateLogic();
                })
                .AddTo(_disposables);
        }

        #endregion

        #region 条件过滤

        /// <summary>
        /// 监听按键输入（Where 过滤）
        /// </summary>
        void InputObservableExample()
        {
            this.UpdateAsObservable()
                .Where(_ => Input.GetKeyDown(KeyCode.Space))
                .Subscribe(_ =>
                {
                    LogManager.Log("按下空格键", LogCategory.Gameplay);
                    Jump();
                })
                .AddTo(_disposables);
        }

        /// <summary>
        /// 监听鼠标点击
        /// </summary>
        void MouseClickObservable()
        {
            this.UpdateAsObservable()
                .Where(_ => Input.GetMouseButtonDown(0))
                .Subscribe(_ =>
                {
                    LogManager.Log("鼠标左键点击", LogCategory.UI);
                })
                .AddTo(_disposables);
        }

        /// <summary>
        /// 条件触发（复杂条件）
        /// </summary>
        void ConditionalObservable()
        {
            this.UpdateAsObservable()
                .Where(_ => Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.LeftShift))
                .Subscribe(_ =>
                {
                    LogManager.Log("按下W键（未按住Shift）", LogCategory.Gameplay);
                })
                .AddTo(_disposables);
        }

        #endregion

        #region 操作符

        /// <summary>
        /// Take - 只取前 N 次
        /// </summary>
        void TakeExample()
        {
            Observable.Interval(TimeSpan.FromSeconds(1))
                .Take(5) // 只执行5次
                .Subscribe(
                    count => LogManager.Log($"执行: {count}", LogCategory.Framework),
                    () => LogManager.Log("完成", LogCategory.Framework)
                )
                .AddTo(_disposables);
        }

        /// <summary>
        /// Skip - 跳过前 N 次
        /// </summary>
        void SkipExample()
        {
            Observable.Interval(TimeSpan.FromSeconds(1))
                .Skip(3) // 跳过前3次
                .Subscribe(count =>
                {
                    LogManager.Log($"从第4次开始: {count}", LogCategory.Framework);
                })
                .AddTo(_disposables);
        }

        /// <summary>
        /// Throttle - 节流（去抖动）
        /// </summary>
        void ThrottleExample()
        {
            this.UpdateAsObservable()
                .Where(_ => Input.GetKeyDown(KeyCode.Space))
                .ThrottleFirst(TimeSpan.FromSeconds(1)) // 1秒内只触发一次
                .Subscribe(_ =>
                {
                    LogManager.Log("节流触发", LogCategory.Gameplay);
                })
                .AddTo(_disposables);
        }

        /// <summary>
        /// Delay - 延迟触发
        /// </summary>
        void DelayExample()
        {
            Observable.ReturnUnit()
                .Delay(TimeSpan.FromSeconds(2))
                .Subscribe(_ =>
                {
                    LogManager.Log("延迟2秒触发", LogCategory.Framework);
                })
                .AddTo(_disposables);
        }

        #endregion

        #region Subject 事件总线

        private Subject<int> _scoreSubject = new Subject<int>();

        /// <summary>
        /// Subject - 手动发送事件
        /// </summary>
        void SubjectExample()
        {
            // 订阅事件
            _scoreSubject
                .Subscribe(score =>
                {
                    LogManager.Log($"分数更新: {score}", LogCategory.Gameplay);
                })
                .AddTo(_disposables);

            // 发送事件
            _scoreSubject.OnNext(100);
            _scoreSubject.OnNext(200);
        }

        /// <summary>
        /// ReactiveProperty - 响应式属性
        /// </summary>
        private ReactiveProperty<int> _health = new ReactiveProperty<int>(100);

        void ReactivePropertyExample()
        {
            // 订阅属性变化
            _health
                .Subscribe(value =>
                {
                    LogManager.Log($"生命值: {value}", LogCategory.Gameplay);
                })
                .AddTo(_disposables);

            // 修改属性
            _health.Value = 80;
            _health.Value = 50;
        }

        #endregion

        #region 合并操作

        /// <summary>
        /// Merge - 合并多个流
        /// </summary>
        void MergeExample()
        {
            var stream1 = Observable.Interval(TimeSpan.FromSeconds(1));
            var stream2 = Observable.Interval(TimeSpan.FromSeconds(2));

            stream1.Merge(stream2)
                .Subscribe(value =>
                {
                    LogManager.Log($"合并流: {value}", LogCategory.Framework);
                })
                .AddTo(_disposables);
        }

        /// <summary>
        /// CombineLatest - 组合最新值
        /// </summary>
        void CombineLatestExample()
        {
            var stream1 = Observable.Interval(TimeSpan.FromSeconds(1));
            var stream2 = Observable.Interval(TimeSpan.FromSeconds(2));

            stream1.CombineLatest(stream2, (a, b) => $"A:{a}, B:{b}")
                .Subscribe(combined =>
                {
                    LogManager.Log($"组合值: {combined}", LogCategory.Framework);
                })
                .AddTo(_disposables);
        }

        #endregion

        #region 示例方法

        void Start()
        {
            // 示例：每2秒轮换提示
            Observable.Interval(TimeSpan.FromSeconds(2))
                .Subscribe(_ => RotateTip())
                .AddTo(_disposables);
        }

        private void UpdateLogic() { }
        private void PhysicsLogic() { }
        private void LateUpdateLogic() { }
        private void Jump() { }
        private void RotateTip() { }

        #endregion

        #region 生命周期

        void OnDestroy()
        {
            // 一次性清理所有订阅
            _disposables?.Dispose();

            // 清理 Subject
            _scoreSubject?.Dispose();
            _health?.Dispose();
        }

        #endregion
    }
}
