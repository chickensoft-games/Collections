namespace Chickensoft.Collections.Tests;

using Shouldly;
using Xunit;

public class EntityTableTest {
  [Fact]
  public void Initializes() {
    new EntityTable<int>().ShouldBeAssignableTo<EntityTable<int>>();
    new EntityTable().ShouldBeAssignableTo<EntityTable<string>>();
  }

  [Fact]
  public void StoresValues() {
    var table = new EntityTable();

    table.Set("a", "one");
    table.Set("b", new object());

    table.Get<string>("a").ShouldBe("one");
    table.Get<object>("b").ShouldNotBeNull();

    table.Remove("a");
    table.Remove(null);

    table.Get<string>("a").ShouldBeNull();
    table.Get<object>("b").ShouldNotBeNull();
  }
}
