namespace Chickensoft.Collections;

using System;
using System.Collections.Generic;

/// <summary>
/// A static class that provides access to a shared instance of a pool for
/// objects of type <typeparamref name="T"/>. This pool can be used
/// to borrow and return objects of the specified type without needing to
/// always allocate a new instance on the heap.
/// </summary>
/// <typeparam name="T">Type of element to pool.</typeparam>
public static class AutoPool<T> where T : class, new() {
  /// <summary>
  /// A shared instance of the <see cref="Pool{T}"/>.
  /// </summary>
  public static Pool<T> Shared =>
    _shared ??= Pool.Create<T>(Pool.DEFAULT_INITIAL_CAPACITY);

  [ThreadStatic]
  private static Pool<T>? _shared;
}

/// <summary>
/// A static class that provides methods to create and manage pools of
/// objects. This class allows you to create pools with a specified initial
/// capacity and a factory function to create new instances of the type.
/// </summary>
public abstract class Pool {
  /// <summary>
  /// Default initial capacity for an automatically (statically) initialized
  /// pool.
  /// </summary>
  public const int DEFAULT_INITIAL_CAPACITY = 64;

  /// <summary>
  /// Creates a new <see cref="Pool{T}"/> with the specified initial
  /// capacity and factory function. The factory function is used to create
  /// new instances of the type when needed. Use this method to create a sync
  /// pool for objects without default parameterless constructors.
  /// </summary>
  /// <typeparam name="T">Type of object.</typeparam>
  /// <param name="initialCapacity">Initial number of objects to make.</param>
  /// <param name="factory">Factory function which generates a new object.
  /// </param>
  /// <returns>A <see cref="Pool{T}"/>.</returns>
  public static Pool<T> Create<T>(
    int initialCapacity, Func<T> factory
  ) where T : class {
    return new Pool<T>(initialCapacity, factory);
  }

  /// <summary>
  /// Creates a new <see cref="Pool{T}"/> with the specified initial
  /// capacity and a default factory function that creates new instances
  /// of the type using its default parameterless constructor.
  /// </summary>
  /// <typeparam name="T">Type of object.</typeparam>
  /// <param name="initialCapacity">Initial number of objects to make.</param>
  /// <returns>A <see cref="Pool{T}"/>.</returns>
  public static Pool<T> Create<T>(
    int initialCapacity
  ) where T : class, new() {
    return new Pool<T>(initialCapacity, static () => new T());
  }

  /// <summary>
  /// Borrows an object from the pool.
  /// </summary>
  /// <returns>Object borrowed from the pool.</returns>
  public abstract object BorrowAsObject();

  /// <summary>
  /// Returns an object to the pool.
  /// </summary>
  /// <param name="item">Object to return.</param>
  public abstract bool ReturnAsObject(object item);
}

/// <summary>
/// <para>
/// Represents a pool of objects that can be borrowed and returned. Objects
/// are pre-instantiated and stored in the pool.
/// </para>
/// <para>
/// The pool will grow automatically if more objects are requested, doubling its
/// capacity each time. Internally, the pool uses a C# <see cref="Stack{T}"/>
/// and a <see cref="HashSet{T}"/> as its backing store, both of which
/// efficiently resize themselves under-the-hood when needed. You may call
/// <see cref="TrimExcess"/> to reduce the pool's size to reclaim memory (if
/// desired).
/// </para>
/// <para>
/// Objects which implement <see cref="IPooled"/> will have their
/// <see cref="IPooled.Reset"/> method called when returned to the pool.
/// </para>
/// <para>
/// Using a pool minimizes memory allocations and helps reduce garbage
/// collection churn for objects which would otherwise be frequently created
/// and destroyed. By tuning the initial capacity, you can optimize the pool
/// for your specific use case.
/// </para>
/// <para>
/// To create a pool, use the static <see cref="Pool.Create{T}(int, Func{T})"/>
/// or <see cref="Pool.Create{T}(int)"/> methods.
/// </para>
/// <para>
/// To obtain an object instance from the pool, simply call
/// <see cref="Pool{T}.Borrow"/>. When you are finished with the object, call
/// <see cref="Pool{T}.Return"/> to return it to the pool.
/// </para>
/// </summary>
public sealed class Pool<T> : Pool where T : class {
  /// <summary>Initial capacity of the pool.</summary>
  public int InitialCapacity { get; }

  /// <summary>
  /// Factory function used to create new instances of the type when needed.
  /// </summary>
  public Func<T> Factory { get; }

  /// <summary>
  /// Number of objects currently available to be borrowed from the pool.
  /// </summary>
  public int Available => _stack.Count;

  /// <summary>
  /// Total number of objects the pool is managing, including those that are
  /// currently borrowed.
  /// </summary>
  public int Capacity => _capacity;

  /// <summary>
  /// Number of objects currently borrowed from the pool.
  /// </summary>
  public int Borrowed => _borrowed.Count;

  private readonly HashSet<T> _borrowed;
  private readonly Stack<T> _stack;
  private int _capacity;

  internal Pool(int initialCapacity, Func<T> factory) {
    Factory = factory;
    InitialCapacity = initialCapacity;

    _stack = new Stack<T>(initialCapacity);
    _borrowed = new HashSet<T>(
      capacity: initialCapacity,
      // use reference equality for borrowed items
      comparer: ReferenceComparer<T>.Default
    );
    _capacity = initialCapacity;

    for (var i = 0; i < initialCapacity; i++) {
      _stack.Push(Factory());
    }
  }

  /// <summary>Borrows an object from the pool.</summary>
  /// <returns>An object of the specified type.</returns>
  public T Borrow() {
    if (!_stack.TryPop(out var item)) {
      // out of values — make more
      EnsureCapacity(_capacity * 2);
      item = _stack.Pop();
    }

    _borrowed.Add(item);
    return item;
  }

  /// <summary>
  /// Ensures that the pool has at least the specified number of objects
  /// available to be borrowed.
  /// </summary>
  /// <param name="capacity">Capacity.</param>
  public void EnsureCapacity(int capacity) {
    if (capacity <= _capacity) {
      return;
    }

    _capacity = capacity;

    var itemsToAdd = _capacity - (_stack.Count + _borrowed.Count);

    while (itemsToAdd-- > 0) {
      var newItem = Factory();
      _stack.Push(newItem);
    }

    _borrowed.EnsureCapacity(_capacity);
  }

  /// <summary>Returns an object to the pool. If the object is not borrowed
  /// from the pool, nothing happens.</summary>
  /// <param name="item">The object to return.</param>
  /// <returns>True if the object was successfully returned, false if it was not
  /// borrowed from the pool.</returns>
  public bool Return(T item) {
    if (!_borrowed.Contains(item)) {
      return false;
    }

    if (item is IPooled pooled) {
      pooled.Reset();
    }

    _borrowed.Remove(item);

    _stack.Push(item);

    return true;
  }

  /// <summary>
  /// Free up memory by trimming the pool to the smallest size that it can
  /// support without losing any borrowed items or shrinking below its
  /// initial capacity.
  /// </summary>
  public void TrimExcess() {
    // cannot shrink below the amount of items currently in use
    var size = Math.Max(Borrowed, InitialCapacity);

    while (_stack.Count > size) {
      _stack.Pop();
    }

    _capacity = size;

    _stack.TrimExcess();
    _borrowed.TrimExcess();
  }

  /// <inheritdoc />
  public override object BorrowAsObject() => Borrow();

  /// <inheritdoc />
  public override bool ReturnAsObject(object item) => Return((T)item);
}
