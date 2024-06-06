namespace Chickensoft.Collections;

using System;
using System.Collections.Generic;

/// <summary>
/// <para>
/// Queue that can store multiple types of structs without boxing them. It
/// does this by quietly creating a new queue whenever it sees a new value type,
/// drastically reducing heap allocations.
/// </para>
/// <para>
/// This is built around standard queues, so it takes advantage of the internal
/// capacity of the queue and all of its resizing functionality at the expense
/// of a little additional memory usage if many struct types are seen by the
/// queue. This trade-off allows the queue to drastically reduce
/// the amount of memory churn caused by boxing and unboxing values and/or
/// allocating lambdas to capture generic contexts.
/// </para>
/// <para>
/// Adapted from https://stackoverflow.com/a/6164880.
/// </para>
/// </summary>
/// <remarks>
/// Creates a new boxless queue that does not box or unbox values.
/// </remarks>
/// <param name="handler"><inheritdoc cref="Handler" path="/summary"/>
/// </param>
public class BoxlessQueue(IBoxlessValueHandler handler) {
  private abstract class TypedValueQueue {
    public abstract void HandleValue(IBoxlessValueHandler handler);
    public abstract void Clear();
  }

  private class TypedMessageQueue<T> : TypedValueQueue where T : struct {
    private readonly Queue<T> _queue = new();

    public void Enqueue(T message) => _queue.Enqueue(message);

    public override void HandleValue(IBoxlessValueHandler handler) =>
      handler.HandleValue(_queue.Dequeue());

    public override void Clear() => _queue.Clear();
  }

  /// <summary>
  /// Object that implements <see cref="IBoxlessValueHandler"/>. Whenever a
  /// value is dequeued, this object will be invoked with the value. This keeps
  /// structs from being boxed and unboxed when they are used, drastically
  /// reducing heap allocations.
  /// </summary>
  public IBoxlessValueHandler Handler { get; } = handler;
  private readonly Queue<Type> _queueSelectorQueue = new();
  private readonly Dictionary<Type, TypedValueQueue> _queues = [];

  /// <summary>
  /// Add a value to the queue without boxing it.
  /// </summary>
  /// <typeparam name="TValue">The type of value to enqueue.</typeparam>
  public void Enqueue<TValue>(TValue message) where TValue : struct {
    TypedMessageQueue<TValue> queue;

    if (!_queues.ContainsKey(typeof(TValue))) {
      queue = new TypedMessageQueue<TValue>();
      _queues[typeof(TValue)] = queue;
    }
    else {
      queue = (TypedMessageQueue<TValue>)_queues[typeof(TValue)];
    }

    queue.Enqueue(message);
    _queueSelectorQueue.Enqueue(typeof(TValue));
  }

  /// <summary>
  /// Returns whether the boxless queue has any values.
  /// </summary>
  public bool HasValues => _queueSelectorQueue.Count > 0;

  /// <summary>
  /// Handle the next value in the queue, if any. This will dequeue the next
  /// value and invoke the <see cref="Handler"/> with it.
  /// </summary>
  public void Dequeue() {
    if (!HasValues) { return; }
    var type = _queueSelectorQueue.Dequeue();
    _queues[type].HandleValue(Handler);
  }

  /// <summary>Clear all values from the queue.</summary>
  public void Clear() {
    _queueSelectorQueue.Clear();

    foreach (var queue in _queues.Values) {
      queue.Clear();
    }
  }
}
