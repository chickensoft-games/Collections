namespace Chickensoft.Collections.Reactive;

using System.Collections.Generic;

/// <summary>
/// A broadcast is a value type that is given an opportunity to invoke
/// callbacks on a binding when it is processed.
/// </summary>
public interface IBroadcast {
  /// <summary>
  /// Invoke any desired callbacks on the provided binding.
  /// </summary>
  /// <param name="binding">The binding to invoke callbacks on.</param>
  void Invoke(SyncBinding binding);
}

/// <summary>
/// <para>
/// In ReactiveX (Rx) terminology, this is a "PublishSubject" that is hot
/// (discards values when there are no listeners), serialized (defers mutations
/// and announcements while already processing to protect against reentry),
/// and is fully synchronous (announcements are made on the same call stack as
/// the broadcast). Bindings are invoked in the order that they are added.
/// </para>
/// <para>
/// Because this is fully synchronous and immediate, value types are given
/// to bindings without boxing them, enabling the design of convenient API's
/// which leverage ephemeral structs for the sole purpose of carrying data
/// in a nice, tidy package. This is a generalization of the bindings first
/// seen in Chickensoft.LogicBlocks.
/// </para>
/// <para>
/// This implementation guarantees atomic operations that can invoke multiple
/// types of callbacks on a single binding before doing the same on the next
/// binding, and so on. These atomic operations are declared as value types
/// that implement <see cref="IBroadcast" />.
/// </para>
/// <para>
/// Errors encountered from executing binding handlers are immediate and halt
/// processing.
/// </para>
/// </summary>
public sealed class SyncSubject {
  internal readonly struct BroadcastPassthrough :
      IBoxlessValueHandler<IBroadcast> {
    public readonly SyncSubject Target { get; }

    public BroadcastPassthrough(SyncSubject target) {
      Target = target;
    }
    public readonly void HandleValue<TValue>(in TValue value)
        where TValue : struct, IBroadcast => Target.HandleValue(in value);
  }

  internal enum OP_TYPE {
    ADD_BINDING,
    REMOVE_BINDING,
    CLEAR_BINDINGS,
    BROADCAST,
  }

  internal record struct OPERATION(
    OP_TYPE Operation,
    SyncBinding? Binding = null
  );

  private readonly LinkedHashSet<SyncBinding> _bindings = [];
  private readonly BoxlessQueue<IBroadcast> _broadcasts = new();
  private readonly Queue<OPERATION> _pendingOperations = [];
  private bool _isProcessing = false;

  /// <summary>
  /// Adds a binding to this subject. If the binding is currently processing
  /// other operations, this is deferred until after the current operations
  /// are completed.
  /// </summary>
  /// <param name="binding">The binding to add.</param>
  public void AddBinding(SyncBinding binding) {
    _pendingOperations.Enqueue(
      new OPERATION(OP_TYPE.ADD_BINDING, Binding: binding)
    );

    Process();
  }

  /// <summary>
  /// Removes a binding from this subject. If the binding is currently
  /// processing other operations, this is deferred until after the current
  /// operations are completed.
  /// </summary>
  /// <param name="binding">The binding to remove.</param>
  public void RemoveBinding(SyncBinding binding) {
    _pendingOperations.Enqueue(
      new OPERATION(OP_TYPE.REMOVE_BINDING, Binding: binding)
    );

    Process();
  }

  /// <summary>
  /// Removes all bindings from this subject. If any bindings are currently
  /// processing other operations, this is deferred until after the current
  /// operations are completed.
  /// </summary>
  public void ClearBindings() {
    _pendingOperations.Enqueue(new OPERATION(OP_TYPE.CLEAR_BINDINGS));

    Process();
  }

  /// <summary>
  /// <para>
  /// Publishes a broadcast to all current bindings. If already processing,
  /// this is deferred until after the current operations are completed.
  /// </para>
  /// <para>
  /// Broadcasts are value types which implement <see cref="IBroadcast" />.
  /// When a broadcast is processed, the subject will invoke the broadcast's
  /// <see cref="IBroadcast.Invoke(SyncBinding)" /> method for each binding,
  /// allowing the broadcast to invoke as many different callbacks on each
  /// binding as it likes. Broadcasts are queued without boxing for memory
  /// efficiency.
  /// </para>
  /// </summary>
  /// <typeparam name="TBroadcast">The type of broadcast to send. Must be a
  /// struct that implements <see cref="IBroadcast" />.</typeparam>
  /// <param name="broadcast">The broadcast instance to send.</param>
  public void Broadcast<TBroadcast>(in TBroadcast broadcast)
      where TBroadcast : struct, IBroadcast {
    _broadcasts.Enqueue(in broadcast);
    _pendingOperations.Enqueue(new OPERATION(OP_TYPE.BROADCAST));

    Process();
  }

  private void Process() {
    if (_isProcessing) { return; }

    _isProcessing = true;

    try {
      while (_pendingOperations.Count > 0) {
        var op = _pendingOperations.Dequeue();

        switch (op.Operation) {
          case OP_TYPE.ADD_BINDING:
            _bindings.Add(op.Binding!);
            break;
          case OP_TYPE.REMOVE_BINDING:
            _bindings.Remove(op.Binding!);
            break;
          case OP_TYPE.CLEAR_BINDINGS:
            _bindings.Clear();
            break;
          case OP_TYPE.BROADCAST:
            Broadcast();
            break;
        }
      }
    }
    finally {
      _isProcessing = false;
    }
  }

  private void Broadcast() {
    if (_bindings.Count == 0) {
      // if a broadcast is made and no one is around to hear it...
      // it isn't ever heard :P
      _broadcasts.Discard();
      return;
    }

    var passthrough = new BroadcastPassthrough(this);

    _broadcasts.Dequeue(in passthrough);
  }

  private void HandleValue<TBroadcast>(in TBroadcast broadcast)
      where TBroadcast : struct, IBroadcast {
    foreach (var binding in _bindings) {
      broadcast.Invoke(binding);
    }
  }
}
