namespace Chickensoft.Collections.Reactive;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// Callback that receives a value type value.
/// </summary>
/// <typeparam name="TValue">Value type.</typeparam>
/// <param name="value">Value.</param>
public delegate void ValueTypeCallback<TValue>(in TValue value)
  where TValue : struct;

/// <summary>
/// Callback that receives a reference type value.
/// </summary>
/// <typeparam name="TValue">Value type.</typeparam>
/// <param name="value">Value.</param>
public delegate void RefTypeCallback<TValue>(TValue value)
  where TValue : class;

/// <summary>
/// A binding that listens to announcements from a <see cref="SyncSubject" />.
/// Bindings can register callbacks for specific channels and types of
/// announcements. When an announcement is made on a channel, all relevant
/// callbacks are invoked synchronously on the same call stack.
/// </summary>
public interface ISyncBinding : IDisposable {
  /// <summary>
  /// Invokes the bindings' value type callbacks for the specified channel
  /// and value.
  /// </summary>
  /// <typeparam name="TValue">Type of value.</typeparam>
  /// <param name="channel">Channel id.</param>
  /// <param name="value">Value.</param>
  void InvokeValueTypeCallbacks<TValue>(int channel, in TValue value)
    where TValue : struct;


  /// <summary>
  /// Invokes the bindings' reference type callbacks for the specified channel
  /// and value.
  /// </summary>
  /// <param name="channel">Channel id.</param>
  /// <param name="value">Value.</param>
  void InvokeRefTypeCallbacks(int channel, object value);

  /// <summary>
  /// Binds this binding to the specified subject, allowing it to receive
  /// announcements. A binding can only be bound to a single subject during its
  /// lifetime. Attempting to bind an already-bound or disposed binding will
  /// throw an exception.
  /// </summary>
  /// <param name="subject">Subject to bind to.</param>
  void Bind(SyncSubject subject);
}

