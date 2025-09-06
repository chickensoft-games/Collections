namespace Chickensoft.Collections.Tests;

using Shouldly;
using Xunit;

public sealed class ReferenceComparerTest {
  private sealed class TestClass {
    public int Value { get; set; }
  }

  [Fact]
  public void ComparesAndHashes() {
    ReferenceComparer<TestClass>.Default.Equals(
      new TestClass(),
      new TestClass()
    ).ShouldBeFalse();

    var obj = new TestClass();
    ReferenceComparer<TestClass>.Default.Equals(obj, obj).ShouldBeTrue();

    var str = "test";

    ReferenceComparer<string>.Default.GetHashCode(str)
      .ShouldBe(
        System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(str)
      );
  }
}
