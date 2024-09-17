namespace Chickensoft.Collections;

using System.Collections.Generic;

/// <summary>
/// <see cref="HashSet{T}" /> extensions.
/// </summary>
public static class HashSetEx {
  /// <summary>
  /// Creates a copy of the hash set with the specified item included.
  /// </summary>
  /// <typeparam name="T">Type of the item.</typeparam>
  /// <param name="set">Hash set.</param>
  /// <param name="item">Item to include.</param>
  /// <returns>A new hash set.</returns>
  public static HashSet<T> With<T>(this HashSet<T> set, T item) {
    var copy = new HashSet<T>(set) { item };
    return copy;
  }

  /// <summary>
  /// Creates a copy of the hash set with the specified item excluded.
  /// </summary>
  /// <typeparam name="T">Type of the item.</typeparam>
  /// <param name="set">Hash set.</param>
  /// <param name="item">Item to exclude.</param>
  /// <returns>A new hash set.</returns>
  public static HashSet<T> Without<T>(this HashSet<T> set, T item) {
    var copy = new HashSet<T>(set);
    copy.Remove(item);
    return copy;
  }
}
