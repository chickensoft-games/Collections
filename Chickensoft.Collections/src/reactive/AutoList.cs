namespace Chickensoft.Collections.Reactive;

using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>A readonly reference to an observable list.</summary>
/// <typeparam name="T">Item type.</typeparam>
public interface IAutoList<T> : IReadOnlyList<T> where T : class {
  /// <summary>
  /// Equality comparer used to determine item equality.
  /// </summary>
  IEqualityComparer<T> Comparer { get; }

  /// <summary>
  /// Creates a new binding that listens to changes in the list.
  /// </summary>
  AutoList<T>.Binding Bind();
}

/// <summary>
/// An observable list.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
public interface IMutableAutoList<T> : IAutoList<T>, IList<T> where T : class;

/// <summary>
/// An observable mutable list.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
public sealed class AutoList<T> : IMutableAutoList<T> where T : class {
  internal const int ADD_CHANNEL = 0;
  internal const int UPDATE_CHANNEL = 1;
  internal const int REMOVE_CHANNEL = 2;
  internal const int CLEAR_CHANNEL = 3;

  internal readonly record struct Entry(T Item, int Index);
  internal readonly record struct UpdateEntry(T Previous, T Item, int Index);

  /// <summary>
  /// A binding to an <see cref="AutoList{T}" />.
  /// </summary>
  public interface IBinding : ISyncBinding {
    /// <summary>
    /// Registers a callback to be invoked when an item is added to the list.
    /// </summary>
    /// <param name="callback">Callback which receives the item.</param>
    void OnAdd(Action<T> callback);

    /// <summary>
    /// Registers a callback to be invoked when an item is added to the list.
    /// </summary>
    /// <param name="callback">Callback which receives the item and its index.
    /// </param>
    void OnAdd(Action<T, int> callback);

    /// <summary>
    /// Registers a callback to be invoked when an item in the list is updated.
    /// </summary>
    /// <param name="callback">Callback which receives the previous item and
    /// the next item.</param>
    void OnUpdate(Action<T, T> callback);

    /// <summary>
    /// Registers a callback to be invoked when an item in the list is updated.
    /// </summary>
    /// <param name="callback">Callback which receives the previous item,
    /// the next item, and the index of the item.</param>
    void OnUpdate(Action<T, T, int> callback);

    /// <summary>
    /// Registers a callback to be invoked when an item is removed from the
    /// list.
    /// </summary>
    /// <param name="callback">Callback which receives the removed item.</param>
    void OnRemove(Action<T> callback);

    /// <summary>
    /// Registers a callback to be invoked when an item is removed from the
    /// list.
    /// </summary>
    /// <param name="callback">Callback which receives the removed item and
    /// its index.</param>
    void OnRemove(Action<T, int> callback);

    /// <summary>
    /// Registers a callback to be invoked when the list is cleared.
    /// </summary>
    /// <param name="callback">Callback.</param>
    void OnClear(Action callback);
  }

  /// <summary>
  /// A binding to an <see cref="AutoList{T}" />.
  /// </summary>

  public class Binding : SyncBinding, IBinding {
    internal Binding(SyncSubject subject) : base(subject) {
      AddChannel(ADD_CHANNEL);
      AddChannel(UPDATE_CHANNEL);
      AddChannel(REMOVE_CHANNEL);
      AddChannel(CLEAR_CHANNEL);
    }

    /// <inheritdoc />
    public void OnAdd(Action<T> callback) =>
      AddRefTypeCallback(ADD_CHANNEL, (T item) => callback(item));

    /// <inheritdoc />
    public void OnAdd(Action<T, int> callback) =>
      AddValueTypeCallback(
        ADD_CHANNEL, (in Entry entry) => callback(entry.Item, entry.Index)
      );

    /// <inheritdoc />
    public void OnUpdate(Action<T, T> callback) =>
      AddValueTypeCallback(
        UPDATE_CHANNEL,
        (in UpdateEntry entry) => callback(entry.Previous, entry.Item)
      );

    /// <inheritdoc />
    public void OnUpdate(Action<T, T, int> callback) =>
      AddValueTypeCallback(
        UPDATE_CHANNEL,
        (in UpdateEntry entry) =>
          callback(entry.Previous, entry.Item, entry.Index)
      );

