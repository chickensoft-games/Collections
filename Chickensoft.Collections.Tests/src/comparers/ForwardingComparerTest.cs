namespace Chickensoft.Collections.Tests;

using System;
using Shouldly;
using Xunit;

public sealed class ForwardingComparerTest {
  private sealed class TestClass {
    public int Value { get; set; }
  }

  [Fact]
  public void ForwardsAndHashes() {
    var comparer = StringComparer.OrdinalIgnoreCase;
    var forwarding = new ForwardingComparer<string>(comparer);

    forwarding.Comparer.ShouldBeSameAs(comparer);
    forwarding.Equals("a", "A").ShouldBeTrue();
    forwarding.GetHashCode("a").ShouldBe(comparer.GetHashCode("a"));
  }
}
