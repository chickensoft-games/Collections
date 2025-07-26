namespace Chickensoft.Collections.Reactive;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

/// <summary>
/// Synchronous, single-threaded subject that invokes bindings and can queue
/// mutations and subsequent invocations to the bindings while in the process
/// of invoking them (optimized for minimal heap allocations).
/// </summary>
public interface ISyncSubject {
  /// <summary>
  /// Adds a binding that will receive announcements from this subject.
  /// </summary>
  /// <param name="binding">Binding to add.</param>
  void AddBinding(SyncBinding binding);

  /// <summary>
  /// Removes a binding from this subject.
  /// </summary>
  /// <param name="binding">Binding to remove.</param>
  void RemoveBinding(SyncBinding binding);

  /// <summary>
  /// Adds a listener that will receive all announcements from this subject,
  /// regardless of the channel or type.
  /// </summary>
  /// <param name="listener">Listener to add.</param>
  void AddListener(ISyncListener listener);

  /// <summary>
  /// Removes a listener from this subject.
  /// </summary>
  /// <param name="listener">Listener to remove.</param>
  void RemoveListener(ISyncListener listener);
}

/// <inheritdoc cref="ISyncSubject" path="/summary" />
public interface IMutableSyncSubject : ISyncSubject {
  /// <summary>
  /// Determines whether an exception thrown while invoking bindings should
  /// be thrown. Return true to throw the exception or false to suppress it.
  /// </summary>
  /// <returns>True if the exception should be thrown, false if it should be
  /// suppressed.</returns>

  Func<Exception, bool> HandleError { get; }

  /// <summary>
  /// Announce a value-type value to all bindings on the specified channel.
  /// </summary>
  /// <typeparam name="TValue">Value type.</typeparam>
  /// <param name="channel">Announcement channel.</param>
  /// <param name="value">Value.</param>
  void AnnounceValue<TValue>(int channel, TValue value) where TValue : struct;

  /// <summary>
  /// Announce a reference-type value to all bindings on the specified channel.
  /// </summary>
  /// <typeparam name="TValue">Reference type.</typeparam>
  /// <param name="channel">Announcement channel.</param>
  /// <param name="value">Value.</param>
  void AnnounceReference<TValue>(
    int channel, TValue value
  ) where TValue : class;

  /// <summary>
  /// Clears all the bindings and listeners from this subject (but does not
  /// stop any pending announcements or changes to listeners and bindings).
  /// </summary>
  void Clear();

  /// <summary>
  /// Stops execution by aborting any pending operations, such as scheduled
  /// changes to bindings and listeners, as well as any scheduled announcements.
  /// Aborting stops after the current invocation of bindings. All bindings
  /// for a given announcement will always be invoked atomically unless one
  /// throws an exception which <see cref="HandleError" /> returns true to throw
  /// for, in which case the exception is thrown.
  /// </summary>
  void Abort();
}

/// <inheritdoc cref="ISyncSubject" path="/summary" />
public sealed class SyncSubject : IMutableSyncSubject {
  internal enum SYNC_OPERATION {
    ADD_BINDING,
    REMOVE_BINDING,
    ADD_LISTENER,
    REMOVE_LISTENER,
    CLEAR,
    ANNOUNCE
  }

  internal record struct SyncOperation(
    SYNC_OPERATION Operation,
    SyncBinding? Binding = null,
    ISyncListener? Listener = null,
    Action? Callback = null
  );

  // listeners receive notifications of all values, unlike bindings. bindings
  // register callbacks for specific types of values on specific channels,
  // whereas listeners get informed of everything.
  internal LinkedHashSet<ISyncListener>? _listeners;
  internal LinkedHashSet<SyncBinding>? _bindings;
  internal readonly Queue<SyncOperation> _pendingOperations = [];
  private readonly SyncLock _sync = new();
  private bool _isAborted = false;

  /// <summary>
  /// Creates a new <see cref="SyncSubject" />.
  /// </summary>
  public SyncSubject() { }

  /// <inheritdoc />
  public Func<Exception, bool> HandleError { get; init; } =
    static (e) => true;

  /// <inheritdoc />
  public void AnnounceValue<TValue>(
    int channel, TValue value
  ) where TValue : struct {
    Exception? err = null;

    if (_sync.IsLocked && !_isAborted) {
      // only true if already invoking bindings

      // queue an invocation to occur later since we're already invoking
      //
      // note that this allocates a closure when we are already busy
      _pendingOperations.Enqueue(
        new SyncOperation(
          SYNC_OPERATION.ANNOUNCE,
          Binding: null,
          Callback: () => AnnounceValue(channel, value)
        )
      );

      return;
    }

    var shouldThrow = false;

    _sync.Lock();

    // listeners are notified first
    if (_listeners is not null) {
      foreach (var listener in _listeners /* struct enumerator */) {
        try {
          listener.OnValueType(channel, value);
        }
        catch (Exception e) {
          err = e;

          try {
            shouldThrow = HandleError(e);
          }
          catch (Exception e2) {
            // exception occurred while handling the original exception :P
            err = e2;
            shouldThrow = true;
          }

          if (shouldThrow) {
            _sync.Unlock();
            goto finishedWithException; // avoids breaking code coverage
          }
        }
      }
    }

    if (_bindings is not null) {
      foreach (var binding in _bindings /* struct enumerator */) {
        var callbacks = binding.GetValueTypeCallbacks<TValue>(channel);

        for (var i = 0; i < callbacks.Count; i++) {
          var callback = (ValueTypeCallback<TValue>)callbacks[i];
          try {
            callback(in value);
          }
          catch (Exception e) {
            err = e;

            try {
              shouldThrow = HandleError(e);
            }
            catch (Exception e2) {
              // exception occurred while handling the original exception :P
              err = e2;
              shouldThrow = true;
            }

            if (shouldThrow) {
              _sync.Unlock();
              goto finishedWithException; // avoids breaking code coverage
            }
          }
        }
      }
    }


    _sync.Unlock();

    if (_isAborted) {
      _pendingOperations.Clear();
    }
    else {
      ProcessPendingIfPossible();
    }

    return;

    finishedWithException:
    if (_isAborted) {
      _pendingOperations.Clear();
    }

    Throw(err!);
  }

