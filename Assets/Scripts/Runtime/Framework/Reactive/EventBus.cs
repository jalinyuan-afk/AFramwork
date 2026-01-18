using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace TradeGame.Runtime.Framework
{
    /// <summary>
    /// 事件总线接口：基于UniRx的发布-订阅模式
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 发布事件
        /// </summary>
        void Publish<TEvent>(TEvent eventData) where TEvent : class;

        /// <summary>
        /// 订阅事件（返回可销毁的订阅）
        /// </summary>
        IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;

        /// <summary>
        /// 订阅事件，带过滤条件
        /// </summary>
        IDisposable Subscribe<TEvent>(Action<TEvent> handler, Func<TEvent, bool> predicate) where TEvent : class;

        /// <summary>
        /// 订阅事件，返回IObservable流
        /// </summary>
        IObservable<TEvent> AsObservable<TEvent>() where TEvent : class;
    }

    /// <summary>
    /// 全局事件总线实现
    /// </summary>
    public class EventBus : IEventBus, IDisposable
    {
        private readonly Dictionary<Type, object> _subjectMap = new();
        private readonly CompositeDisposable _disposables = new();

        /// <summary>
        /// 发布事件
        /// </summary>
        public void Publish<TEvent>(TEvent eventData) where TEvent : class
        {
            if (eventData == null) throw new ArgumentNullException(nameof(eventData));

            var subject = GetSubject<TEvent>();
            subject.OnNext(eventData);
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var subscription = AsObservable<TEvent>().Subscribe(handler);
            subscription.AddTo(_disposables);
            return subscription;
        }

        /// <summary>
        /// 订阅事件，带过滤条件
        /// </summary>
        public IDisposable Subscribe<TEvent>(Action<TEvent> handler, Func<TEvent, bool> predicate) where TEvent : class
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            var subscription = AsObservable<TEvent>().Where(predicate).Subscribe(handler);
            subscription.AddTo(_disposables);
            return subscription;
        }

        /// <summary>
        /// 获取事件的可观察流
        /// </summary>
        public IObservable<TEvent> AsObservable<TEvent>() where TEvent : class
        {
            var subject = GetSubject<TEvent>();
            return subject.AsObservable();
        }

        private ISubject<TEvent> GetSubject<TEvent>() where TEvent : class
        {
            var type = typeof(TEvent);
            if (!_subjectMap.TryGetValue(type, out var subjectObj))
            {
                var subject = new Subject<TEvent>();
                subjectObj = subject;
                _subjectMap[type] = subjectObj;
                subject.AddTo(_disposables);
            }

            return (ISubject<TEvent>)subjectObj;
        }

        /// <summary>
        /// 清理所有订阅
        /// </summary>
        public void Dispose()
        {
            _disposables?.Dispose();
            _subjectMap.Clear();
        }
    }

    /// <summary>
    /// 游戏事件基类（可继承用于强类型事件）
    /// </summary>
    public abstract class GameEvent
    {
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public object Sender { get; set; }
    }

    /// <summary>
    /// 带数据的事件
    /// </summary>
    public class GameEvent<TData> : GameEvent
    {
        public TData Data { get; }

        public GameEvent(TData data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// 事件总线扩展方法
    /// </summary>
    public static class EventBusExtensions
    {
        /// <summary>
        /// 发布带数据的事件
        /// </summary>
        public static void Publish<TData>(this IEventBus eventBus, TData data, object sender = null)
        {
            var gameEvent = new GameEvent<TData>(data) { Sender = sender };
            eventBus.Publish(gameEvent);
        }

        /// <summary>
        /// 订阅带数据的事件
        /// </summary>
        public static IDisposable Subscribe<TData>(this IEventBus eventBus, Action<GameEvent<TData>> handler)
        {
            return eventBus.Subscribe<GameEvent<TData>>(handler);
        }

        /// <summary>
        /// 订阅带数据的事件，带过滤条件
        /// </summary>
        public static IDisposable Subscribe<TData>(this IEventBus eventBus, Action<GameEvent<TData>> handler, Func<GameEvent<TData>, bool> predicate)
        {
            return eventBus.Subscribe(handler, predicate);
        }

        /// <summary>
        /// 订阅事件，自动解包数据
        /// </summary>
        public static IDisposable Subscribe<TData>(this IEventBus eventBus, Action<TData> handler)
        {
            return eventBus.Subscribe<GameEvent<TData>>(e => handler(e.Data));
        }
    }
}