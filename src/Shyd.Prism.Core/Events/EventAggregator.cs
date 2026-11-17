using System;
using System.Collections.Generic;

namespace Shyd.Prism.Events;

/// <summary>
/// 类型化事件聚合器（仿 Prism IEventAggregator 思想的轻量实现）：
/// 发布方与订阅方通过事件类型解耦，适合「协议层 → UI」这类反向通知。
/// <para>线程安全：可从任意线程 Subscribe / Publish；事件按快照顺序同步触发，
/// 订阅方自行决定是否封送 UI 线程；异常会直接抛给 Publish 调用方。</para>
/// </summary>
public interface IEventAggregator
{
    /// <summary>订阅指定类型事件；返回 <see cref="IDisposable"/>，Dispose 即退订。</summary>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler);

    /// <summary>发布一个事件，按订阅顺序同步通知所有订阅者。</summary>
    void Publish<TEvent>(TEvent evt);
}

/// <inheritdoc />
public sealed class EventAggregator : IEventAggregator
{
    private readonly Lock _gate = new();
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    /// <inheritdoc />
    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_gate)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var list))
            {
                _handlers[typeof(TEvent)] = list = new List<Delegate>();
            }

            list.Add(handler);
        }

        return new Subscription(this, typeof(TEvent), handler);
    }

    /// <inheritdoc />
    public void Publish<TEvent>(TEvent evt)
    {
        Action<TEvent>[] snapshot;
        lock (_gate)
        {
            snapshot = _handlers.TryGetValue(typeof(TEvent), out var list)
                ? list.OfType<Action<TEvent>>().ToArray()
                : Array.Empty<Action<TEvent>>();
        }

        foreach (var handler in snapshot)
        {
            handler(evt);
        }
    }

    private void Unsubscribe(Type eventType, Delegate handler)
    {
        lock (_gate)
        {
            if (_handlers.TryGetValue(eventType, out var list))
            {
                list.Remove(handler);
                if (list.Count == 0)
                {
                    _handlers.Remove(eventType);
                }
            }
        }
    }

    private sealed class Subscription(EventAggregator owner, Type eventType, Delegate handler) : IDisposable
    {
        public void Dispose() => owner.Unsubscribe(eventType, handler);
    }
}
