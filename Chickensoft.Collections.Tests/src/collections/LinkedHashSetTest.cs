namespace Chickensoft.Collections.Tests;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Xunit;

#pragma warning disable IDE0028 // don't change collection semantics on me

public class LinkedHashSetTest {
  [Fact]
  public void Initializes() {
    var set = new LinkedHashSet<string>();

    set.ShouldBeOfType<LinkedHashSet<string>>();
  }

  [Fact]
  public void IsReadOnly() {
    new LinkedHashSet<int>().IsReadOnly.ShouldBe(false);
  }

  [Fact]
  public void CountAndAdd() {
    var set = new LinkedHashSet<string>();
    set.Count.ShouldBe(0);
    set.Add("a").ShouldBe(true);
    set.Count.ShouldBe(1);
    // Adding duplicate returns false and does not change count
    set.Add("a").ShouldBe(false);
    set.Count.ShouldBe(1);
  }

  [Fact]
  public void ContainsAndRemove() {
    var set = new LinkedHashSet<int>();
    set.Add(1).ShouldBe(true);
    set.Contains(1).ShouldBe(true);
    set.Contains(2).ShouldBe(false);

    set.Remove(1).ShouldBe(true);
    set.Contains(1).ShouldBe(false);
    // Removing non-existent returns false
    set.Remove(1).ShouldBe(false);
  }

  [Fact]
  public void Clear() {
    var set = new LinkedHashSet<int> { 1, 2, 3 };
    set.Clear();
    set.Count.ShouldBe(0);
    set.Contains(1).ShouldBe(false);
  }

  [Fact]
  public void CopyTo() {
    var set = new LinkedHashSet<string> { "a", "b", "c" };
    var array = new string[3];
    set.CopyTo(array, 0);
    array.ShouldBe(["a", "b", "c"]);
  }

  [Fact]
  public void CopyToThrowsOnInvalidArgs() {
    var set = new LinkedHashSet<int> { 1 };
    // negative index
    Should.Throw<ArgumentOutOfRangeException>(() => set.CopyTo(new int[1], -1));
    // too small
    Should.Throw<ArgumentException>(() => set.CopyTo([], 0));
  }

  [Fact]
  public void EnumeratorOrdersItems() {
    var set = new LinkedHashSet<char> { 'b', 'a', 'c' };
    // Duplicate 'a' ignored
    set.Add('a').ShouldBe(false);
    set.Add('d').ShouldBe(true);

    var list = set.ToList();
    list.ShouldBe(['b', 'a', 'c', 'd']);

    IEnumerable enumerable = set;

    var enumerator = enumerable.GetEnumerator();
    enumerator.MoveNext().ShouldBe(true);
    enumerator.Current.ShouldBe('b');

    enumerator.MoveNext().ShouldBe(true);
    enumerator.Current.ShouldBe('a');

    enumerator.MoveNext().ShouldBe(true);
    enumerator.Current.ShouldBe('c');
  }

  [Fact]
  public void EnumeratorThrowsIfModified() {
    var set = new LinkedHashSet<int> { 1, 2 };
    var enumerator = set.GetEnumerator();
    enumerator.MoveNext().ShouldBe(true);
    // Modify during enumeration
    set.Add(3);
    Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
  }

  [Fact]
  public void IEnumerableEnumeration() {
    IEnumerable<int> set = new LinkedHashSet<int> { 1, 2, 3 };
    var list = new List<int>();
    foreach (var item in set) {
      list.Add(item);
    }
    list.ShouldBe([1, 2, 3]);
  }

  [Fact]
  public void ConstructorWithCollection() {
    var items = new List<int> { 3, 1, 2, 1 };
    var set = new LinkedHashSet<int>(items);
    set.ToList().ShouldBe([3, 1, 2]);
  }

  [Fact]
  public void CollectionInitializerPreservesOrderAndUniqueness() {
    var set = new LinkedHashSet<string> { "x", "y", "x", "z" };
    set.ToList().ShouldBe(["x", "y", "z"]);
  }

  [Fact]
  public void ResetEnumeratorAndEnumerateAgain() {
    var set = new LinkedHashSet<int> { 10, 20 };
    var enumerator = set.GetEnumerator();

    // Iterate fully
    enumerator.MoveNext().ShouldBe(true);
    enumerator.MoveNext().ShouldBe(true);
    enumerator.MoveNext().ShouldBe(false);

    // Reset and iterate from start
    enumerator.Reset();
    enumerator.MoveNext().ShouldBe(true);
    enumerator.Current.ShouldBe(10);
  }

  [Fact]
  public void ICollectionAddWorks() {
    ICollection<string> coll = new LinkedHashSet<string>();
    coll.Add("a");
    coll.Contains("a").ShouldBe(true);
  }

  [Fact]
  public void RemoveThenAddChangesOrder() {
    var set = new LinkedHashSet<char> { 'a', 'b', 'c' };
    set.Remove('a').ShouldBe(true);
    set.Add('a').ShouldBe(true);

    set.ToList().ShouldBe(['b', 'c', 'a']);
  }
}

#pragma warning restore IDE0028
