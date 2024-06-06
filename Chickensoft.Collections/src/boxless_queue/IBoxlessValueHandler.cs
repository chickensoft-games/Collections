namespace Chickensoft.Collections;

/// <summary>
/// Interface for handling values stored in a <see cref="BoxlessQueue"/>.
/// </summary>
public interface IBoxlessValueHandler {
  /// <summary>
  /// Callback invoked when a value is dequeued from a
  /// <see cref="BoxlessQueue"/>.
  /// </summary>
  /// <param name="value">Value that was dequeued.</param>
  /// <typeparam name="TValue">Type of the value.</typeparam>
  void HandleValue<TValue>(in TValue value)
    where TValue : struct;
}
