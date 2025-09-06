namespace Chickensoft.Collections;

using System.Collections.Generic;

/// <summary>
/// Comparer that checks for reference equality of objects.
/// </summary>
/// <typeparam name="T">Object type.</typeparam>
public sealed class ReferenceComparer<T> : IEqualityComparer<T> {
  /// <summary>
  /// Default comparer
  /// </summary>
  public static ReferenceComparer<T> Default { get; } =
    _instance ??= new ReferenceComparer<T>();

  private static ReferenceComparer<T>? _instance;

  /// <inheritdoc />
  public bool Equals(T x, T y) => ReferenceEquals(x, y);

  /// <inheritdoc />
  public int GetHashCode(T obj) {
    return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
  }
}