    /// <inheritdoc />
    public void OnRemove(Action<T> callback) =>
      AddRefTypeCallback(REMOVE_CHANNEL, (T item) => callback(item));

    /// <inheritdoc />
    public void OnRemove(Action<T, int> callback) =>
      AddValueTypeCallback(
        REMOVE_CHANNEL, (in Entry entry) => callback(entry.Item, entry.Index)
      );

    /// <inheritdoc />
    public void OnClear(Action callback) =>
      AddValueTypeCallback(CLEAR_CHANNEL, (in int _) => callback());
  }

  /// <summary>
  /// A binding to an <see cref="AutoList{T}" /> that can be used for testing
  /// purposes.
  /// </summary>
  public interface IFakeBinding : IBinding {
    /// <summary>
    /// Simulates adding an item to the list at the specified index.
    /// </summary>
    /// <param name="item">Item to add.</param>
    /// <param name="index">Index at which to add the item.</param>
    void Add(T item, int index);

    /// <summary>
    /// Simulates updating an item in the list at the specified index.
    /// </summary>
    /// <param name="previous">Previous item.</param>
    /// <param name="item">New item.</param>
    /// <param name="index">Index at which to update the item.</param>
    void Update(T previous, T item, int index);

    /// <summary>
    /// Simulates removing an item from the list at the specified index.
    /// </summary>
    /// <param name="item">Item to remove.</param>
    /// <param name="index">Index at which to remove the item.</param>
    void Remove(T item, int index);

    /// <summary>
    /// Simulates clearing the list.
    /// </summary>
    void Clear();
  }

  /// <summary>
  /// A binding to an <see cref="AutoList{T}" /> that can be used for testing
  /// purposes.
  /// </summary>
  public sealed class FakeBinding : Binding, IFakeBinding {
    internal FakeBinding() : base(new SyncSubject()) { }

    /// <inheritdoc />
    public void Add(T item, int index) =>
      _subject?.Broadcast(new AddBroadcast(item, index));

    /// <inheritdoc />
    public void Update(T previous, T item, int index) =>
      _subject?.Broadcast(new UpdateBroadcast(previous, item, index));

    /// <inheritdoc />
    public void Remove(T item, int index) =>
      _subject?.Broadcast(new RemoveBroadcast(item, index));

    /// <inheritdoc />
    public void Clear() => _subject?.Broadcast(new ClearBroadcast());
  }

  internal readonly record struct AddBroadcast(T Item, int Index) : IBroadcast {
    public void Invoke(SyncBinding binding) {
      binding.InvokeRefTypeCallbacks(ADD_CHANNEL, Item);
      binding.InvokeValueTypeCallbacks(ADD_CHANNEL, new Entry(Item, Index));
    }
  }

  internal readonly record struct UpdateBroadcast(
    T Previous, T Item, int Index
  ) : IBroadcast {
    public void Invoke(SyncBinding binding) {
      binding.InvokeValueTypeCallbacks(
        UPDATE_CHANNEL, new UpdateEntry(Previous, Item, Index)
      );
    }
  }

  internal readonly record struct RemoveBroadcast(
    T Item, int Index
  ) : IBroadcast {
    public void Invoke(SyncBinding binding) {
      binding.InvokeRefTypeCallbacks(REMOVE_CHANNEL, Item);
      binding.InvokeValueTypeCallbacks(REMOVE_CHANNEL, new Entry(Item, Index));
    }
  }

  internal readonly record struct ClearBroadcast() : IBroadcast {
    public void Invoke(SyncBinding binding) {
      binding.InvokeValueTypeCallbacks(CLEAR_CHANNEL, 0);
    }
  }

  private readonly List<T> _list;
  private readonly SyncSubject _subject;
  private int _version;

  /// <inheritdoc />
  public IEqualityComparer<T> Comparer { get; }

  /// <summary>
  /// Creates a new observable <see cref="AutoList{T}" />.
  /// </summary>
  public AutoList() : this([]) { }

  /// <summary>
  /// Creates a new observable <see cref="AutoList{T}" /> containing the
  /// items from the provided enumerable.
  /// </summary>
  /// <param name="items">Initial items for the list.</param>
  /// <param name="comparer">
  /// Equality comparer used to determine item equality. If null, the
  /// default equality comparer for <typeparamref name="T" /> is used.
  /// </param>
  public AutoList(
    IEnumerable<T> items,
    IEqualityComparer<T>? comparer = null
  ) {
    _subject = new();
    Comparer = comparer ?? EqualityComparer<T>.Default;
    _list = [.. items];
  }

