namespace Chickensoft.Collections;

using System;

internal class PooledLinkedList<T> {
  private PooledLinkedListNode<T>? _head;
  private PooledLinkedListNode<T>? _tail;
  private int _count;

  public int Count => _count;
  public PooledLinkedListNode<T>? First => _head;
  public PooledLinkedListNode<T>? Last => _tail;

  private Pool<PooledLinkedListNode<T>> Pool =>
    AutoPool<PooledLinkedListNode<T>>.Shared;

  public PooledLinkedListNode<T> AddFirst(T value) {
    var node = Pool.Borrow();

    node.Value = value;

    if (_head is null) {
      _head = node;
      _tail = node;
    }
    else {
      node.Next = _head;
      _head.Previous = node;
      _head = node;
    }

    _count++;

    return node;
  }

  public PooledLinkedListNode<T> AddLast(T value) {
    var node = Pool.Borrow();

    node.Value = value;

    if (_tail is null) {
      _head = node;
      _tail = node;
    }
    else {
      node.Previous = _tail;
      _tail.Next = node;
      _tail = node;
    }

    _count++;

    return node;
  }

  public T Remove(PooledLinkedListNode<T> node) {
    if (node.Previous is not null) {
      node.Previous.Next = node.Next;
    }
    else {
      _head = node.Next;
    }

    if (node.Next is not null) {
      node.Next.Previous = node.Previous;
    }
    else {
      _tail = node.Previous;
    }

    var value = node.Value;

    // node will be reset when returned to the pool, which will nullify its
    // properties
    Pool.Return(node);

    if (_count > 0) {
      _count--;
    }

    return value;
  }

  public void Clear() {
    var current = _head;
    while (current is not null) {
      var next = current.Next;
      Pool.Return(current);
      current = next;
    }

    _head = null;
    _tail = null;
    _count = 0;
  }

  public void CopyTo(T[] array, int arrayIndex) {
    if (arrayIndex < 0 || arrayIndex + _count > array.Length) {
      throw new ArgumentOutOfRangeException(
        nameof(arrayIndex),
        "Array index is out of range."
      );
    }

    var current = _head;
    for (var i = 0; i < _count; i++) {
      array[arrayIndex + i] = current!.Value;
      current = current.Next;
    }
  }

  public Enumerator GetEnumerator() {
    return new Enumerator(_head);
  }

  public EnumeratorReversed Reversed() {
    return new EnumeratorReversed(_tail);
  }

  public struct Enumerator {
    private PooledLinkedListNode<T>? _current;
    private readonly PooledLinkedListNode<T>? _head;

    public Enumerator(PooledLinkedListNode<T>? head) {
      _head = head;
      _current = null;
    }

    public readonly PooledLinkedListNode<T> Current => _current!;

    public bool MoveNext() {
      if (_current == null) {
        _current = _head;
      }
      else {
        _current = _current.Next;
      }
      return _current is not null;
    }
  }

  public struct EnumeratorReversed {
    private PooledLinkedListNode<T>? _current;
    private readonly PooledLinkedListNode<T>? _tail;

    public EnumeratorReversed(PooledLinkedListNode<T>? tail) {
      _tail = tail;
      _current = null;
    }

    public readonly PooledLinkedListNode<T> Current => _current!;

    public bool MoveNext() {
      if (_current == null) {
        _current = _tail;
      }
      else {
        _current = _current.Previous;
      }
      return _current is not null;
    }

    public readonly EnumeratorReversed GetEnumerator() => this;
  }
}
