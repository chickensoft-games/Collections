namespace Chickensoft.Collections.Tests;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Xunit;

#pragma warning disable IDE0028 // don't change collection semantics on me

public class LinkedHashSetTest
{
  [Fact]
  public void Initializes()
  {
    var set = new LinkedHashSet<string>();

    set.ShouldBeOfType<LinkedHashSet<string>>();
  }

  [Fact]
  public void IsReadOnly() =>
    new LinkedHashSet<int>().IsReadOnly.ShouldBe(false);

  [Fact]
  public void AddAndCount()
  {
    var set = new LinkedHashSet<string>();

    set.Count.ShouldBe(0);

    set.Add("a").ShouldBe(true);

    set.Count.ShouldBe(1);

    set.Add("a").ShouldBe(false);

    set.Count.ShouldBe(1);
  }

  [Fact]
  public void IEnumerable()
  {
    var set = new LinkedHashSet<int> { 1, 2, 3 } as IEnumerable;

    // boxed since we access as IEnumerable, but still the right thing.
    set.GetEnumerator().ShouldBeOfType<LinkedHashSet<int>.Enumerator>();
  }

  [Fact]
  public void ContainsAndRemove()
  {
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
  public void Clear()
  {
    var set = new LinkedHashSet<int> { 1, 2, 3 };
    set.Clear();
    set.Count.ShouldBe(0);
    set.Contains(1).ShouldBe(false);
  }

  [Fact]
  public void CopyTo()
  {
    var set = new LinkedHashSet<string> { "a", "b", "c" };
    var array = new string[3];
    set.CopyTo(array, 0);
    array.ShouldBe(["a", "b", "c"]);
  }

  [Fact]
  public void CopyToThrowsOnInvalidArgs()
  {
    var set = new LinkedHashSet<int> { 1 };
    // negative index
    Should.Throw<ArgumentOutOfRangeException>(() => set.CopyTo(new int[1], -1));
    // too small
    Should.Throw<ArgumentException>(() => set.CopyTo([], 0));
  }

  [Fact]
  public void EnumeratorOrdersItems()
  {
    var set = new LinkedHashSet<char> { 'b', 'a', 'c' };
    var enumerator = set.GetEnumerator();

    Should.Throw<Exception>(() => (enumerator as IEnumerator).Current);

    enumerator.MoveNext().ShouldBe(true);
    enumerator.Current.ShouldBe('b');

    enumerator.MoveNext().ShouldBe(true);
    enumerator.Current.ShouldBe('a');

    enumerator.MoveNext().ShouldBe(true);
    enumerator.Current.ShouldBe('c');
  }

  [Fact]
  public void EnumeratorThrowsIfModified()
  {
    var set = new LinkedHashSet<int> { 1, 2 };

    var enumerator = set.GetEnumerator();
    enumerator.MoveNext().ShouldBe(true);

    set.Add(3);

    Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
  }

  [Fact]
  public void ReverseEnumeratorOrdersItems()
  {
    var set = new LinkedHashSet<int> { 1, 2, 3 };
    var enumerator = set.GetReverseEnumerator();

    Should.Throw<Exception>(() => (enumerator as IEnumerator).Current);

    enumerator.MoveNext().ShouldBe(true);
    enumerator.Current.ShouldBe(3);

    enumerator.MoveNext().ShouldBe(true);
    enumerator.Current.ShouldBe(2);

    enumerator.MoveNext().ShouldBe(true);
    enumerator.Current.ShouldBe(1);

    enumerator.MoveNext().ShouldBe(false);
  }

  [Fact]
  public void ReverseEnumeratorThrowsIfModified()
  {
    var set = new LinkedHashSet<int> { 1, 2 };
    var enumerator = set.GetReverseEnumerator();

    enumerator.MoveNext().ShouldBe(true);
    set.Remove(1);
    Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
  }

  [Fact]
  public void IEnumerableEnumeration()
  {
    IEnumerable<int> set = new LinkedHashSet<int> { 1, 2, 3 };
    var list = new List<int>();

    foreach (var item in set)
    {
      list.Add(item);
    }

    list.ShouldBe([1, 2, 3]);
  }

  [Fact]
  public void ConstructorWithCollection()
  {
    var items = new List<int> { 3, 1, 2, 1 };
    var set = new LinkedHashSet<int>(items);
    set.ToList().ShouldBe([3, 1, 2]);
  }

  [Fact]
  public void CollectionInitializerPreservesOrderAndUniqueness()
  {
    var set = new LinkedHashSet<string> { "x", "y", "x", "z" };
    set.ToList().ShouldBe(["x", "y", "z"]);
  }

  [Fact]
  public void ResetEnumeratorAndEnumerateAgain()
  {
    var set = new LinkedHashSet<int> { 10, 20 };
    using var enumerator = set.GetEnumerator();

    enumerator.MoveNext().ShouldBe(true);
    enumerator.MoveNext().ShouldBe(true);
    enumerator.MoveNext().ShouldBe(false);

    // reset and do it again!
    enumerator.Reset();
    enumerator.MoveNext().ShouldBe(true);
    enumerator.Current.ShouldBe(10);
  }

  [Fact]
  public void ResetReverseEnumeratorAndEnumerateAgain()
  {
    var set = new LinkedHashSet<int> { 10, 20 };
    using var enumerator = set.GetReverseEnumerator();

    enumerator.MoveNext().ShouldBe(true);
    enumerator.MoveNext().ShouldBe(true);
    enumerator.MoveNext().ShouldBe(false);

    // reset and do it again!
    enumerator.Reset();
    enumerator.MoveNext().ShouldBe(true);
    enumerator.Current.ShouldBe(20);
  }

  [Fact]
  public void ICollectionAddWorks()
  {
    ICollection<string> coll = new LinkedHashSet<string>();
    coll.Add("a");
    coll.Contains("a").ShouldBe(true);
  }

  [Fact]
  public void RemoveThenAddChangesOrder()
  {
    var set = new LinkedHashSet<char> { 'a', 'b', 'c' };
    set.Remove('a').ShouldBe(true);
    set.Add('a').ShouldBe(true);

    set.ToList().ShouldBe(['b', 'c', 'a']);
  }

  [Fact]
  public void ComparerSetThrowsWhenCollectionIsNonEmpty()
  {
    Should.Throw<InvalidOperationException>(() =>
    {
      var set = new LinkedHashSet<int> { 1, 2, 3 };
      set.Comparer = EqualityComparer<int>.Default;
    });
  }

  [Fact]
  public void ComparerSetWorksWhenCollectionIsEmpty()
  {
    var set = new LinkedHashSet<int>();
    Should.NotThrow(() => set.Comparer = EqualityComparer<int>.Default);
    set.Comparer.ShouldBe(EqualityComparer<int>.Default);
  }

  [Fact]
  public void UnionWithAddsMissingItems()
  {
    var set = new LinkedHashSet<int> { 1, 2, 3 };
    set.UnionWith([3, 4, 5]);

    set.ToList().ShouldBe([1, 2, 3, 4, 5]);
  }

  [Fact]
  public void TryGetValue()
  {
    var set = new LinkedHashSet<string>(
      ["one", "two", "three"], comparer: StringComparer.OrdinalIgnoreCase
    );

    set.TryGetValue("TWO", out var found).ShouldBeTrue();
    // specific instance can be different but equivalent due to custom comparer
    found.ShouldBe("two");

    set.TryGetValue("four", out found).ShouldBe(false);
    found.ShouldBeNull();
  }
}

#pragma warning restore IDE0028
