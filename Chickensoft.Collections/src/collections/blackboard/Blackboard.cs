namespace Chickensoft.Collections;

using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;

/// <summary><inheritdoc cref="IBlackboard" /></summary>
public class Blackboard : IBlackboard {
  /// <summary>Blackboard data storage.</summary>
  protected readonly Dictionary<Type, object> _blackboard = [];
  /// <summary>Type of objects present in the blackboard.</summary>
  protected readonly Set<Type> _types = [];

  /// <summary>
  /// Creates a new blackboard. <inheritdoc cref="Blackboard" />
  /// </summary>
  public Blackboard() { }

  #region IReadOnlyBlackboard
  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public TData Get<TData>() where TData : class {
    var type = typeof(TData);
    return (TData)GetBlackboardData(type);
  }

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public object GetObject(Type type) => GetBlackboardData(type);

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool Has<TData>() where TData : class => HasObject(typeof(TData));

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool HasObject(Type type) => _blackboard.ContainsKey(type);
  #endregion IReadOnlyBlackboard

  /// <inheritdoc />
  #region Blackboard
  public IReadOnlySet<Type> Types => _types;

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Set<TData>(TData data) where TData : class {
    var type = typeof(TData);
    _types.Add(type);
    SetBlackboardData(type, data);
  }

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void SetObject(Type type, object data) {
    _types.Add(type);
    SetBlackboardData(type, data);
  }

  /// <inheritdoc />
  public void Overwrite<TData>(TData data) where TData : class {
    var type = typeof(TData);
    _types.Add(type);
    OverwriteBlackboardData(type, data);
  }

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void OverwriteObject(Type type, object data) {
    _types.Add(type);
    OverwriteBlackboardData(type, data);
  }
  #endregion Blackboard

  /// <summary>
  /// Underlying method to get data from the blackboard.
  /// </summary>
  /// <param name="type">Type of data to get.</param>
  /// <returns>Blackboard data.</returns>
  /// <exception cref="KeyNotFoundException" />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  protected virtual object GetBlackboardData(Type type) =>
    _blackboard.TryGetValue(type, out var data)
      ? data
      : throw new KeyNotFoundException(
        $"Data of type {type} not found in the blackboard. (You may be missing the [Meta] attribute.)"
      );

  /// <summary>
  /// Underlying method to set data in the blackboard.
  /// </summary>
  /// <param name="type">Type of data to set.</param>
  /// <param name="data">Blackboard data.</param>
  /// <exception cref="DuplicateNameException" />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  protected virtual void SetBlackboardData(Type type, object data) {
    if (!_blackboard.TryAdd(type, data)) {
      throw new DuplicateNameException(
        $"Data of type {type} already exists in the blackboard."
      );
    }
  }

  /// <summary>
  /// Underlying method to overwrite data in the blackboard.
  /// </summary>
  /// <param name="type">Type of data to overwrite.</param>
  /// <param name="data">Blackboard data.</param>
  protected virtual void OverwriteBlackboardData(Type type, object data) =>
    _blackboard[type] = data;
}