  /// <inheritdoc />
  public void AnnounceReference<TValue>(
    int channel, TValue value
  ) where TValue : class {
    Exception? err = null;

    if (_sync.IsLocked && !_isAborted) {
      // only true if already invoking bindings

      // queue an invocation to occur later since we're already invoking
      //
      // note that this allocates a closure when we are already busy
      _pendingOperations.Enqueue(
        new SyncOperation(
          SYNC_OPERATION.ANNOUNCE,
          Binding: null,
          Callback: () => AnnounceReference(channel, value)
        )
      );

      return;
    }

    var shouldThrow = false;

    _sync.Lock();

    // listeners are notified first
    if (_listeners is not null) {
      foreach (var listener in _listeners /* struct enumerator */) {
        try {
          listener.OnReferenceType(channel, value);
        }
        catch (Exception e) {
          err = e;

          try {
            shouldThrow = HandleError(e);
          }
          catch (Exception e2) {
            // exception occurred while handling the original exception :P
            err = e2;
            shouldThrow = true;
          }

          if (shouldThrow) {
            _sync.Unlock();
            goto finishedWithException; // avoids breaking code coverage
          }
        }
      }
    }

    if (_bindings is not null) {
      foreach (var binding in _bindings /* struct enumerator */) {
        var callbacks = binding.GetRefTypeCallbacks(channel);

        for (var i = 0; i < callbacks.Count; i++) {
          var callback = callbacks[i];

          if (!callback.ShouldInvoke(value)) { continue; }

          try {
            callback.Invoke(value);
          }
          catch (Exception e) {
            err = e;

            try {
              shouldThrow = HandleError(e);
            }
            catch (Exception e2) {
              // exception occurred while handling the original exception :P
              err = e2;
              shouldThrow = true;
            }

            if (shouldThrow) {
              _sync.Unlock();
              goto finishedWithException; // avoids breaking code coverage
            }
          }
        }
      }
    }


    _sync.Unlock();

    if (_isAborted) {
      _pendingOperations.Clear();
    }
    else {
      ProcessPendingIfPossible();
    }

    return;

    finishedWithException:
    if (_isAborted) {
      _pendingOperations.Clear();
    }

    Throw(err!);
  }

  /// <inheritdoc />
  public void AddBinding(SyncBinding binding) {
    _pendingOperations.Enqueue(
      new SyncOperation(SYNC_OPERATION.ADD_BINDING, Binding: binding)
    );
    ProcessPendingIfPossible();
  }

  /// <inheritdoc />
  public void RemoveBinding(SyncBinding binding) {
    _pendingOperations.Enqueue(
      new SyncOperation(SYNC_OPERATION.REMOVE_BINDING, Binding: binding)
    );
    ProcessPendingIfPossible();
  }

  /// <inheritdoc />
  public void AddListener(ISyncListener listener) {
    _pendingOperations.Enqueue(
      new SyncOperation(SYNC_OPERATION.ADD_LISTENER, Listener: listener)
    );
    ProcessPendingIfPossible();
  }

  /// <inheritdoc />
  public void RemoveListener(ISyncListener listener) {
    _pendingOperations.Enqueue(
      new SyncOperation(SYNC_OPERATION.REMOVE_LISTENER, Listener: listener)
    );
    ProcessPendingIfPossible();
  }

  /// <inheritdoc />
  public void Clear() {
    _pendingOperations.Enqueue(
      new SyncOperation(SYNC_OPERATION.CLEAR)
    );

    ProcessPendingIfPossible();
  }

  /// <inheritdoc />
  public void Abort() {
    if (_sync.IsLocked) {
      _isAborted = true;
      return;
    }
  }

  [ExcludeFromCodeCoverage]
  private void Throw(Exception e) {
    ExceptionDispatchInfo.Capture(e).Throw(); // preserve original stack trace
  }

  private void ProcessPendingIfPossible() {
    if (_sync.IsLocked) {
      // can only mutate when not invoking bindings
      return;
    }

    while (_pendingOperations.Count > 0) {
      var operation = _pendingOperations.Dequeue();

      switch (operation.Operation) {
        case SYNC_OPERATION.ADD_BINDING:
          _bindings ??= [];
          _bindings.Add(operation.Binding!);
          break;
        case SYNC_OPERATION.REMOVE_BINDING:
          _bindings?.Remove(operation.Binding!);
          break;
        case SYNC_OPERATION.ADD_LISTENER:
          _listeners ??= [];
          _listeners.Add(operation.Listener!);
          break;
        case SYNC_OPERATION.REMOVE_LISTENER:
          _listeners?.Remove(operation.Listener!);
          break;
        case SYNC_OPERATION.CLEAR:
          _bindings?.Clear();
          _listeners?.Clear();
          break;
        case SYNC_OPERATION.ANNOUNCE:
          operation.Callback!.Invoke();
          return; // must leave loop, done everything we can for now
      }
    }
  }
}
