namespace Chickensoft.Collections.Tests;

using System;
using System.Collections.Generic;
using Chickensoft.Collections;
using Shouldly;
using Xunit;

public class PooledLinkedListTests {
  [Fact]
  public void AddFirstSingleElementSetsHeadAndTail() {
    var list = new PooledLinkedList<int>();
    var node = list.AddFirst(42);

    list.Count.ShouldBe(1);
    list.First.ShouldBeSameAs(node);
    list.Last.ShouldBeSameAs(node);
    node.Value.ShouldBe(42);
    node.Previous.ShouldBeNull();
    node.Next.ShouldBeNull();
  }

  [Fact]
  public void AddFirstOrdersElements() {
    var list = new PooledLinkedList<string>();
    var n1 = list.AddFirst("one");
    var n2 = list.AddFirst("two");
    var n3 = list.AddFirst("three");

    list.Count.ShouldBe(3);
    list.First.ShouldBeSameAs(n3);
    list.Last.ShouldBeSameAs(n1);

    list.First!.Value.ShouldBe("three");
    list.First.Next!.Value.ShouldBe("two");
    list.First.Next.Next!.Value.ShouldBe("one");
    list.First.Next.Next.Next.ShouldBeNull();

    n1.Previous!.Value.ShouldBe("two");
    n2.Previous!.Value.ShouldBe("three");
  }

  [Fact]
  public void AddLastSingleElementSetsHeadAndTail() {
    var list = new PooledLinkedList<double>();
    var node = list.AddLast(3.14);

    list.Count.ShouldBe(1);
    list.First.ShouldBeSameAs(node);
    list.Last.ShouldBeSameAs(node);
    node.Value.ShouldBe(3.14);
    node.Previous.ShouldBeNull();
    node.Next.ShouldBeNull();
  }

  [Fact]
  public void AddLastMultipleElementsOrdersCorrectly() {
    var list = new PooledLinkedList<char>();
    var n1 = list.AddLast('a');
    var n2 = list.AddLast('b');
    var n3 = list.AddLast('c');

    list.Count.ShouldBe(3);
    list.First.ShouldBeSameAs(n1);
    list.Last.ShouldBeSameAs(n3);

    // forward traversal: a -> b -> c
    list.First!.Value.ShouldBe('a');
    list.First.Next!.Value.ShouldBe('b');
    list.First.Next.Next!.Value.ShouldBe('c');
    list.First.Next.Next.Next.ShouldBeNull();

    // backward links
    n3.Previous!.Value.ShouldBe('b');
    n2.Previous!.Value.ShouldBe('a');
  }

  [Fact]
  public void MixedAddFirstAndAddLastYieldsCorrectSequence() {
    var list = new PooledLinkedList<int>();
    list.AddFirst(2);   // [2]
    list.AddLast(3);    // [2,3]
    list.AddFirst(1);   // [1,2,3]
    list.AddLast(4);    // [1,2,3,4]

    list.Count.ShouldBe(4);

    // verify forward
    var forward = new int[list.Count];
    var idx = 0;
    foreach (var node in list) {
      forward[idx++] = node.Value;
    }
    forward.ShouldBe([1, 2, 3, 4]);

    // verify reversed
    var backward = new int[list.Count];
    idx = 0;
    foreach (var node in list.Reversed()) {
      backward[idx++] = node.Value;
    }
    backward.ShouldBe([4, 3, 2, 1]);
  }

  [Fact]
  public void RemoveNodeAtHeadTailAndHeadUpdated() {
    var list = new PooledLinkedList<string>();
    var n1 = list.AddLast("first");
    var n2 = list.AddLast("second");
    list.Remove(n1).ShouldBe("first");

    list.Count.ShouldBe(1);
    list.First.ShouldBeSameAs(n2);
    list.Last.ShouldBeSameAs(n2);
    n2.Previous.ShouldBeNull();
    n2.Next.ShouldBeNull();
  }

