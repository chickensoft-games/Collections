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
  public void SetStoresValuesAndOverwritesExistingValues() {
    var table = new EntityTable();

    table.Set("a", "one");
    table.Set("b", new object());

    table.Get<string>("a").ShouldBe("one");
    table.Get<object>("b").ShouldNotBeNull();

    table.Set("a", "two");
    table.Get<string>("a").ShouldBe("two");

    table.Remove("a");
    table.Remove(null);

    table.Get<string>("a").ShouldBeNull();
    table.Get<object>("b").ShouldNotBeNull();
  }

  [Fact]
  public void TryAddOnlyStoresValuesForNewKeys() {
    var table = new EntityTable();

    table.TryAdd("a", "one").ShouldBeTrue();
    table.TryAdd("a", "two").ShouldBeFalse();
    table.Get<string>("a").ShouldBe("one");
  }

  [Fact]
  public void Clears() {
    var table = new EntityTable();

    table.Set("a", "one");
    table.Set("b", new object());

    table.Get<string>("a").ShouldBe("one");
    table.Get<object>("b").ShouldNotBeNull();

    table.Clear();

    table.Get<string>("a").ShouldBeNull();
    table.Get<object>("b").ShouldBeNull();
  }
}
