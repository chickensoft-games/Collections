namespace Chickensoft.Collections;

using System;
using System.Collections.Generic;

/// <inheritdoc cref="BoxlessQueue{TConformance}" path="/summary" />
public sealed class BoxlessQueue : BoxlessQueue<ValueType> { }

/// <summary>
/// <para>
/// Queue that can store multiple types of structs without boxing them. It
/// does this by quietly creating a new queue whenever it sees a new value type,
/// drastically reducing heap allocations at the expense of warming up when
/// new types are seen for the first time (and the additional memory used by
/// the underlying collections). This is useful for high-throughput scenarios
/// where many different struct types may be enqueued and dequeued and the
/// queue is never very large at any one time.
/// </para>
/// <para>
/// These trade-off were chosen to allow the queue to drastically reduce
/// the amount of memory churn caused by boxing and unboxing values and/or
/// allocating lambdas to capture generic contexts.
/// </para>
/// <para>
/// Adapted from https://stackoverflow.com/a/6164880.
/// </para>
/// </summary>
/// <typeparam name="TConformance">
/// Type that all values in the queue must conform to. If you want to allow
/// any value type, use the non-generic version of <see cref="BoxlessQueue"/>.
/// </typeparam>
public class BoxlessQueue<TConformance> {
  private abstract class TypedValueQueueBase {
    public abstract void HandleValue<THandler>(in THandler handler)
        where THandler : struct, IBoxlessValueHandler<TConformance>;
    public abstract bool Peek<THandler>(in THandler handler)
        where THandler : struct, IBoxlessValueHandler<TConformance>;
    public abstract void Discard();
    public abstract void Clear();
  }

  private class TypedValueQueue<T> : TypedValueQueueBase
      where T : struct, TConformance {
    private Queue<T>? _queue;
    private T? _single;

    public void Enqueue(in T value) {
      if (_single.HasValue) {
        // only make a queue if we need to store more than one item
        // and we don't already have a queue
        _queue ??= [];
        _queue.Enqueue(value);

        return;
      }

      _single = value;
    }

    public override void HandleValue<THandler>(in THandler handler) {
      if (_single.HasValue) {
        var value = _single.Value;
        handler.HandleValue(in value);
        _single = null;
        return;
      }

      var item = _queue!.Dequeue();

      handler.HandleValue(item);
    }

    public override void Discard() {
      if (_single.HasValue) {
        _single = null;
        return;
      }

      _queue!.Dequeue();
    }

    public override bool Peek<THandler>(in THandler handler) {
      if (_single.HasValue) {
        var value = _single.Value;
        handler.HandleValue(in value);
        return true;
      }

      handler.HandleValue(_queue!.Peek());

      return true;
    }

    public override void Clear() {
      _single = null;
      _queue?.Clear();
    }
  }

  private readonly Queue<Type> _queueSelectorQueue = [];
  private readonly Dictionary<Type, TypedValueQueueBase> _queues = [];

  /// <summary>
  /// Total number of values in the queue.
  /// </summary>
  public int Count => _queueSelectorQueue.Count;

  /// <summary>
  /// Add a value to the queue without boxing it.
  /// </summary>
  /// <typeparam name="TValue">The type of value to enqueue.</typeparam>
  public void Enqueue<TValue>(in TValue value)
      where TValue : struct, TConformance {
    TypedValueQueue<TValue> queue;

    if (_queues.TryGetValue(typeof(TValue), out var existingQueue)) {
      queue = (TypedValueQueue<TValue>)existingQueue;
    }
    else {
      queue = new TypedValueQueue<TValue>();
      _queues[typeof(TValue)] = queue;
    }

    queue.Enqueue(in value);
    _queueSelectorQueue.Enqueue(typeof(TValue));
  }

  /// <summary>
  /// Returns whether the boxless queue has any values.
  /// </summary>
  public bool HasValues => _queueSelectorQueue.Count > 0;

  /// <summary>
  /// Handle the next value in the queue, if any. This will dequeue the next
  /// value and invoke the <paramref name="handler"/> with it.
  /// </summary>
  /// <param name="handler">
  /// Object that implements <see cref="IBoxlessValueHandler{TConformance}"/>.
  /// Whenever a value is dequeued, this object will be invoked with the
  /// value. This keeps structs from being boxed and unboxed when they are
  /// used, drastically reducing heap allocations.
  /// </param>
  public void Dequeue<THandler>(in THandler handler)
      where THandler : struct, IBoxlessValueHandler<TConformance> {
    if (!HasValues) { return; }
    var type = _queueSelectorQueue.Dequeue();
    _queues[type].HandleValue(in handler);
  }

  /// <summary>
  /// Discard a number of values from the queue without handling them.
  /// </summary>
  /// <param name="count">
  /// The number of values to discard. If this is greater than the number of
  /// values in the queue, all values will be discarded.
  /// </param>
  public void Discard(int count = 1) {
    if (count <= 0) { return; }

    while (count > 0 && HasValues) {
      var type = _queueSelectorQueue.Dequeue();
      _queues[type].Discard();
      count--;
    }
  }

  /// <summary>
  /// Peek at the current value in the queue. This invokes the handler with
  /// the value.
  /// </summary>
  /// <typeparam name="THandler">
  /// Type of the handler that will receive the peeked value. Must implement
  /// <see cref="IBoxlessValueHandler{TConformance}"/>.
  /// </typeparam>
  /// <param name="handler">
  /// Object that implements <see cref="IBoxlessValueHandler{TConformance}"/>.
  /// Whenever a value is peeked, this object will be invoked with the value.
  /// </param>
  public bool Peek<THandler>(in THandler handler)
      where THandler : struct, IBoxlessValueHandler<TConformance> {
    if (!HasValues) { return false; }

    var type = _queueSelectorQueue.Peek();

    return _queues[type].Peek(in handler);
  }

  /// <summary>Clear all values from the queue.</summary>
  public void Clear() {
    _queueSelectorQueue.Clear();

    foreach (var queue in _queues.Values) {
      queue.Clear();
    }
  }
}
