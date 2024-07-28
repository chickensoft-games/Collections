namespace Chickensoft.Collections;

/// <summary>
/// Represents a type that can be stored in a pool. Pools maintain
/// pre-instantiated instances that can be borrowed and returned to avoid
/// excess memory allocations. Pooled objects are "reset" whenever they are
/// returned to the pool so that they can be reused.
/// </summary>
public interface IPooled {
  /// <summary>Resets the object to its default state.</summary>
  void Reset();
}
