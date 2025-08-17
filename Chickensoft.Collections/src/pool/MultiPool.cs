namespace Chickensoft.Collections;

using System;
using System.Collections.Generic;

/// <summary>
/// <para>
/// Represents a pool of objects which inherit from the common base type
/// <typeparamref name="TBase" />. Under the hood, each individual derived type
/// has its own pool. Pools automatically grow as needed, but the initial
/// capacity can be specified at the time of creation.
/// </para>
/// <para>
/// Each derived type must be registered with the pool before objects can be
/// borrowed from it or returned. Use the
/// <see cref="Register{TDerived}(Func{TDerived})" /> or
/// <see cref="Register{TDerived}()" /> methods to register a derived type with
/// the pool.
/// </para>
/// <para>
/// For more information, see <see cref="Pool{T}" />.
/// </para>
/// </summary>
/// <typeparam name="TBase"></typeparam>
public sealed class MultiPool<TBase> where TBase : class {
  /// <summary>Initial capacity for each derived type.</summary>
  public int InitialCapacity { get; }

  private readonly Dictionary<Type, Pool> _pools = [];

  /// <summary>
  /// Creates a new <see cref="MultiPool{TBase}"/> with the specified
  /// initial capacity for each derived type.
  /// </summary>
  /// <param name="initialCapacity">Initial capacity for every derived type
  /// of <typeparamref name="TBase" /> that will be registered with the pool.
  /// </param>
  public MultiPool(int initialCapacity = Pool.DEFAULT_INITIAL_CAPACITY) {
    InitialCapacity = initialCapacity;
  }

  /// <summary>
  /// Creates a new <see cref="MultiPool{TBase}"/> with the default
  /// initial capacity for each derived type.
  /// </summary>
  public MultiPool() : this(Pool.DEFAULT_INITIAL_CAPACITY) { }

  /// <summary>
  /// Registers a derived type with the pool using a factory function
  /// that creates new instances of the type.
  /// </summary>
  /// <typeparam name="TDerived">Derived type.</typeparam>
  /// <param name="factory">Factory function.</param>
  public void Register<TDerived>(Func<TDerived> factory)
      where TDerived : class, TBase {
    var type = typeof(TDerived);

    if (_pools.TryGetValue(type, out var pool)) {
      return;
    }

    pool = Pool.Create(InitialCapacity, factory);
    _pools[type] = pool;
  }

  /// <summary>
  /// Registers a derived type with the pool using its default
  /// parameterless constructor.
  /// </summary>
  /// <typeparam name="TDerived">Derived type.</typeparam>
  public void Register<TDerived>()
      where TDerived : class, TBase, new() {
    Register(static () => new TDerived());
  }

  /// <summary>
  /// Borrows an object of the specified derived type from the pool.
  /// </summary>
  /// <typeparam name="TDerived">Derived type.</typeparam>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  public TDerived Borrow<TDerived>()
      where TDerived : class, TBase {
    var type = typeof(TDerived);

    if (!_pools.TryGetValue(type, out var pool)) {
      throw new InvalidOperationException(
        $"Type '{type.Name}' is not registered in the pool."
      );
    }

    return (TDerived)pool.BorrowAsObject();
  }

  /// <summary>
  /// Returns an object of the specified derived type to the pool.
  /// </summary>
  /// <param name="item">Item to return.</param>
  /// <returns>True if the item belonged to the pool and was returned, false
  /// otherwise.</returns>
  public bool Return(TBase item) {
    var type = item.GetType();

    if (!_pools.TryGetValue(type, out var pool)) {
      return false;
    }

    return pool.ReturnAsObject(item);
  }
}
