
namespace Chickensoft.Collections;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// A dictionary that maintains key insertion order and enumerates efficiently
/// and deterministically. This uses <see cref="LinkedList{T}"/> to
/// efficiently track key insertion order and a standard
/// <see cref="Dictionary{TKey, TValue}" /> as the backing store.
/// </summary>
/// <typeparam name="TKey">Key type.</typeparam>
/// <typeparam name="TValue">Value type.</typeparam>
public sealed class LinkedHashMap<TKey, TValue> :
    ICollection<KeyValuePair<TKey, TValue>>,
    IDictionary<TKey, TValue>,
    IReadOnlyDictionary<TKey, TValue> where TKey : notnull {
  private readonly LinkedList<KeyValuePair<TKey, TValue>> _list;
  private readonly Dictionary<
    TKey, LinkedListNode<KeyValuePair<TKey, TValue>>
  > _map;
  private readonly ForwardingComparer<TKey> _keyComparer;
  private readonly ForwardingComparer<TValue> _valueComparer;
  private int _version;

  /// <summary>Comparer used for key equality checks.</summary>
  public IEqualityComparer<TKey> KeyComparer {
    get => _keyComparer.Comparer;
    set {
      if (Count > 0) {
        throw new InvalidOperationException(
          "Cannot change a comparer when the map is not empty."
        );
      }

      _keyComparer.Comparer = value;
    }
  }

  /// <summary>Comparer used for value equality checks.</summary>
  public IEqualityComparer<TValue> ValueComparer {
    get => _valueComparer.Comparer;
    set {
      if (Count > 0) {
        throw new InvalidOperationException(
          "Cannot change a comparer when the map is not empty."
        );
      }

      _valueComparer.Comparer = value;
    }
  }

  /// <summary>
  /// Initializes a new <see cref="LinkedHashMap{TKey, TValue}"/>
  /// dictionary that maintains key insertion order.
  /// </summary>
  public LinkedHashMap() : this(0, null) { }

  /// <summary>
  /// Initializes a new <see cref="LinkedHashMap{TKey, TValue}"/>
  /// dictionary that maintains key insertion order.
  /// </summary>
  /// <param name="capacity">Initial capacity. Leave 0 for default HashSet
  /// behavior.
  /// </param>
  /// <param name="valueComparer">Equality comparer for values.</param>
  /// <param name="keyComparer">Equality comparer for keys.</param>
  public LinkedHashMap(
    int capacity = 0,
    IEqualityComparer<TKey>? keyComparer = null,
    IEqualityComparer<TValue>? valueComparer = null
  ) {
    _valueComparer = new ForwardingComparer<TValue>(
      valueComparer ?? EqualityComparer<TValue>.Default
    );
    _keyComparer = new ForwardingComparer<TKey>(
      keyComparer ?? EqualityComparer<TKey>.Default
    );
    _list = new LinkedList<KeyValuePair<TKey, TValue>>();
    _map = new Dictionary<
      TKey, LinkedListNode<KeyValuePair<TKey, TValue>>
    >(capacity, keyComparer);
  }

  /// <summary>
  /// Initializes a new <see cref="LinkedHashMap{TKey, TValue}"/>
  /// dictionary that maintains key insertion order and adds the elements of
  /// the specified collection.
  /// </summary>
  /// <param name="collection">Collection of initial key-value pairs.</param>
  /// <param name="capacity">Initial capacity. Leave 0 for default HashSet
  /// behavior.
  /// </param>
  /// <param name="valueComparer">Equality comparer for values.</param>
  /// <param name="keyComparer">Equality comparer for keys.</param>
  public LinkedHashMap(
    IEnumerable<KeyValuePair<TKey, TValue>>? collection = null,
    int capacity = 0,
    IEqualityComparer<TKey>? keyComparer = null,
    IEqualityComparer<TValue>? valueComparer = null
  ) : this(capacity, keyComparer, valueComparer) {
    if (collection is ICollection<KeyValuePair<TKey, TValue>> o1) {
      _map.EnsureCapacity(o1.Count);
    }

    if (collection is null) { return; }

    foreach (var kvp in collection) {
      Add(kvp);
    }
  }


  /// <summary>
  /// Gets or sets the value associated with the specified key.
  /// </summary>
  /// <param name="key">Key.</param>
  /// <returns>Value for the specified key.</returns>
  /// <exception cref="KeyNotFoundException" />
  public TValue this[TKey key] {
    get {
      if (_map.TryGetValue(key, out var node)) {
        return node.Value.Value;
      }
      throw new KeyNotFoundException($"Key '{key}' not found in the map.");
    }
    set {
      _version++;
      if (_map.TryGetValue(key, out var node)) {
        node.Value = new KeyValuePair<TKey, TValue>(key, value);
      }
      else {
        var kvp = new KeyValuePair<TKey, TValue>(key, value);
        node = _list.AddLast(kvp);
        _map[key] = node;
      }
    }
  }

  /// <summary>Key enumerator.</summary>
  public KeyEnumerator Keys => new KeyEnumerator(this, _list);

  /// <summary>Value enumerator.</summary>
  public ValueEnumerator Values => new ValueEnumerator(this, _list);

  /// <inheritdoc />
  public int Count => _map.Count;

  /// <inheritdoc />
  public bool IsReadOnly => false;

  // Instead of boxing enumerators, we prefer not to support these. Users
  // should reference/downcast to LinkedHashMap (rather than accessing this
  // as an IDictionary or IReadOnlyDictionary) to get efficient struct
  // enumerators.
  ICollection<TKey> IDictionary<TKey, TValue>.Keys =>
    throw new NotSupportedException(
      "Please call LinkedHashMap.Keys to obtain a struct KeyEnumerator " +
      "that avoids boxing an enumerator."
    );

  ICollection<TValue> IDictionary<TKey, TValue>.Values =>
    throw new NotSupportedException(
      "Please call LinkedHashMap.Values to obtain a struct ValueEnumerator " +
      "that avoids boxing an enumerator."
    );

  IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys =>
    throw new NotSupportedException(
      "Please call LinkedHashMap.Keys to obtain a struct KeyEnumerator " +
      "that avoids boxing an enumerator."
    );

  IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values =>
    throw new NotSupportedException(
      "Please call LinkedHashMap.Values to obtain a struct ValueEnumerator " +
      "that avoids boxing an enumerator."
    );

  /// <summary>
  /// Adds a key-value pair to the dictionary.
  /// </summary>
  /// <param name="key">Key.</param>
  /// <param name="value">Value.</param>
  /// <exception cref="ArgumentException" />
  public void Add(TKey key, TValue value) {
    if (_map.ContainsKey(key)) {
      throw new ArgumentException(
        "An item with the same key has already been added.", nameof(key)
      );
    }

    var kvp = new KeyValuePair<TKey, TValue>(key, value);
    var node = _list.AddLast(kvp);
    _version++;
    _map[key] = node;
  }

  /// <inheritdoc />
  public void Add(KeyValuePair<TKey, TValue> item) {
    if (_map.ContainsKey(item.Key)) {
      throw new ArgumentException(
        "An item with the same key has already been added.", nameof(item)
      );
    }

    var node = _list.AddLast(item);
    _version++;
    _map[item.Key] = node;
  }

  /// <inheritdoc />
  public void Clear() {
    _version++;
    _list.Clear();
    _map.Clear();
  }

  /// <inheritdoc />
  public bool Contains(KeyValuePair<TKey, TValue> item) {
    return
      _map.TryGetValue(item.Key, out var node) &&
      ValueComparer.Equals(node.Value.Value, item.Value);
  }
  /// <summary>
  /// Checks if the dictionary contains the specified key.
  /// </summary>
  /// <param name="key">Key to look for.</param>
  /// <returns>True if the key is present in the dictionary.</returns>
  public bool ContainsKey(TKey key) => _map.ContainsKey(key);


  /// <inheritdoc />
  public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
    _list.CopyTo(array, arrayIndex);
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  IEnumerator<KeyValuePair<TKey, TValue>>
    IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() =>
    GetEnumerator();

  /// <summary>
  /// Struct enumerator for efficient enumeration of the dictionary.
  /// </summary>
  /// <returns>Struct enumerator.</returns>
  public Enumerator GetEnumerator() => new Enumerator(this, _list);

  /// <summary>
  /// Removes the specified key from the dictionary.
  /// </summary>
  /// <param name="key">Key to remove.</param>
  /// <returns>True if the key was present and removed.</returns>
  public bool Remove(TKey key) {
    if (!_map.TryGetValue(key, out var node)) {
      return false;
    }

    _version++;
    _list.Remove(node);
    _map.Remove(key);
    return true;
  }

  /// <summary>
  /// Removes the specified key from the dictionary.
  /// </summary>
  /// <param name="key">Key to remove.</param>
  /// <param name="value">Value associated with the removed key, if found.
  /// </param>
  /// <returns>True if the key was present and removed.</returns>
  public bool Remove(TKey key, [MaybeNullWhen(false)] out TValue value) {
    if (!_map.TryGetValue(key, out var node)) {
      value = default;
      return false;
    }

    _version++;
    _list.Remove(node);
    _map.Remove(key);
    value = node.Value.Value;
    return true;
  }

  /// <summary>
  /// Removes the specified key-value pair from the dictionary.
  /// </summary>
  /// <param name="item">Key-value pair to remove.</param>
  /// <returns>True if the pair was present and removed.</returns>
  public bool Remove(KeyValuePair<TKey, TValue> item) {
    if (
      !_map.TryGetValue(item.Key, out var node) ||
      !ValueComparer.Equals(node.Value.Value, item.Value)
    ) {
      return false;
    }

    _version++;
    _list.Remove(node);
    _map.Remove(item.Key);
    return true;
  }

  /// <summary>
  /// Tries to get the value associated with the specified key.
  /// If the key is found, the value is returned in the out parameter.
  /// If the key is not found, the out parameter is set to the default value
  /// for the type.
  /// </summary>
  /// <param name="key">Key.</param>
  /// <param name="value">Value to receive.</param>
  /// <returns>True if the key was present and the value was set.</returns>
#nullable disable warnings // IReadOnlyDictionary is dumb in netstandard
  public bool TryGetValue(TKey key,
    [MaybeNullWhen(false)]
    out TValue value
  ) {
    if (_map.TryGetValue(key, out var node)) {
      value = node.Value.Value;
      return true;
    }

    value = default;
    return false;
  }
#nullable restore warnings

  /// <summary>
  /// Struct enumerator for iterating over the keys in the dictionary.
  /// </summary>
  public struct KeyEnumerator : IEnumerator<TKey> {
    private readonly LinkedHashMap<TKey, TValue> _owner;
    private readonly int _version;
    private LinkedListNode<KeyValuePair<TKey, TValue>>? _current;
    private readonly LinkedList<KeyValuePair<TKey, TValue>> _list;

    internal KeyEnumerator(
      LinkedHashMap<TKey, TValue> owner,
      LinkedList<KeyValuePair<TKey, TValue>> list
    ) {
      _owner = owner;
      _version = owner._version;
      _list = list;
      _current = null;
    }

    /// <summary>
    /// Converts the keys in the dictionary to an array.
    /// </summary>
    /// <returns>An array of keys.</returns>
    public readonly TKey[] ToArray() {
      var array = new TKey[_list.Count];
      var index = 0;
      foreach (var kvp in _list) {
        array[index++] = kvp.Key;
      }
      return array;
    }

    /// <summary>
    /// Converts the keys in the dictionary to a list.
    /// </summary>
    /// <returns>A list of keys.</returns>
    public readonly List<TKey> ToList() {
      var list = new List<TKey>(_list.Count);
      foreach (var kvp in _list) {
        list.Add(kvp.Key);
      }
      return list;
    }

    /// <inheritdoc />
    public readonly TKey Current => _current!.Value.Key;

    readonly object IEnumerator.Current =>
      _current!.Value.Key!;


    /// <summary>
    /// Allows the enumerator to be used in a foreach loop.
    /// </summary>
    /// <returns>Itself.</returns>
    public readonly KeyEnumerator GetEnumerator() => this;
    // ------------------------- ^
    // The one place that C# allows duck typing. If this method is implemented,
    // you can use it as the sequence in a foreach loop. No interface needed.

    /// <inheritdoc />
    public void Dispose() => Reset();

    /// <inheritdoc />
    public bool MoveNext() {
      if (_owner._version != _version) {
        throw new InvalidOperationException(
          "LinkedHashMap was modified during enumeration."
        );
      }

      _current = _current == null ? _list.First : _current.Next;
      return _current != null;
    }

    /// <inheritdoc />
    public void Reset() {
      _current = null;
    }
  }

  /// <summary>
  /// Struct enumerator for iterating over the values in the dictionary.
  /// </summary>
  public struct ValueEnumerator : IEnumerator<TValue> {
    private readonly LinkedHashMap<TKey, TValue> _owner;
    private readonly int _version;
    private LinkedListNode<KeyValuePair<TKey, TValue>>? _current;
    private readonly LinkedList<KeyValuePair<TKey, TValue>> _list;

    internal ValueEnumerator(
      LinkedHashMap<TKey, TValue> owner,
      LinkedList<KeyValuePair<TKey, TValue>> list
    ) {
      _owner = owner;
      _version = owner._version;
      _list = list;
      _current = null;
    }

    /// <summary>
    /// Converts the values in the dictionary to an array.
    /// </summary>
    /// <returns>An array of values.</returns>
    public readonly TValue[] ToArray() {
      var array = new TValue[_list.Count];
      var index = 0;
      foreach (var kvp in _list) {
        array[index++] = kvp.Value;
      }
      return array;
    }

    /// <summary>
    /// Converts the values in the dictionary to a list.
    /// </summary>
    /// <returns>A list of values.</returns>
    public readonly List<TValue> ToList() {
      var list = new List<TValue>(_list.Count);
      foreach (var kvp in _list) {
        list.Add(kvp.Value);
      }
      return list;
    }

    /// <inheritdoc />
    public readonly TValue Current => _current!.Value.Value;

    readonly object IEnumerator.Current =>
      _current!.Value.Value!;

    /// <summary>
    /// Allows the enumerator to be used in a foreach loop.
    /// </summary>
    /// <returns>Itself.</returns>
    public readonly ValueEnumerator GetEnumerator() => this;


    /// <inheritdoc />
    public void Dispose() => Reset();

    /// <inheritdoc />
    public bool MoveNext() {
      if (_owner._version != _version) {
        throw new InvalidOperationException(
          "LinkedHashMap was modified during enumeration."
        );
      }

      _current = _current == null ? _list.First : _current.Next;
      return _current != null;
    }

    /// <inheritdoc />
    public void Reset() {
      _current = null;
    }
  }

  /// <summary>
  /// Struct enumerator for iterating over key-value pairs in the dictionary.
  /// </summary>

  public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>> {
    private readonly LinkedHashMap<TKey, TValue> _owner;
    private readonly int _version;
    private LinkedListNode<KeyValuePair<TKey, TValue>>? _current;
    private readonly LinkedList<KeyValuePair<TKey, TValue>> _list;

    internal Enumerator(
      LinkedHashMap<TKey, TValue> owner,
      LinkedList<KeyValuePair<TKey, TValue>> list
    ) {
      _owner = owner;
      _version = owner._version;
      _list = list;
      _current = null;
    }

    /// <summary>
    /// Converts the key-value pairs in the dictionary to an array.
    /// </summary>
    /// <returns>An array of key-value pairs.</returns>
    public readonly KeyValuePair<TKey, TValue>[] ToArray() {
      var array = new KeyValuePair<TKey, TValue>[_list.Count];
      _list.CopyTo(array, 0);
      return array;
    }

    /// <summary>
    /// Converts the key-value pairs in the dictionary to a list.
    /// </summary>
    /// <returns>A list of key-value pairs.</returns>
    public readonly List<KeyValuePair<TKey, TValue>> ToList() {
      var list = new List<KeyValuePair<TKey, TValue>>(_list.Count);
      foreach (var kvp in _list) {
        list.Add(kvp);
      }
      return list;
    }

    /// <inheritdoc />
    public readonly KeyValuePair<TKey, TValue> Current => _current!.Value;

    readonly object IEnumerator.Current => _current!.Value!;

    /// <summary>
    /// Allows the enumerator to be used in a foreach loop.
    /// </summary>
    /// <returns>Itself.</returns>
    public readonly Enumerator GetEnumerator() => this;

    /// <inheritdoc />
    public void Dispose() => Reset();

    /// <inheritdoc />
    public bool MoveNext() {
      if (_owner._version != _version) {
        throw new InvalidOperationException(
          "LinkedHashMap was modified during enumeration."
        );
      }

      _current = _current == null ? _list.First : _current.Next;
      return _current != null;
    }

    /// <inheritdoc />
    public void Reset() {
      _current = null;
    }
  }
}
