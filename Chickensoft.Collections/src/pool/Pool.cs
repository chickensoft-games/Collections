namespace Chickensoft.Collections;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

/// <summary>
/// Represents a pool of objects that can be borrowed and returned. Objects
/// are pre-instantiated and stored in the pool. When an object is borrowed,
/// it is removed from the pool. When an object is returned, it is reset and
/// placed back into the pool.
/// </summary>
public class Pool<T>
{
  private readonly ConcurrentDictionary<Type, ConcurrentQueue<T>>
    _pool = [];
  private readonly Dictionary<Type, Func<T>> _factories = [];

  /// <summary>Registers a type with the pool.</summary>
  /// <param name="capacity">The number of items to instantiate for each type
  /// registered in the pool.</param>
  /// <typeparam name="TDerived">The type to register.</typeparam>
  public void Register<TDerived>(int capacity = 1)
    where TDerived : T, new() => Register(() => new TDerived());

  /// <summary>Registers a type with the pool.</summary>
  /// <param name="factory">A factory function that creates an instance of the
  /// type to register.</param>
  /// <param name="capacity">The number of items to instantiate for each type
  /// registered in the pool.</param>
  /// <typeparam name="TDerived">The type to register.</typeparam>
  /// <exception cref="InvalidOperationException" />
  public void Register<TDerived>(
    Func<TDerived> factory, int capacity = 1
  )
  where TDerived : T
  {
    var type = typeof(TDerived);
    var queue = new ConcurrentQueue<T>();

    for (var i = 0; i < capacity; i++)
    {
      var item = factory();
      queue.Enqueue(item);
    }

    lock (_pool)
    {
      if (!_pool.TryAdd(type, queue))
      {
        throw new InvalidOperationException(
          $"Type `{type}` is already registered."
        );
      }

      _factories.TryAdd(type, () => factory());
    }
  }

  /// <summary>Borrows an object from the pool.</summary>
  /// <typeparam name="TDerived">The type of object to borrow.</typeparam>
  /// <returns>An object of the specified type.</returns>
  public TDerived Get<TDerived>() where TDerived : T, new()
    => (TDerived)Get(typeof(TDerived))!;

  /// <summary>Borrows an object from the pool.</summary>
  /// <param name="type">The type of object to borrow.</param>
  /// <returns>An object of the specified type.</returns>
  public T Get(Type type)
  {
    if (!_pool.TryGetValue(type, out var queue))
    {
      throw new InvalidOperationException($"Type `{type}` is not registered.");
    }

    if (queue.TryDequeue(out var item))
    {
      return item;
    }

    // Out of values. Just make one.
    return _factories[type]();
  }

  /// <summary>Returns an object to the pool.</summary>
  /// <param name="item">The object to return.</param>
  /// <exception cref="InvalidOperationException" />
  public void Return(T item)
  {
    if (item is null)
    { return; }

    var type = item.GetType();

    if (!_pool.TryGetValue(type, out var queue))
    {
      throw new InvalidOperationException($"Type `{type}` is not registered.");
    }

    if (item is IPooled pooled)
    {
      pooled.Reset();
    }

    queue.Enqueue(item);
  }
}
