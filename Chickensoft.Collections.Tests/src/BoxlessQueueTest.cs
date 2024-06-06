namespace Chickensoft.Collections.Tests;

using System.Collections.Generic;
using Chickensoft.Collections;
using Shouldly;
using Xunit;

public class BoxlessQueueTests {
  public readonly record struct ValueA;
  public readonly record struct ValueB;

  public class TestValueHandler : IBoxlessValueHandler {
    public List<object> Values { get; } = [];
    public void HandleValue<TValue>(in TValue value) where TValue : struct =>
      Values.Add(value);
  }

  [Fact]
  public void Initializes() {
    var handler = new TestValueHandler();
    var queue = new BoxlessQueue(handler);

    queue.Handler.ShouldBe(handler);
  }

  [Fact]
  public void EnqueueAndHandleValues() {
    var handler = new TestValueHandler();
    var queue = new BoxlessQueue(handler);

    var valueA = new ValueA();
    var valueA2 = new ValueA();
    var valueB = new ValueB();

    queue.Enqueue(valueA);
    queue.Enqueue(valueA2);
    queue.Enqueue(valueB);

    queue.HasValues.ShouldBeTrue();

    queue.Dequeue();
    queue.Dequeue();
    queue.Dequeue();

    queue.HasValues.ShouldBeFalse();
    queue.Dequeue();
    handler.Values.ShouldBe(new object[] { valueA, valueA2, valueB });
  }

  [Fact]
  public void ClearQueue() {
    var handler = new TestValueHandler();
    var queue = new BoxlessQueue(handler);

    queue.Enqueue(new ValueA());
    queue.Enqueue(new ValueB());

    queue.Clear();

    queue.HasValues.ShouldBeFalse();
  }
}