  #region Binding

  /// <summary>
  /// Creates a new <see cref="FakeBinding" /> that can be used for testing
  /// purposes.
  /// </summary>
  public static FakeBinding CreateFakeBinding() => new FakeBinding();

  /// <inheritdoc />
  public Binding Bind() => new Binding(_subject);

  #endregion Binding

  #region IList<T>

  /// <inheritdoc />
  public T this[int index] {
    get => _list[index];
    set {
      if (index < 0 || index >= _list.Count) {
        throw new ArgumentOutOfRangeException(nameof(index));
      }

      var previous = _list[index];

      if (!Comparer.Equals(previous, value)) {
        _version++;
        _list[index] = value;
        _subject.Broadcast(new UpdateBroadcast(previous, value, index));
      }
    }
  }

  /// <inheritdoc />
  public bool IsReadOnly => false;

  /// <inheritdoc />
  public void Add(T item) {
    _version++;
    _list.Add(item);
    _subject.Broadcast(new AddBroadcast(item, _list.Count - 1));
  }

  /// <inheritdoc />
  public void Clear() {
    if (_list.Count == 0) { return; }

    _version++;
    _list.Clear();
    _subject.Broadcast(new ClearBroadcast());
  }

  /// <inheritdoc />
  public bool Contains(T item) => IndexOfWithComparer(item) >= 0;

  /// <inheritdoc />
  public void CopyTo(T[] array, int arrayIndex) =>
    _list.CopyTo(array, arrayIndex);

  /// <inheritdoc />
  public int IndexOf(T item) => IndexOfWithComparer(item);

  /// <inheritdoc />
  public void Insert(int index, T item) {
    if (index < 0 || index > _list.Count) {
      throw new ArgumentOutOfRangeException(nameof(index));
    }

    _version++;
    _list.Insert(index, item);
    _subject.Broadcast(new AddBroadcast(item, index));
  }

  /// <inheritdoc />
  public bool Remove(T item) {
    var index = IndexOfWithComparer(item);

    if (index < 0) { return false; }

    _version++;

    var removedItem = _list[index];
    _list.RemoveAt(index);
    _subject.Broadcast(new RemoveBroadcast(removedItem, index));

    return true;
  }

  /// <inheritdoc />
  public void RemoveAt(int index) {
    if (index < 0 || index >= _list.Count) {
      throw new ArgumentOutOfRangeException(nameof(index));
    }

    var item = _list[index];
    _version++;
    _list.RemoveAt(index);
    _subject.Broadcast(new RemoveBroadcast(item, index));
  }

  #endregion IList<T>

  #region IReadOnlyList<T>

  /// <inheritdoc />
  public int Count => _list.Count;

  /// <inheritdoc />
  IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

  /// <inheritdoc />
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  #endregion IReadOnlyList<T>

  #region StructEnumerator
  /// <summary>
  /// Gets a struct enumerator for the <see cref="AutoList{T}"/> for efficient
  /// enumeration.
  /// </summary>
  public Enumerator GetEnumerator() => new Enumerator(this);
  #endregion StructEnumerator

  private int IndexOfWithComparer(T item) {
    for (var i = 0; i < _list.Count; i++) {
      if (Comparer.Equals(_list[i], item)) {
        return i;
      }
    }

    return -1;
  }

  /// <summary>
  /// Enumerator for the <see cref="AutoList{T}"/>.
  /// </summary>
  public struct Enumerator : IEnumerator<T> {
    private readonly AutoList<T> _list;
    private int _index;
    private readonly int _version;

    internal Enumerator(AutoList<T> list) {
      _list = list;
      _index = -1;
      _version = list._version;
    }

    /// <inheritdoc />
    public readonly T Current => _list[_index];

    readonly object? IEnumerator.Current => Current;

    /// <inheritdoc />
    public bool MoveNext() {
      if (_version != _list._version) {
        throw new InvalidOperationException(
          "AutoList collection was modified during enumeration."
        );
      }

      _index++;
      return _index < _list.Count;
    }

    /// <inheritdoc />
    public void Reset() {
      _index = -1;
    }

    /// <inheritdoc />
    public readonly void Dispose() { }
  }
}