/// <summary>
/// Base class for creating custom bindings that listen to announcements from
/// a <see cref="SyncSubject" />.
/// </summary>
public abstract class SyncBinding : ISyncBinding {
  internal readonly record struct RefCallback(
    Action<object> Callback, Func<object, bool> Checker
  ) {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool ShouldInvoke(object value) => Checker(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Invoke(object value) => Callback(value);
  }

  // channel -> type -> callback
  private readonly Dictionary<int, Dictionary<Type, List<object>>>
    _valueCallbacks;
  private readonly Dictionary<int, List<RefCallback>>
    _refCallbacks;

  private static readonly List<object> _emptyValueCallbacks = [];

  /// <summary>
  /// The subject this binding is attached to. May be null if the binding
  /// has been disposed.
  /// </summary>
  protected SyncSubject? _subject;
  private bool _isDisposed;

  /// <inheritdoc />
  public void InvokeValueTypeCallbacks<TValue>(int channel, in TValue value)
    where TValue : struct {
    var callbacks = GetValueTypeCallbacks<TValue>(channel);

    for (var i = 0; i < callbacks.Count; i++) {
      var callback = (ValueTypeCallback<TValue>)callbacks[i];
      callback(in value);
    }
  }

  /// <inheritdoc />
  public void InvokeRefTypeCallbacks(int channel, object value) {
    var callbacks = GetRefTypeCallbacks(channel);

    for (var i = 0; i < callbacks.Count; i++) {
      var entry = callbacks[i];

      if (!entry.ShouldInvoke(value)) { continue; }

      entry.Invoke(value);
    }
  }

  /// <summary>
  /// Creates a new <see cref="SyncBinding" /> that can listen to
  /// announcements from a <see cref="SyncSubject" />.
  /// </summary>
  protected SyncBinding(SyncSubject? subject = null) {
    if (subject is not null) {
      Bind(subject);
    }

    _valueCallbacks = [];
    _refCallbacks = [];
  }

  /// <inheritdoc />
  public void Bind(SyncSubject subject) {
    if (_subject is not null) {
      throw new InvalidOperationException(
        "This binding is already bound to a subject."
      );
    }

    if (_isDisposed) {
      throw new ObjectDisposedException(
        nameof(SyncBinding),
        "Binding has already been disposed and cannot be bound to a subject."
      );
    }

    _subject = subject;
    _subject.AddBinding(this);
  }

  /// <summary>
  /// Adds a channel. Channels are used to differentiate between different
  /// types of announcements.
  /// </summary>
  /// <param name="channel"></param>
  protected internal void AddChannel(int channel) {
    if (_valueCallbacks.ContainsKey(channel)) {
      return;
    }

    _valueCallbacks[channel] = [];
    _refCallbacks[channel] = [];
  }

  /// <summary>
  /// Registers a callback which receives value type announcements for the
  /// specified channel.
  /// </summary>
  /// <typeparam name="TValue">Value type.</typeparam>
  /// <param name="channel">Announcement channel.</param>
  /// <param name="callback">Callback which receives values.</param>
  /// <param name="predicate">Optional predicate that checks if the
  /// callback should be invoked for a given value. If not specified, the
  /// callback will be invoked for any value of the specified type.</param>
  protected void AddValueTypeCallback<TValue>(
    int channel,
    ValueTypeCallback<TValue> callback,
    Func<TValue, bool>? predicate = null
  ) where TValue : struct {
    if (!_valueCallbacks.TryGetValue(channel, out var runners)) {
      throw new ArgumentException(
        $"Channel {channel} not found. Be sure to add it with " +
        "AddChannel(int) first.", nameof(channel)
      );
    }

#pragma warning disable IDE0350 // tries to erroneously simplify a lambda
    var callbackToUse = predicate is null
      ? callback
      : (in TValue value) => { if (predicate(value)) { callback(value); } };
#pragma warning restore IDE0350

    if (runners.TryGetValue(typeof(TValue), out var callbacks)) {
      callbacks.Add(callbackToUse);
    }
    else {
      runners[typeof(TValue)] = [callbackToUse];
    }
  }

  /// <summary>
  /// Registers a callback which receives reference type announcements for the
  /// specified channel.
  /// </summary>
  /// <typeparam name="TValue">Reference type.</typeparam>
  /// <param name="channel">Announcement channel.</param>
  /// <param name="callback">Callback which receives values.</param>
  /// <param name="predicate">Optional predicate that checks if the
  /// callback should be invoked for a given value. If not specified, the
  /// callback will be invoked for any value of the specified type.</param>
  protected void AddRefTypeCallback<TValue>(
    int channel,
    RefTypeCallback<TValue> callback,
    Func<TValue, bool>? predicate = null
  ) where TValue : class {
    if (!_refCallbacks.TryGetValue(channel, out var runners)) {
      throw new ArgumentException(
        $"Channel {channel} not found. Be sure to add it with " +
        "AddChannel(int) first.", nameof(channel)
      );
    }

    var entry = new RefCallback(
      Callback: (value) => callback((TValue)value),
      // respect the type hierarchy
      Checker: predicate is null
        ? static (value) => value is TValue
        : (value) => value is TValue valueTyped && predicate(valueTyped)
    );

    runners.Add(entry);
  }

  /// <summary>
  /// Gets all the callbacks registered on the specified announcement channel
  /// for the specified type.
  /// </summary>
  /// <typeparam name="TValue">Value type.</typeparam>
  /// <param name="channel">Announcement channel.</param>
  internal List<object> GetValueTypeCallbacks<TValue>(int channel)
      where TValue : struct {
    if (!_valueCallbacks.TryGetValue(channel, out var runners)) {
      throw new ArgumentException(
        $"Channel {channel} not found. Be sure to add it with " +
        "AddChannel(int) first.", nameof(channel)
      );
    }

    if (!runners.TryGetValue(typeof(TValue), out var callbacks)) {
      // no callbacks for this type — nothing to do
      return _emptyValueCallbacks;
    }

    return callbacks;
  }

  /// <summary>
  /// Gets all the callbacks registered on the specified announcement channel.
  /// These are not filtered by type, so you'll need to check
  /// <see cref="RefCallback.ShouldInvoke" /> on each one to see
  /// if it should be invoked for a given value.
  /// </summary>
  /// <param name="channel">Announcement channel.</param>
  internal List<RefCallback> GetRefTypeCallbacks(int channel) {
    if (!_refCallbacks.TryGetValue(channel, out var entries)) {
      throw new ArgumentException(
        $"Channel {channel} not found. Be sure to add it with " +
        "AddChannel(int) first.", nameof(channel)
      );
    }

    return entries;
  }

  #region IDisposable

  /// <inheritdoc />
  protected virtual void Dispose(bool disposing) {
    if (_isDisposed) {
      return;
    }

    if (disposing) {
      _subject!.RemoveBinding(this);
      _valueCallbacks.Clear();
      _refCallbacks.Clear();
    }

    _subject = null;
    _isDisposed = true;
  }

  /// <inheritdoc />
  public void Dispose() {
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }

  #endregion IDisposable
}
