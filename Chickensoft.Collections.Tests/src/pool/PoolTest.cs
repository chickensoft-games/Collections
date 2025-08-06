namespace Chickensoft.Collections.Tests;

using Chickensoft.Collections;
using Shouldly;
using Xunit;

public class PoolTest {
  private sealed class MyObject { }

  private sealed class MyPooledObject : IPooled {
    public bool WasReset;
    public void Reset() => WasReset = true;
  }

  [Fact]
  public void CreateWithDefaultConstructor_InitializesStack() {
    var pool = Pool.Create<MyObject>(3);
    pool.InitialCapacity.ShouldBe(3);
    pool.Capacity.ShouldBe(3);
    pool.Available.ShouldBe(3);
    pool.Borrowed.ShouldBe(0);

    // borrow everything
    var d1 = pool.Borrow();
    var d2 = pool.Borrow();
    var d3 = pool.Borrow();
    pool.Available.ShouldBe(0);
    pool.Borrowed.ShouldBe(3);

    // ensure unique instances
    d1.ShouldNotBeSameAs(d2);
    d2.ShouldNotBeSameAs(d3);
  }

  [Fact]
  public void CreateWithFactoryRespectsFactory() {
    MyPooledObject factory() => new MyPooledObject { WasReset = false };
    var pool = Pool.Create(2, factory);

    pool.InitialCapacity.ShouldBe(2);
    pool.Capacity.ShouldBe(2);
    pool.Available.ShouldBe(2);

    var p1 = pool.Borrow();
    var p2 = pool.Borrow();

    p1.ShouldBeOfType<MyPooledObject>();
    p2.ShouldBeOfType<MyPooledObject>();
    p1.ShouldNotBeSameAs(p2);
  }

  [Fact]
  public void BorrowBeyondCapacityGrowsPool() {
    var pool = Pool.Create<MyObject>(2);
    pool.Capacity.ShouldBe(2);

    // grow pool by borrowing more than initial capacity
    var a = pool.Borrow();
    var b = pool.Borrow();
    var c = pool.Borrow();

    pool.Capacity.ShouldBe(4); // capacity doubles
    pool.Available.ShouldBe(1);
    pool.Borrowed.ShouldBe(3);
  }

  [Fact]
  public void ReturnNonBorrowedReturnsFalse() {
    var pool = Pool.Create<MyObject>(1);
    var outside = new MyObject();
    pool.Return(outside).ShouldBeFalse();
  }

  [Fact]
  public void ReturnIPooledResetsAndReEnqueues() {
    var pool = Pool.Create(1, () => new MyPooledObject());
    var item = pool.Borrow();
    pool.Borrowed.ShouldBe(1);
    pool.Available.ShouldBe(0);

    item.WasReset = false;
    var returned = pool.Return(item);

    returned.ShouldBeTrue();
    item.WasReset.ShouldBeTrue();
    pool.Available.ShouldBe(1);
    pool.Borrowed.ShouldBe(0);

    // Borrowing again should yield the same object
    var again = pool.Borrow();
    again.ShouldBeSameAs(item);
  }

  [Fact]
  public void BorrowAsObjectAndReturnAsObjectWorkProperly() {
    var pool = Pool.Create<MyObject>(1);
    var o = pool.BorrowAsObject();
    o.ShouldBeOfType<MyObject>();

    pool.ReturnAsObject(o).ShouldBeTrue();
    pool.Available.ShouldBe(1);
    pool.Borrowed.ShouldBe(0);
  }

  [Fact]
  public void EnsureCapacityGrowsButDoesNotShrink() {
    var pool = Pool.Create<MyObject>(2);
    pool.Capacity.ShouldBe(2);

    pool.EnsureCapacity(5);
    pool.Capacity.ShouldBe(5);
    pool.Available.ShouldBe(5);
    pool.Borrowed.ShouldBe(0);

    // calling with a smaller capacity does nothing
    pool.EnsureCapacity(3);
    pool.Capacity.ShouldBe(5);
  }

  [Fact]
  public void TrimExcessShrinksCorrectly() {
    var pool = Pool.Create<MyObject>(4);
    var items = new MyObject[5];

    // force growth
    for (var i = 0; i < 5; i++) {
      items[i] = pool.Borrow();
    }
    pool.Capacity.ShouldBe(8);
    pool.Borrowed.ShouldBe(5);
    pool.Available.ShouldBe(3);

    // return 3 items: Borrowed = 2, Available = 6
    pool.Return(items[0]);
    pool.Return(items[1]);
    pool.Return(items[2]);
    pool.Borrowed.ShouldBe(2);
    pool.Available.ShouldBe(6);

    pool.TrimExcess();

    pool.Capacity.ShouldBe(4);
    pool.Available.ShouldBe(4);
    pool.Borrowed.ShouldBe(2);
  }

  [Fact]
  public void AutoPoolSharedIsSingletonWithDefaultCapacity() {
    var a1 = AutoPool<object>.Shared;
    var a2 = AutoPool<object>.Shared;
    a1.ShouldBeSameAs(a2);

    a1.InitialCapacity.ShouldBe(Pool.DEFAULT_INITIAL_CAPACITY);
    a1.Capacity.ShouldBe(Pool.DEFAULT_INITIAL_CAPACITY);
    a1.Available.ShouldBe(Pool.DEFAULT_INITIAL_CAPACITY);
    a1.Borrowed.ShouldBe(0);

    var s = a1.Borrow();
    a1.Borrowed.ShouldBe(1);
    a1.Return(s).ShouldBeTrue();
  }
}
