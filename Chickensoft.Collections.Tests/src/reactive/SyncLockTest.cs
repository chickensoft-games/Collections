namespace Chickensoft.Collections.Tests.Reactive;

using Chickensoft.Collections.Reactive;
using Shouldly;
using Xunit;

public class SyncLockTest {
  [Fact]
  public void Works() {
    var sync = new SyncLock();

    // does nothing
    sync.Unlock();
    sync.IsLocked.ShouldBeFalse();

    // locks and unlocks
    sync.Lock();
    sync.IsLocked.ShouldBeTrue();
    sync.Unlock();
    sync.IsLocked.ShouldBeFalse();

    // counts locks
    sync.Lock();
    sync.IsLocked.ShouldBeTrue();
    sync.Lock();
    sync.IsLocked.ShouldBeTrue();
    sync.Unlock();
    sync.IsLocked.ShouldBeTrue();
    sync.Unlock();
    sync.IsLocked.ShouldBeFalse();
  }
}
