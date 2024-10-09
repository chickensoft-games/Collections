namespace Chickensoft.Collections;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

/// <summary>
/// A map is an <see cref="OrderedDictionary" /> wrapper that preserves key
/// insertion order. Based on <a href="https://stackoverflow.com/a/1396743" />
/// </summary>
/// <typeparam name="TKey">Key type.</typeparam>
/// <typeparam name="TValue">Value type.</typeparam>
public class Map<TKey, TValue> :
  IDictionary<TKey, TValue> where TKey : notnull {

  private readonly OrderedDictionary _collection = [];

  /// <summary>Retrieve a map value by key.</summary>
  /// <param name="key">Map key.</param>
  public TValue this[TKey key] {
    get => (TValue)_collection[key];
    set => _collection[key] = value;
  }

  /// <summary>Retrieve a map value by index.</summary>
  /// <param name="index">Index of the value to access.</param>
  public TValue? this[int index] {
    get => (TValue?)_collection[index];
    set => _collection[index] = value;
  }

  /// <summary>
  /// <para>
  /// Creates a new map.
  /// </para>
  /// <para>
  /// <inheritdoc cref="Map{TKey, TValue}" path="/summary" />
  /// </para>
  /// </summary>
  public Map() { }

  /// <summary>
  /// <para>
  /// Creates a new map.
  /// </para>
  /// <para>
  /// <inheritdoc cref="Map{TKey, TValue}" path="/summary" />
  /// </para>
  /// </summary>
  /// <param name="collection">An enumerable of key-value-pairs which should
  /// be added to the map initially.
  /// </param>
  public Map(IEnumerable<KeyValuePair<TKey, TValue>> collection) {
    foreach (var item in collection) {
      _collection.Add(item.Key, item.Value);
    }
  }

  /// <inheritdoc />
  public bool IsReadOnly => _collection.IsReadOnly;

  /// <inheritdoc />
  public int Count => _collection.Count;

  /// <inheritdoc />
  public ICollection<TKey> Keys =>
    _collection.Keys.Cast<TKey>().ToArray();

  /// <inheritdoc />
  public ICollection<TValue> Values =>
    _collection.Values.Cast<TValue>().ToArray();

  /// <summary>Insert a key and value at the specified index.</summary>
  /// <param name="index">Index to insert to.</param>
  /// <param name="key">Key.</param>
  /// <param name="value">Value.</param>
  public void Insert(int index, TKey key, TValue value)
    => _collection.Insert(index, key, value);

  /// <summary>Remove a key/value pair at the specified index.</summary>
  /// <param name="index">Index to remove from.</param>
  public void RemoveAt(int index) => _collection.RemoveAt(index);

  /// <inheritdoc />
  public void Add(TKey key, TValue value) => _collection.Add(key, value);

  /// <inheritdoc />
  public void Clear() => _collection.Clear();

  // IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  /// <inheritdoc />
  public bool ContainsKey(TKey key) => _collection.Contains(key);

  /// <inheritdoc />
  public bool Remove(TKey key) {
    lock (_collection) {
      if (_collection.Contains(key)) {
        _collection.Remove(key);
        return true;
      }
    }
    return false;
  }

  /// <inheritdoc />
  public bool TryGetValue(TKey key, out TValue value) {
    lock (_collection) {
      if (_collection.Contains(key)) {
        value = (TValue)_collection[key];
        return true;
      }
    }
    value = default!;
    return false;
  }

  /// <inheritdoc />
  public void Add(KeyValuePair<TKey, TValue> item) =>
    _collection.Add(item.Key, item.Value);

  /// <inheritdoc />
  public bool Contains(KeyValuePair<TKey, TValue> item) {
    lock (_collection) {
      return _collection.Contains(item.Key) &&
      EqualityComparer<object?>.Default.Equals(
        _collection[item.Key], item.Value
      );
    }
  }

  /// <inheritdoc />
  public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
    lock (_collection) {
      foreach (DictionaryEntry entry in _collection) {
        array[arrayIndex++] = new KeyValuePair<TKey, TValue>(
          (TKey)entry.Key, (TValue)entry.Value
        );
      }
    }
  }

  /// <inheritdoc />
  public bool Remove(KeyValuePair<TKey, TValue> item) {
    lock (_collection) {
      if (Contains(item)) {
        _collection.Remove(item.Key);
        return true;
      }
    }
    return false;
  }

  /// <inheritdoc />
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  /// <inheritdoc />
  public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
    foreach (DictionaryEntry entry in _collection) {
      yield return new KeyValuePair<TKey, TValue>(
        (TKey)entry.Key, (TValue)entry.Value
      );
    }
  }
}
