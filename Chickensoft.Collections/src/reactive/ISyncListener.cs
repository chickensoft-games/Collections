namespace Chickensoft.Collections.Reactive;


/// <summary>
/// Listens to values announced by a <see cref="SyncSubject"/> synchronously.
/// Implement this to be informed of every value announced by a
/// <see cref="SyncSubject"/>, regardless of its channel or type.
/// </summary>
public interface ISyncListener {
  /// <summary>
  /// A value type was announced on the given channel.
  /// </summary>
  /// <typeparam name="TValue">Value type.</typeparam>
  /// <param name="channel">Announcement channel.</param>
  /// <param name="value">Value.</param>
  void OnValueType<TValue>(int channel, in TValue value) where TValue : struct;

  /// <summary>
  /// A reference type was announced on the given channel.
  /// </summary>
  /// <typeparam name="TValue">Value type.</typeparam>
  /// <param name="channel">Announcement channel.</param>
  /// <param name="value">Value.</param>
  void OnReferenceType<TValue>(int channel, TValue value) where TValue : class;
}
