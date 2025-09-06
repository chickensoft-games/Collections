namespace Chickensoft.Collections.Tests;

using System.Collections.Generic;
using Chickensoft.Collections;
using Shouldly;
using Xunit;

public class BoxlessQueueTests {
  public readonly record struct ValueA(int Id);
  public readonly record struct ValueB(int Id);
  public readonly record struct ValueC(int Id);

  public readonly struct TestValueHandler : IBoxlessValueHandler {
    public List<object> Values { get; }

    public TestValueHandler() { Values = []; }

    public readonly void HandleValue<TValue>(in TValue value)
        where TValue : struct => Values.Add(value);
  }

  [Fact]
  public void Initializes() {
    var queue = new BoxlessQueue();

    queue.ShouldBeOfType<BoxlessQueue>();
  }

  [Fact]
  public void EnqueueAndHandleValues() {
    var handler = new TestValueHandler();
    var queue = new BoxlessQueue();

    var valueA = new ValueA();
    var valueA2 = new ValueA();
    var valueB = new ValueB();

    queue.Count.ShouldBe(0);

    queue.Enqueue(valueA);

    queue.Count.ShouldBe(1);

    queue.Enqueue(valueA2);
    queue.Enqueue(valueB);

    queue.Count.ShouldBe(3);

    queue.HasValues.ShouldBeTrue();

    queue.Dequeue(in handler);

    queue.Count.ShouldBe(2);

    queue.Dequeue(in handler);

    queue.Count.ShouldBe(1);

    queue.Dequeue(in handler);

    queue.HasValues.ShouldBeFalse();
    queue.Dequeue(in handler);
    handler.Values.ShouldBe(new object[] { valueA, valueA2, valueB });
  }

  [Fact]
  public void ClearQueue() {
    var handler = new TestValueHandler();
    var queue = new BoxlessQueue();

    queue.Enqueue(new ValueA());
    queue.Enqueue(new ValueB());

    queue.Clear();

    queue.HasValues.ShouldBeFalse();
  }

  [Fact]
  public void Peeks() {
    var handler = new TestValueHandler();
    var queue = new BoxlessQueue();

    var valueA = new ValueA();
    var valueB = new ValueB();
    var valueC = new ValueC();

    var valueD = new ValueA(1);
    var valueE = new ValueB(2);
    var valueF = new ValueC(3);

    queue.Enqueue(valueA);
    queue.Enqueue(valueB);
    queue.Enqueue(valueC);
    queue.Enqueue(valueD);
    queue.Enqueue(valueE);
    queue.Enqueue(valueF);

    queue.Peek(in handler).ShouldBeTrue();
    handler.Values.ShouldBe([valueA]);
    handler.Values.Clear();

    queue.Discard();

    queue.Peek(in handler).ShouldBeTrue();
    handler.Values.ShouldBe([valueB]);
    handler.Values.Clear();

    queue.Discard();

    queue.Peek(in handler).ShouldBeTrue();
    handler.Values.ShouldBe([valueC]);
    handler.Values.Clear();

    queue.Discard();

    queue.Peek(in handler).ShouldBeTrue();
    handler.Values.ShouldBe([valueD]);
    handler.Values.Clear();

    queue.Discard();

    queue.Peek(in handler).ShouldBeTrue();
    handler.Values.ShouldBe([valueE]);
    handler.Values.Clear();

    queue.Discard();

    queue.Peek(in handler).ShouldBeTrue();
    handler.Values.ShouldBe([valueF]);
    handler.Values.Clear();

    queue.Discard();

    queue.Peek(in handler).ShouldBeFalse();
    handler.Values.ShouldBeEmpty();

    queue.Count.ShouldBe(0);

    queue.Clear();

    queue.Count.ShouldBe(0);
  }

  [Fact]
  public void CannotDiscardOutOfRange() {
    var queue = new BoxlessQueue();

    queue.Enqueue(new ValueA());
    queue.Enqueue(new ValueB());

    queue.Discard(-1);

    queue.Count.ShouldBe(2);

    queue.Discard(3);

    queue.Count.ShouldBe(0);
  }
}
