namespace Chickensoft.Collections;

using System.Collections.Generic;

/// <summary>
/// A forwarding equality comparer which forwards all calls to an inner
/// comparer. The inner comparer can be changed at any time.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
public sealed class ForwardingComparer<T> : IEqualityComparer<T> {
  /// <summary>
  /// The inner comparer to forward to.
  /// </summary>
  public IEqualityComparer<T> Comparer { get; set; }

  /// <summary>
  /// Creates a new forwarding comparer.
  /// </summary>
  /// <param name="innerComparer">The inner comparer to forward to.</param>
  public ForwardingComparer(IEqualityComparer<T> innerComparer) {
    Comparer = innerComparer;
  }

  /// <inheritdoc />
  public bool Equals(T x, T y) => Comparer.Equals(x, y);

  /// <inheritdoc />
  public int GetHashCode(T obj) => Comparer.GetHashCode(obj);
}
