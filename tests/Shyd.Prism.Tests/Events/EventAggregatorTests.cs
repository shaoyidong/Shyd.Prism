using Shyd.Prism.Events;
using System;
using System.Collections.Generic;
using Xunit;

namespace Shyd.Prism.Tests.Events;

/// <summary>
/// 事件聚合器：发布 / 订阅 / 退订 / 多订阅者 / 事件类型隔离。
/// </summary>
public class EventAggregatorTests
{
    private sealed class Subscribed : Exception
    {
    }

    [Fact]
    public void Publish_DeliversPayloadToSubscriber()
    {
        var aggregator = new EventAggregator();
        var received = new List<string>();

        using var subscription = aggregator.Subscribe<string>(received.Add);
        aggregator.Publish("hello");

        Assert.Equal(new[] { "hello" }, received);
    }

    [Fact]
    public void Publish_NotifiesAllSubscribersInOrder()
    {
        var aggregator = new EventAggregator();
        var calls = new List<string>();

        using var _ = aggregator.Subscribe<int>(_ => calls.Add("first"));
        using var __ = aggregator.Subscribe<int>(_ => calls.Add("second"));

        aggregator.Publish(42);

        Assert.Equal(new[] { "first", "second" }, calls);
    }

    [Fact]
    public void Dispose_StopsDelivery()
    {
        var aggregator = new EventAggregator();
        var count = 0;
        var subscription = aggregator.Subscribe<int>(_ => count++);

        aggregator.Publish(1);
        Assert.Equal(1, count);

        subscription.Dispose();
        aggregator.Publish(2);

        Assert.Equal(1, count);
    }

    [Fact]
    public void Publish_WithoutSubscribers_IsNoOp()
    {
        var aggregator = new EventAggregator();

        aggregator.Publish("nobody listens");
        aggregator.Publish(123);
    }

    [Fact]
    public void DifferentEventTypes_AreIsolated()
    {
        var aggregator = new EventAggregator();
        var strings = new List<string>();
        var ints = new List<int>();

        using var _ = aggregator.Subscribe<string>(strings.Add);
        using var __ = aggregator.Subscribe<int>(ints.Add);

        aggregator.Publish("text");
        aggregator.Publish(7);

        Assert.Equal(new[] { "text" }, strings);
        Assert.Equal(new[] { 7 }, ints);
    }

    [Fact]
    public void SubscriberException_PropagatesToPublisher()
    {
        var aggregator = new EventAggregator();
        using var _ = aggregator.Subscribe<int>(_ => throw new Subscribed());

        Assert.Throws<Subscribed>(() => aggregator.Publish(1));
    }
}
