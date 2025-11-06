namespace Chickensoft.Collections;

using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A set which preserves insertion order and enumerates efficiently and
/// deterministically. This uses <see cref="LinkedList{T}"/> to
/// efficiently track key insertion order and a standard
/// <see cref="Dictionary{TKey, TValue}" /> as the backing store.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
public sealed class LinkedHashSet<T> : ICollection<T>
{
  private readonly LinkedList<T> _list;
  private readonly Dictionary<T, LinkedListNode<T>> _map;
  private readonly ForwardingComparer<T> _comparer;
  private int _version;

  /// <summary>
  /// Comparer used for equality checks.
  /// </summary>
  public IEqualityComparer<T> Comparer
  {
    get => _comparer.Comparer;
    set
    {
      if (Count > 0)
      {
        throw new InvalidOperationException(
          "Cannot change the comparer when the set is not empty."
        );
      }

      _comparer.Comparer = value;
    }
  }

  /// <summary>
  /// Creates an empty LinkedHashSet.
  /// </summary>
  public LinkedHashSet() : this(null, 0) { }

  /// <summary>
  /// Initializes the set with the given items, preserving their order.
  /// </summary>
  /// <param name="collection">Items to add.</param>
  /// <param name="capacity">Initial capacity. Leave 0 for default HashSet
  /// behavior.
  /// </param>
  /// <param name="comparer">Equality comparer.</param>
  public LinkedHashSet(
    IEnumerable<T>? collection = null,
    int capacity = 0,
    IEqualityComparer<T>? comparer = null
  )
  {
    _comparer = new ForwardingComparer<T>(
      comparer ?? EqualityComparer<T>.Default
    );
    _list = new LinkedList<T>();
    _map = new Dictionary<T, LinkedListNode<T>>(capacity, _comparer);

    if (collection is null)
    {
      return;
    }

    foreach (var item in collection)
    {
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
  public bool Add(T item)
  {
    if (_map.ContainsKey(item))
    {
      return false;
    }
    var node = _list.AddLast(item);
    _map[item] = node;
    _version++;
    return true;
  }

  void ICollection<T>.Add(T item) => Add(item);

  /// <summary>
  /// Adds all items from the given enumerable to the set.
  /// </summary>
  public void UnionWith(IEnumerable<T> other)
  {
    foreach (var item in other)
    {
      Add(item);
    }
  }

  /// <summary>
  /// Removes the item from the set.
  /// </summary>
  public bool Remove(T item)
  {
    if (!_map.TryGetValue(item, out var node))
    {
      return false;
    }
    _version++;
    _list.Remove(node);
    _map.Remove(item);
    return true;
  }

  /// <inheritdoc />
  public void Clear()
  {
    _version++;
    _list.Clear();
    _map.Clear();
  }

  /// <inheritdoc />
  public bool Contains(T item) => _map.ContainsKey(item);

  /// <summary>
  /// Attempts to get the actual stored value that is equal to the given
  /// value.
  /// </summary>
  /// <param name="equalValue">The value to search for.</param>
  /// <param name="actualValue">The actual stored value, if found.</param>
  /// <returns>True if the value was found, false otherwise.</returns>
  public bool TryGetValue(T equalValue, out T actualValue)
  {
    if (_map.TryGetValue(equalValue, out var node))
    {
      actualValue = node.Value;
      return true;
    }
    actualValue = default!;
    return false;
  }

  /// <inheritdoc />
  public void CopyTo(T[] array, int arrayIndex) =>
    _list.CopyTo(array, arrayIndex);

  #region Enumeration

  /// <summary>
  /// Returns a struct-based enumerator over the elements in insertion order.
  /// </summary>
  public Enumerator GetEnumerator() => new(this, _list);

  /// <summary>
  /// Returns a struct-based reverse enumerator over the elements in reverse
  /// insertion order.
  /// </summary>
  public ReverseEnumerator GetReverseEnumerator() =>
    new(this, _list);

  /// <inheritdoc />
  IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

  /// <inheritdoc />
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  #endregion Enumeration

  /// <summary>
  /// Struct enumerator that throws if the set is modified during enumeration.
  /// </summary>
  public struct Enumerator : IEnumerator<T>
  {
    private readonly LinkedHashSet<T> _owner;
    private readonly int _version;
    private LinkedListNode<T>? _current;
    private readonly LinkedList<T> _list;

    internal Enumerator(LinkedHashSet<T> owner, LinkedList<T> list)
    {
      _owner = owner;
      _version = owner._version;
      _list = list;
      _current = null;
    }

    /// <inheritdoc />
    public readonly T Current => _current!.Value;
    readonly object IEnumerator.Current => _current!.Value!;

    /// <inheritdoc />
    public bool MoveNext()
    {
      if (_owner._version != _version)
      {
        throw new InvalidOperationException(
        "LinkedHashSet was modified during enumeration."
        );
      }
      _current = _current == null ? _list.First : _current.Next;
      return _current != null;
    }

    /// <inheritdoc />
    public void Reset() => _current = null;

    /// <inheritdoc />
    public void Dispose() => Reset();
  }

  /// <summary>
  /// A reverse struct enumerator that throws if the set is modified during
  /// enumeration.
  /// </summary>
  public struct ReverseEnumerator : IEnumerator<T>
  {
    private readonly LinkedHashSet<T> _owner;
    private readonly int _version;
    private LinkedListNode<T>? _current;
    private readonly LinkedList<T> _list;

    internal ReverseEnumerator(LinkedHashSet<T> owner, LinkedList<T> list)
    {
      _owner = owner;
      _version = owner._version;
      _list = list;
      _current = null;
    }

    /// <inheritdoc />
    public readonly T Current => _current!.Value;
    readonly object IEnumerator.Current => _current!.Value!;

    /// <inheritdoc />
    public bool MoveNext()
    {
      if (_owner._version != _version)
      {
        throw new InvalidOperationException(
        "LinkedHashSet was modified during enumeration."
        );
      }
      _current = _current == null ? _list.Last : _current.Previous;
      return _current != null;
    }

    /// <inheritdoc />
    public void Reset() => _current = null;

    /// <inheritdoc />
    public void Dispose() => Reset();
  }
}
