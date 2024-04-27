namespace Chickensoft.Collections;

using System;
using System.Collections.Generic;
using System.Data;

/// <summary>
/// A blackboard is a table of data. Data is accessed by its type and shared
/// between logic block states.
/// </summary>
public interface IBlackboard : IReadOnlyBlackboard {
  /// <summary>
  /// Adds data to the blackboard so that it can be looked up by its
  /// compile-time type.
  /// <br />
  /// Data is retrieved by its type, so do not add more than one piece of data
  /// with the same type.
  /// </summary>
  /// <param name="data">Data to write to the blackboard.</param>
  /// <typeparam name="TData">Type of the data to add.</typeparam>
  /// <exception cref="KeyNotFoundException" />
  void Set<TData>(TData data) where TData : class;
  /// <summary>
  /// Adds data to the blackboard and associates it with a specific type.
  /// <br />
  /// Data is retrieved by its type, so do not add more than one piece of data
  /// with the same type.
  /// </summary>
  /// <param name="type">Type of the data.</param>
  /// <param name="data">Data to write to the blackboard.</param>
  /// <exception cref="DuplicateNameException" />
  void SetObject(Type type, object data);
  /// <summary>
  /// Adds new data or overwrites existing data in the blackboard, based on
  /// its compile-time type.
  /// <br />
  /// Data is retrieved by its type, so this will overwrite any existing data
  /// of the given type, unlike <see cref="Set{TData}(TData)" />.
  /// </summary>
  /// <param name="data">Data to write to the blackboard.</param>
  /// <typeparam name="TData">Type of the data to add or overwrite.</typeparam>
  void Overwrite<TData>(TData data) where TData : class;
  /// <summary>
  /// Adds new data or overwrites existing data in the blackboard and associates
  /// it with a specific type.
  /// <br />
  /// Data is retrieved by its type, so this will overwrite any existing data
  /// of the given type, unlike <see cref="Set{TData}(TData)" />.
  /// </summary>
  /// <param name="type">Type of the data.</param>
  /// <param name="data">Data to write to the blackboard.</param>
  void OverwriteObject(Type type, object data);
}
