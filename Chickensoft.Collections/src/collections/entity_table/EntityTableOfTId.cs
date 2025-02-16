namespace Chickensoft.Collections;

using System.Collections.Concurrent;

/// <summary>
/// Very simple wrapper over a concurrent dictionary that allows entities to
/// be associated to an id and retrieved by that id as a specific type, if
/// the entity is of that type.
/// </summary>
/// <typeparam name="TId">Key type.</typeparam>
public class EntityTable<TId> where TId : notnull {
  private readonly ConcurrentDictionary<TId, object> _entities = new();

  /// <summary>
  /// Set an entity in the table.
  /// </summary>
  /// <param name="id">Entity id.</param>
  /// <param name="entity">Entity object.</param>
  public void Set(TId id, object entity) => _entities[id] = entity;

  /// <summary>
  /// Attempts to add an entity to the table returning true if successful.
  /// </summary>
  /// <param name="id">Entity id.</param>
  /// <param name="entity">Entity object.</param>
  /// <returns>`true` if the entity was added, `false` otherwise.</returns>
  public bool TryAdd(TId id, object entity) => _entities.TryAdd(id, entity);

  /// <summary>
  /// Remove an entity from the table.
  /// </summary>
  /// <param name="id">Entity id.</param>
  public void Remove(TId? id) {
    if (id is null) { return; }

    _entities.TryRemove(id, out _);
  }

  /// <summary>
  /// Clears (but does not dispose) all entities from the table.
  /// </summary>
  public void Clear() => _entities.Clear();

  /// <summary>
  /// Retrieve an entity from the table.
  /// </summary>
  /// <typeparam name="TUsage">Type to use the entity as — entity must be
  /// assignable to this type.</typeparam>
  /// <param name="id"></param>
  /// <returns>Entity with the associated id as the given type, if the entity
  /// exists and is of that type.</returns>
  public TUsage? Get<TUsage>(TId? id) where TUsage : class {
    if (
      id is not null &&
      _entities.TryGetValue(id, out var entity) &&
      entity is TUsage expected
    ) {
      return expected;
    }

    return default;
  }
}
