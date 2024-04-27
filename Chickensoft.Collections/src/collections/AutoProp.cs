namespace Chickensoft.Collections;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a readonly value that can be observed for changes.
/// <br />
/// This is a lightweight observable object that uses events to publish
/// changes synchronously to event handlers. When you're done listening,
/// simply unsubscribe from the relevant events. All handlers are invoked the
/// way C# events are invoked: synchronously, on the same thread, and in order
/// of subscription (unless C# changes).
/// <br />
/// Event handlers are only invoked if the new value is not equal to the
/// previous value, as determined by the <see cref="Comparer"/>. If you don't
/// supply one, the default comparer for the type will be used.
/// <br />
/// Auto properties guarantee that handlers will be invoked for each distinct
/// value, in order. If you update the value from within a handler, the change
/// will be queued and processed after the current handler has finished. You
/// can always access the current value via <see cref="Value"/>.
/// <br />
/// <see cref="Changed"/> handlers will be invoked whenever a new value is
/// pushed.
/// <br />
/// <see cref="Sync"/> handlers will be invoked immediately when they are added,
/// as well as whenever a new value is published. This allows you to keep the
/// values in "sync" from the moment you're interested in observing them.
/// <br />
/// <see cref="Completed"/> handlers will be invoked when the property has
/// been marked as completed.
/// <br />
/// <see cref="Error"/> handlers will be invoked when an error is pushed to
/// the property. Errors can be pushed more than once and do not throw.
/// </summary>
/// <typeparam name="T">Type of the value to observe.</typeparam>
public interface IAutoProp<T> {
  /// <summary>
  /// Current value of the property. This is always the most up-to-date value
  /// at any given moment.
  /// </summary>
  T Value { get; }

  /// <summary>
  /// Equality comparer to use. Handlers will only be invoked if the comparer
  /// determines that the new value is not equal to the previous value.
  /// </summary>
  IEqualityComparer<T> Comparer { get; }

  /// <summary>
  /// Property change handler that is invoked whenever a new, distinct value
  /// is pushed.
  /// </summary>
  event Action<T>? Changed;

  /// <summary>
  /// Sync handler that is invoked immediately when it is added, as well as
  /// whenever a new, distinct value is pushed.
  /// </summary>
  event Action<T>? Sync;

  /// <summary>
  /// Completed handler that is invoked when the property has been marked as
  /// completed.
  /// </summary>
  event Action? Completed;

  /// <summary>
  /// Error handler that is invoked when an error is pushed to the property.
  /// </summary>
  event Action<Exception>? Error;
}

/// <summary>
/// Observable property that uses C# events to invoke handlers synchronously.
/// <br />
/// This is a lightweight observable object that uses events to publish
/// changes synchronously to event handlers. When you're done listening,
/// simply unsubscribe from the relevant events. All handlers are invoked the
/// way C# events are invoked: synchronously, on the same thread, and in order
/// of subscription (unless C# changes).
/// <br />
/// Event handlers are only invoked if the new value is not equal to the
/// previous value, as determined by the <see cref="Comparer"/>. If you don't
/// supply one, the default comparer for the type will be used.
/// <br />
/// Auto properties guarantee that handlers will be invoked for each distinct
/// value, in order. If you update the value from within a handler, the change
/// will be queued and processed after the current handler has finished. You
/// can always access the current value via <see cref="Value"/>.
/// <br />
/// <see cref="Changed"/> handlers will be invoked whenever a new value is
/// pushed.
/// <br />
/// <see cref="Sync"/> handlers will be invoked immediately when they are added,
/// as well as whenever a new value is published. This allows you to keep the
/// values in "sync" from the moment you're interested in observing them.
/// <br />
/// <see cref="Completed"/> handlers will be invoked when the property has
/// been marked as completed.
/// <br />
/// <see cref="Error"/> handlers will be invoked when an error is pushed to
/// the property. Errors can be pushed more than once and do not throw.
/// </summary>
/// <typeparam name="T">Type of the value to observe.</typeparam>
public sealed class AutoProp<T> : IDisposable, IAutoProp<T> {
  /// <inheritdoc />
  public event Action<T>? Changed;

  /// <inheritdoc />
  public event Action<T>? Sync {
    add {
      InternalSync += value;
      value?.Invoke(Value);
    }
    remove => InternalSync -= value;
  }

  /// <inheritdoc />
  public event Action? Completed;

  /// <inheritdoc />
  public event Action<Exception>? Error;

  /// <inheritdoc />
  public T Value { get; private set; }

  private event Action<T>? InternalSync;
  private bool _busy;
  private bool _completed;
  private bool _disposed;
  private readonly Queue<T> _pending = new();

  /// <inheritdoc />
  public IEqualityComparer<T> Comparer { get; }

  /// <summary>
  /// Creates a new auto property with the given value.
  /// </summary>
  /// <param name="value">Initial value.</param>
  public AutoProp(T value) {
    Value = value;
    Comparer = EqualityComparer<T>.Default;
  }

  /// <summary>
  /// Creates a new auto property with the given value and comparer.
  /// </summary>
  /// <param name="value">Initial value.</param>
  /// <param name="comparer">Equality comparer to use.</param>
  public AutoProp(T value, IEqualityComparer<T> comparer) {
    Value = value;
    Comparer = comparer;
  }

  /// <summary>
  /// Pushes a new value to the property. If the value is distinct from the
  /// previous value, the <see cref="Changed"/> and <see cref="Sync"/> event
  /// handlers will be invoked.
  /// </summary>
  /// <param name="value">New value.</param>
  public void OnNext(T value) {
    if (_completed) { return; }

    _pending.Enqueue(value);

    if (_busy) { return; }

    lock (_pending) {
      _busy = true;

      while (_pending.Count > 0 && !_completed) {
        var next = _pending.Dequeue();

        if (Comparer.Equals(Value, next)) {
          continue;
        }

        Value = next;
        Changed?.Invoke(Value);
        InternalSync?.Invoke(Value);
      }

      _busy = false;
    }
  }

  /// <summary>
  /// Marks the property as completed. This will prevent any further values
  /// from being pushed to the property.
  /// </summary>
  public void OnCompleted() {
    if (_completed) { return; }

    _completed = true;
    _pending.Clear();

    Completed?.Invoke();
  }

  /// <summary>
  /// Pushes an error to the property and invokes any <see cref="Error"/> event
  /// handlers. Errors can be pushed more than once and do not throw. Pushing
  /// an error does not prevent further values from being pushed to the
  /// property.
  /// </summary>
  /// <param name="error">Error to push.</param>
  public void OnError(Exception error) {
    if (_completed) { return; }

    Error?.Invoke(error);
  }

  /// <summary>Clears all event handlers.</summary>
  public void Clear() {
    // Assigning events to null clears registered event handlers.
    // https://stackoverflow.com/a/36084493
    Changed = null;
    InternalSync = null;
    Completed = null;
    Error = null;
  }

  private void Dispose(bool disposing) {
    if (_disposed) {
      return;
    }

    if (disposing) {
      Clear();
    }

    _disposed = true;
  }

  /// <inheritdoc />
  public void Dispose() {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  /// <summary>Finalizer.</summary>
  ~AutoProp() {
    Dispose(false);
  }
}
