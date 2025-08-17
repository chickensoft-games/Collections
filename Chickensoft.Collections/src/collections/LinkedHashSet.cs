namespace Chickensoft.Collections;

using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A set which preserves insertion order and enumerates efficiently and
/// deterministically. This uses <see cref="PooledLinkedList{T}"/> to
/// efficiently track key insertion order and a standard
/// <see cref="Dictionary{TKey, TValue}" /> as the backing store.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
public sealed class LinkedHashSet<T> : ICollection<T> where T : notnull {
  private readonly PooledLinkedList<T> _list;
  private readonly Dictionary<T, PooledLinkedListNode<T>> _map;
  private int _version;

  /// <summary>
  /// Comparer used for equality checks.
  /// </summary>
  public IEqualityComparer<T> Comparer { get; }

  /// <summary>
  /// Creates an empty LinkedHashSet.
  /// </summary>
  public LinkedHashSet() : this(0, null) { }

  /// <summary>
  /// Initializes an empty set with a specified capacity and comparer.
  /// </summary>
  /// <param name="capacity">Initial capacity. Leave 0 for default HashSet
  /// behavior.
  /// </param>
  /// <param name="comparer">Equality comparer.</param>
  public LinkedHashSet(
    int capacity = 0,
    IEqualityComparer<T>? comparer = null
  ) {
    Comparer = comparer ?? EqualityComparer<T>.Default;
    _list = new PooledLinkedList<T>();
    _map = new Dictionary<T, PooledLinkedListNode<T>>(capacity, Comparer);
  }

  /// <summary>
  /// Initializes the set with the given items, preserving their order.
  /// </summary>
  /// <param name="collection">Items to add.</param>
  /// <param name="capacity">Initial capacity. Leave 0 for default HashSet
  /// behavior.
  /// </param>
  /// <param name="comparer">Equality comparer.</param>
  public LinkedHashSet(
    IEnumerable<T> collection,
    int capacity = 0,
    IEqualityComparer<T>? comparer = null
  ) : this(capacity, comparer) {
    foreach (var item in collection) {
      Add(item);
    }
  }

  /// <inheritdoc />
  public int Count => _map.Count;

  /// <inheritdoc />
  public bool IsReadOnly => false;

  /// <summary>
  /// Adds an item to the set if it's not already present.
  /// </summary>
  /// <returns>True if the item was added; false if it was already in the set.
  /// </returns>
  public bool Add(T item) {
    if (_map.ContainsKey(item)) {
      return false;
    }
    var node = _list.AddLast(item);
    _map[item] = node;
    _version++;
    return true;
  }

  void ICollection<T>.Add(T item) => Add(item);

  /// <summary>
  /// Removes the item from the set.
  /// </summary>
  public bool Remove(T item) {
    if (!_map.TryGetValue(item, out var node)) {
      return false;
    }
    _version++;
    _list.Remove(node);
    _map.Remove(item);
    return true;
  }

  /// <inheritdoc />
  public void Clear() {
    _version++;
    _list.Clear();
    _map.Clear();
  }

  /// <inheritdoc />
  public bool Contains(T item) => _map.ContainsKey(item);

  /// <inheritdoc />
  public void CopyTo(T[] array, int arrayIndex) {
    _list.CopyTo(array, arrayIndex);
  }

  /// <summary>
  /// Returns a struct-based enumerator over the elements in insertion order.
  /// </summary>
  public Enumerator GetEnumerator() => new Enumerator(this, _list);

  IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  /// <summary>
  /// Struct enumerator that throws if the set is modified during enumeration.
  /// </summary>
  public struct Enumerator : IEnumerator<T> {
    private readonly LinkedHashSet<T> _owner;
    private readonly int _version;
    private PooledLinkedListNode<T>? _current;
    private readonly PooledLinkedList<T> _list;

    internal Enumerator(LinkedHashSet<T> owner, PooledLinkedList<T> list) {
      _owner = owner;
      _version = owner._version;
      _list = list;
      _current = null;
    }

    /// <inheritdoc />
    public readonly T Current => _current!.Value;
    readonly object IEnumerator.Current => _current!.Value;

    /// <inheritdoc />
    public bool MoveNext() {
      if (_owner._version != _version) {
        throw new InvalidOperationException(
        "LinkedHashSet was modified during enumeration."
        );
      }
      _current = _current == null ? _list.First : _current.Next;
      return _current != null;
    }

    /// <inheritdoc />
    public void Reset() {
      _current = null;
    }

    /// <inheritdoc />
    public void Dispose() {
      Reset();
    }
  }
}