  [Fact]
  public void RemoveNodeAtTailHeadAndTailUpdated() {
    var list = new PooledLinkedList<int>();
    var n1 = list.AddLast(10);
    var n2 = list.AddLast(20);
    list.Remove(n2).ShouldBe(20);

    list.Count.ShouldBe(1);
    list.First.ShouldBeSameAs(n1);
    list.Last.ShouldBeSameAs(n1);
    n1.Previous.ShouldBeNull();
    n1.Next.ShouldBeNull();
  }

  [Fact]
  public void RemoveNodeInMiddleLinksNeighbors() {
    var list = new PooledLinkedList<int>();
    list.AddLast(1);
    var middle = list.AddLast(2);
    list.AddLast(3);

    list.Count.ShouldBe(3);
    var removed = list.Remove(middle);
    removed.ShouldBe(2);

    list.Count.ShouldBe(2);
    list.First!.Value.ShouldBe(1);
    list.First.Next!.Value.ShouldBe(3);
    list.Last!.Value.ShouldBe(3);
    list.Last.Previous!.Value.ShouldBe(1);
  }

  [Fact]
  public void RemoveOnlyNodeResetsList() {
    var list = new PooledLinkedList<string>();
    var value = "item";
    var single = list.AddFirst(value);

    var removed = list.Remove(single);
    removed.ShouldBeSameAs(value);

    list.Count.ShouldBe(0);
    list.First.ShouldBeNull();
    list.Last.ShouldBeNull();
  }

  [Fact]
  public void ClearReturnsAllNodesAndResetsList() {
    var list = new PooledLinkedList<int>();
    list.AddLast(1);
    list.AddLast(2);
    list.AddLast(3);

    list.Count.ShouldBe(3);
    list.Clear();

    list.Count.ShouldBe(0);
    list.First.ShouldBeNull();
    list.Last.ShouldBeNull();
  }

  [Fact]
  public void CopyToValidArrayCopiesInOrder() {
    var list = new PooledLinkedList<string>();
    list.AddLast("x");
    list.AddLast("y");
    list.AddLast("z");

    var array = new string[5];
    list.CopyTo(array, 1);

    array[0].ShouldBeNull();
    array[1].ShouldBe("x");
    array[2].ShouldBe("y");
    array[3].ShouldBe("z");
    array[4].ShouldBeNull();
  }

  [Theory]
  [InlineData(-1)]
  [InlineData(3)]
  [InlineData(4)]
  public void CopyToInvalidIndexThrows(int index) {
    var list = new PooledLinkedList<int>();
    list.AddFirst(1);
    list.AddLast(2);

    var arr = new int[2];
    Should.Throw<ArgumentOutOfRangeException>(() => list.CopyTo(arr, index));
  }

  [Fact]
  public void EnumeratorEmptyListYieldsNoElements() {
    var list = new PooledLinkedList<int>();
    var e = list.GetEnumerator();
    e.MoveNext().ShouldBeFalse();
  }

  [Fact]
  public void EnumeratorReversedEmptyListYieldsNoElements() {
    var list = new PooledLinkedList<int>();
    var e = list.Reversed();
    e.MoveNext().ShouldBeFalse();
  }

  [Fact]
  public void EnumeratorsEnumerate() {
    var list = new PooledLinkedList<string>();
    list.AddLast("a");
    var b = list.AddLast("b");
    list.AddLast("c");

    var values = new List<string>();

    foreach (var node in list) {
      values.Add(node.Value);
    }

    values.ShouldBe(["a", "b", "c"]);

    values.Clear();

    foreach (var node in list.Reversed()) {
      values.Add(node.Value);
    }

    values.ShouldBe(["c", "b", "a"]);

    list.Remove(b);
    values.Clear();

    foreach (var node in list) {
      values.Add(node.Value);
    }

    values.ShouldBe(["a", "c"]);
  }
}
