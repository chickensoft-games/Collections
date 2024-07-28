namespace Chickensoft.Collections.Tests;

using System;
using Shouldly;
using Xunit;

public class PoolTest {
  public abstract class Shape : IPooled {
    public void Reset() { }
  }

  public class Cube : Shape { }
  public class Sphere : Shape { }

  [Fact]
  public void InitializesAndRegisters() {
    var pool = new Pool<Shape>();

    pool.Register<Cube>();
    pool.Register<Sphere>();

    pool.Get<Cube>().ShouldBeOfType<Cube>();
    pool.Get<Sphere>().ShouldBeOfType<Sphere>();
  }

  [Fact]
  public void ThrowsIfRegisteringDuplicateType() {
    var pool = new Pool<Shape>();

    pool.Register<Cube>();

    Should.Throw<InvalidOperationException>(() => pool.Register<Cube>(1));
  }

  [Fact]
  public void ThrowsIfGettingUnregisteredType() {
    var pool = new Pool<Shape>();

    Should.Throw<InvalidOperationException>(pool.Get<Cube>);
  }

  [Fact]
  public void CreatesNewInstancesIfPoolIsEmpty() {
    var pool = new Pool<Shape>();

    pool.Register<Cube>();

    var cube1 = pool.Get<Cube>();
    var cube2 = pool.Get<Cube>();

    cube1.ShouldNotBeSameAs(cube2);
  }

  [Fact]
  public void ReturnThrowsIfTypeNotRegistered() {
    var pool = new Pool<Shape>();

    Should.Throw<InvalidOperationException>(() => pool.Return(new Cube()));
  }

  [Fact]
  public void ReturnResetsAndEnqueues() {
    var pool = new Pool<Shape>();

    pool.Register<Cube>();

    var cube = pool.Get<Cube>();

    pool.Return(cube);

    var cube2 = pool.Get<Cube>();

    cube.ShouldBeSameAs(cube2);
  }
}
