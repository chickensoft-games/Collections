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
/// Represents a callback for a reference type value that was registered on
/// a binding.
/// </summary>
public sealed class SyncBindingRefCallback {
  internal Action<object> Callback { get; }
  internal Func<object, bool> Checker { get; }

  internal SyncBindingRefCallback(
    Action<object> callback, Func<object, bool> checker
  ) {
    Callback = callback;
    Checker = checker;
  }

  /// <summary>
  /// Predicate that checks if the registered callback should be invoked for
  /// a given value.
  /// </summary>
  /// <param name="value">Value.</param>
  /// <returns>True if the callback handles that type of value and should
  /// be invoked, false otherwise.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool ShouldInvoke(object value) => Checker(value);

  /// <summary>
  /// Invokes the registered callback with the specified value.
  /// </summary>
  /// <param name="value">Value.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Invoke(object value) => Callback(value);
}

/// <summary>
/// Base class for creating custom bindings that listen to announcements from
/// a <see cref="SyncSubject" />.
/// </summary>
public abstract class SyncBinding {
  // channel -> type -> callback
  internal readonly Dictionary<int, Dictionary<Type, List<object>>>
    _valueCallbacks;
  internal readonly Dictionary<int, List<SyncBindingRefCallback>>
    _refCallbacks;

  private static readonly List<object> _emptyValueCallbacks = [];

  /// <summary>
  /// Creates a new <see cref="SyncBinding" /> that can listen to
  /// announcements from a <see cref="SyncSubject" />.
  /// </summary>
  protected SyncBinding() {
    _valueCallbacks = [];
    _refCallbacks = [];
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
  /// Gets all the callbacks registered on the specified announcement channel
  /// for the specified type.
  /// </summary>
  /// <typeparam name="TValue">Value type.</typeparam>
  /// <param name="channel">Announcement channel.</param>
  protected internal IReadOnlyList<object> GetValueTypeCallbacks<TValue>(
    int channel
  ) where TValue : struct {
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
  /// <see cref="SyncBindingRefCallback.ShouldInvoke" /> on each one to see
  /// if it should be invoked for a given value.
  /// </summary>
  /// <param name="channel">Announcement channel.</param>
  protected internal IReadOnlyList<SyncBindingRefCallback> GetRefTypeCallbacks(
    int channel
  ) {
    if (!_refCallbacks.TryGetValue(channel, out var entries)) {
      throw new ArgumentException(
        $"Channel {channel} not found. Be sure to add it with " +
        "AddChannel(int) first.", nameof(channel)
      );
    }

    return entries;
  }

  /// <summary>
  /// Registers a callback which receives value type announcements for the
  /// specified channel.
  /// </summary>
  /// <typeparam name="TValue">Value type.</typeparam>
  /// <param name="channel">Announcement channel.</param>
  /// <param name="handler">Callback which receives values.</param>
  protected void AddValueTypeCallback<TValue>(
    int channel, ValueTypeCallback<TValue> handler
  ) where TValue : struct {
    if (!_valueCallbacks.TryGetValue(channel, out var runners)) {
      throw new ArgumentException(
        $"Channel {channel} not found. Be sure to add it with " +
        "AddChannel(int) first.", nameof(channel)
      );
    }

    if (runners.TryGetValue(typeof(TValue), out var callbacks)) {
      callbacks.Add(handler);
    }
    else {
      runners[typeof(TValue)] = [handler];
    }
  }

  /// <summary>
  /// Registers a callback which receives reference type announcements for the
  /// specified channel.
  /// </summary>
  /// <typeparam name="TValue">Reference type.</typeparam>
  /// <param name="channel">Announcement channel.</param>
  /// <param name="handler">Callback which receives values.</param>
  protected void AddRefTypeCallback<TValue>(
    int channel, RefTypeCallback<TValue> handler
  ) where TValue : class {
    if (!_refCallbacks.TryGetValue(channel, out var runners)) {
      throw new ArgumentException(
        $"Channel {channel} not found. Be sure to add it with " +
        "AddChannel(int) first.", nameof(channel)
      );
    }

    var entry = new SyncBindingRefCallback(
      (value) => handler((TValue)value),
      // respect the type hierarchy
      static (value) => value is TValue
    );

    runners.Add(entry);
  }
}
