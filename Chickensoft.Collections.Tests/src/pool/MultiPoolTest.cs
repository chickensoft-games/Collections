namespace Chickensoft.Collections.Tests;

using System;
using Shouldly;
using Xunit;

public class MultiPoolTest {
  public abstract class Shape : IPooled {
    public void Reset() { }
  }

  public class Cube : Shape { }
  public class Sphere : Shape { }

  [Fact]
  public void InitializesAndRegisters() {
    var pool = new MultiPool<Shape>();

    pool.Register<Cube>();
    pool.Register<Sphere>();

    pool.Borrow<Cube>().ShouldBeOfType<Cube>();
    pool.Borrow<Sphere>().ShouldBeOfType<Sphere>();
  }

  [Fact]
  public void DoesNothingIfRegisteringDuplicateType() {
    var pool = new MultiPool<Shape>();

    pool.Register<Cube>();

    Should.NotThrow(pool.Register<Cube>);
  }

  [Fact]
  public void ThrowsIfGettingUnregisteredType() {
    var pool = new MultiPool<Shape>();

    Should.Throw<InvalidOperationException>(pool.Borrow<Cube>);
  }

  [Fact]
  public void CreatesNewInstancesIfPoolIsEmpty() {
    var pool = new MultiPool<Shape>();

    pool.Register<Cube>();

    var cube1 = pool.Borrow<Cube>();
    var cube2 = pool.Borrow<Cube>();

    cube1.ShouldNotBeSameAs(cube2);
  }

  [Fact]
  public void ReturnsFalseOnReturnIfTypeNotRegistered() {
    var pool = new MultiPool<Shape>();

    pool.Return(new Cube()).ShouldBeFalse();
  }

  [Fact]
  public void ReturnResetsAndEnqueues() {
    var pool = new MultiPool<Shape>();

    pool.Register<Cube>();

    var cube = pool.Borrow<Cube>();

    pool.Return(cube);

    var cube2 = pool.Borrow<Cube>();

    cube.ShouldBeSameAs(cube2);

    pool.Return(cube2);
  }
}
