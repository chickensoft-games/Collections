namespace Chickensoft.Collections;

using System;
using System.Collections.Generic;

/// <summary>
/// A read-only blackboard. A blackboard is a table of data. Data is accessed by
/// its type and shared between logic block states.
/// </summary>
public interface IReadOnlyBlackboard {
  /// <summary>All types present in the blackboard.</summary>
  /// <returns>Enumerable types present in the blackboard.</returns>
  IReadOnlySet<Type> Types { get; }

  /// <summary>
  /// Gets data from the blackboard by its compile-time type.
  /// </summary>
  /// <typeparam name="TData">The type of data to retrieve.</typeparam>
  /// <exception cref="KeyNotFoundException" />
  TData Get<TData>() where TData : class;

  /// <summary>
  /// Gets data from the blackboard by its runtime type.
  /// </summary>
  /// <param name="type">Type of the data to retrieve.</param>
  /// <exception cref="KeyNotFoundException" />
  object GetObject(Type type);

  /// <summary>
  /// Determines if the logic block has data of the given type.
  /// </summary>
  /// <typeparam name="TData">The type of data to look for.</typeparam>
  /// <returns>True if the blackboard contains data for that type, false
  /// otherwise.</returns>
  bool Has<TData>() where TData : class;

  /// <summary>
  /// Determines if the logic block has data of the given type.
  /// </summary>
  /// <param name="type">The type of data to look for.</param>
  /// <returns>True if the blackboard contains data for that type, false
  /// otherwise.</returns>
  bool HasObject(Type type);
}
