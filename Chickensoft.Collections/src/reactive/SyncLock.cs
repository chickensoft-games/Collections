namespace Chickensoft.Collections.Reactive;

/// <summary>
/// An extremely simple "busy" counter that assists in reentrance detection for
/// common, single-threaded synchronous observable patterns.
/// </summary>
public sealed class SyncLock {
  private int _lockCount;

  /// <summary>
  /// True if the lock is currently held.
  /// </summary>
  public bool IsLocked => _lockCount > 0;

  /// <summary>
  /// Locks the lock and returns the new lock count. For each invocation, the
  /// internal lock count is incremented by one. The lock must be unlocked
  /// the same number of times it was locked to return to an unlocked state.
  /// </summary>
  /// <returns>Number of locks held afterwards.</returns>
  public int Lock() => ++_lockCount;

  /// <summary>
  /// Unlocks the lock and returns the new lock count. If the lock count is
  /// already zero, it remains zero.
  /// </summary>
  /// <returns>Number of locks held afterwards.</returns>
  public int Unlock() {
    if (_lockCount <= 0) {
      return _lockCount = 0;
    }

    return --_lockCount;
  }
}
