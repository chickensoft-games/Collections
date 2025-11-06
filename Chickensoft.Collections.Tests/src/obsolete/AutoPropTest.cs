namespace Chickensoft.Collections.Tests;

using System;
using System.Collections.Generic;
using Chickensoft.Collections;
using Shouldly;
using Xunit;


#pragma warning disable CS0618 // Type or member is obsolete
public class AutoPropTest
{
  public static class Utils
  {
    public static void ClearWeakReference(WeakReference weakReference)
    {
      weakReference.Target = null;
      GC.Collect();
      GC.WaitForPendingFinalizers();
    }
  }

  public static WeakReference CreateWeakReference() => new(
    new AutoProp<int>(1)
  );

  [Fact]
  public void Initializes()
  {
    var subject = new AutoProp<int>(1);
    subject.Value.ShouldBe(1);
  }

  [Fact]
  public void InitializesWithComparer()
  {
    var subject = new AutoProp<int>(1, EqualityComparer<int>.Default);
    subject.Value.ShouldBe(1);
    subject.Comparer.ShouldBe(EqualityComparer<int>.Default);
  }

  [Fact]
  public void SyncCallsHandlerImmediatelyAndAllowsUnsubscribe()
  {
    using var subject = new AutoProp<int>(1);

    var changedCalled = 0;
    var syncCalled = 0;

    void onSync(int value)
    {
      value.ShouldBe(1);
      syncCalled++;
    }

    subject.Changed += (value) => changedCalled++;
    subject.Sync += onSync;

    changedCalled.ShouldBe(0);
    syncCalled.ShouldBe(1);

    subject.Sync -= onSync;
    subject.OnNext(2);

    changedCalled.ShouldBe(1);
  }

  [Fact]
  public void ClearsEventHandlers()
  {
    using var subject = new AutoProp<int>(1);

    var changedCalled = 0;
    var syncCalled = 0;
    var completedCalled = 0;
    var errorCalled = 0;

    subject.Changed += (value) => changedCalled++;
    subject.Sync += (value) => syncCalled++;
    subject.Completed += () => completedCalled++;
    subject.Error += (exception) => errorCalled++;

    subject.Clear();

    subject.OnNext(2);
    subject.OnCompleted();
    subject.OnError(new InvalidOperationException());

    changedCalled.ShouldBe(0);
    syncCalled.ShouldBe(1);
    completedCalled.ShouldBe(0);
    errorCalled.ShouldBe(0);
  }

  [Fact]
  public void CompletesAndBlocksOtherActions()
  {
    using var subject = new AutoProp<int>(1);

    var changedCalled = 0;
    var syncCalled = 0;
    var completedCalled = 0;
    var errorCalled = 0;

    subject.Changed += (value) => changedCalled++;
    subject.Sync += (value) => syncCalled++;
    subject.Completed += () => completedCalled++;
    subject.Error += (exception) => errorCalled++;

    subject.OnCompleted();
    subject.OnCompleted();
    subject.OnNext(2);
    subject.Value.ShouldBe(1);
    subject.OnError(new InvalidOperationException());

    changedCalled.ShouldBe(0);
    syncCalled.ShouldBe(1);
    completedCalled.ShouldBe(1);
    errorCalled.ShouldBe(0);
  }

  [Fact]
  public void CallsErrorHandler()
  {
    using var subject = new AutoProp<int>(1);

    var errorCalled = 0;

    void onError(Exception exception)
    {
      exception.ShouldBeOfType<InvalidOperationException>();
      errorCalled++;
    }

    subject.Error += onError;

    subject.OnError(new InvalidOperationException());
    errorCalled.ShouldBe(1);

    subject.Error -= onError;

    subject.OnError(new InvalidOperationException());
    errorCalled.ShouldBe(1);
  }

  [Fact]
  public void DisposesCorrectly()
  {
    var subject = new AutoProp<int>(1);

    var changedCalled = 0;
    var syncCalled = 0;
    var completedCalled = 0;
    var errorCalled = 0;

    subject.Changed += (value) => changedCalled++;
    subject.Sync += (value) => syncCalled++;
    subject.Completed += () => completedCalled++;
    subject.Error += (exception) => errorCalled++;

    subject.Dispose();

    subject.OnNext(2);
    subject.OnCompleted();
    subject.OnError(new InvalidOperationException());
    subject.Dispose();

    changedCalled.ShouldBe(0);
    syncCalled.ShouldBe(1);
    completedCalled.ShouldBe(0);
    errorCalled.ShouldBe(0);
  }

  [Fact]
  public void Finalizes()
  {
    // Weak reference has to be created and cleared from a static function
    // or else the GC won't ever collect it :P
    var subject = CreateWeakReference();
    Utils.ClearWeakReference(subject);
  }

  [Fact]
  public void DoesNotCallHandlersIfValueHasNotChanged()
  {
    var subject = new AutoProp<int>(1);

    var changedCalled = 0;
    var syncCalled = 0;

    subject.Changed += (value) => changedCalled++;
    subject.Sync += (value) => syncCalled++;

    subject.OnNext(2);
    subject.OnNext(2);

    changedCalled.ShouldBe(1);
    syncCalled.ShouldBe(2);
  }

  [Fact]
  public void DoesNotCallHandlerWhileInsideHandler()
  {
    var subject = new AutoProp<int>(1);

    var changes = new List<int>();
    var syncs = new List<int>();

    subject.Changed += (value) =>
    {
      changes.Add(value);
      subject.OnNext(3);
    };
    subject.Sync += syncs.Add;

    subject.OnNext(2);

    changes.ShouldBe([2, 3]);
    syncs.ShouldBe([1, 2, 3]);
  }
}
#pragma warning restore CS0618 // Type or member is obsolete
