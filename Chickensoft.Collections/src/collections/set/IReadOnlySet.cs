namespace Chickensoft.Collections;

using System.Collections.Generic;

/// <summary>
/// Provides a readonly abstraction of a set.
/// </summary>
/// <typeparam name="T">The type of elements in the set.</typeparam>
public interface IReadOnlySet<T> : IReadOnlyCollection<T> {
  /// <summary>
  /// Determines whether the set contains the specified element.
  /// </summary>
  /// <param name="item">The element to locate in the set.</param>
  /// <returns>True if the set contains the specified element; otherwise, false.
  /// </returns>
  bool Contains(T item);

  /// <summary>
  /// Determines whether a set is a subset of a specified collection.
  /// </summary>
  /// <param name="other">The collection to compare to the current set.</param>
  /// <returns>True if the current set is a subset of other; otherwise, false.
  /// </returns>
  /// <remarks>If other contains the same elements as the current set, the
  /// current set is still considered a subset of other. This method always
  /// returns false if the current set has elements that are not in other.
  /// </remarks>
  bool IsSubsetOf(IEnumerable<T> other);

  /// <summary>
  /// Determines whether the current set is a superset of a specified
  /// collection.
  /// </summary>
  /// <param name="other">The collection to compare to the current set.</param>
  /// <returns>True if the current set is a subset of other; otherwise, false.
  /// </returns>
  /// <remarks>
  /// If other contains the same elements as the current set, the current set
  /// is still considered a subset of other. This method always returns false
  /// if the current set has elements that are not in other.
  /// </remarks>
  bool IsSupersetOf(IEnumerable<T> other);

  /// <summary>
  /// Determines whether the current set is a proper (strict) superset of a
  /// specified collection.
  /// </summary>
  /// <param name="other">The collection to compare to the current set.</param>
  /// <returns>True if the current set is a proper superset of other;
  /// otherwise, false.</returns>
  /// <remarks>
  /// If the current set is a proper superset of other, the current set must
  /// have at least one element that other does not have.
  /// <br />
  /// An empty set is a proper superset of any other collection. Therefore, this
  /// method returns true if the collection represented by the other parameter
  /// is empty, unless the current set is also empty.
  /// <br />
  /// This method always returns false if the number of elements in the current
  /// set is less than or equal to the number of elements in other.
  /// </remarks>
  bool IsProperSupersetOf(IEnumerable<T> other);

  /// <summary>
  /// Determines whether the current set is a proper (strict) subset of a
  /// specified collection.
  /// </summary>
  /// <param name="other">The collection to compare to the current set.</param>
  /// <returns>True if the current set is a proper subset of other; otherwise,
  /// false.</returns>
  /// <remarks>
  /// If the current set is a proper subset of other, other must have at least
  /// one element that the current set does not have.
  /// <br />
  /// An empty set is a proper subset of any other collection. Therefore, this
  /// method returns true if the current set is empty, unless the other
  /// parameter is also an empty set.
  /// <br />
  /// This method always returns false if the current set has more or the same
  ///  number of elements than other.
  /// </remarks>
  bool IsProperSubsetOf(IEnumerable<T> other);

  /// <summary>
  /// Determines whether the current set overlaps with the specified collection.
  /// </summary>
  /// <param name="other">The collection to compare to the current set.</param>
  /// <returns>True if the current set and other share at least one common
  /// element; otherwise, false.</returns>
  /// <remarks>Any duplicate elements in other are ignored.</remarks>
  bool Overlaps(IEnumerable<T> other);

  /// <summary>
  /// Determines whether the current set and the specified collection contain
  /// the same elements.
  /// </summary>
  /// <param name="other">The collection to compare to the current set.</param>
  /// <returns>True if the current set is equal to other; otherwise, false.
  /// </returns>
  /// <remarks>
  /// This method ignores the order of elements and any duplicate elements in
  /// other.
  /// </remarks>
  bool SetEquals(IEnumerable<T> other);
}
