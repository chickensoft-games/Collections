namespace Chickensoft.Collections.Tests.Reactive;

using System;
using System.Collections.Generic;
using Chickensoft.Collections.Reactive;
using Shouldly;
using Xunit;

public class AutoListTest {
  private sealed class CaseInsensitiveComparer : IEqualityComparer<string?> {
    public bool Equals(string? x, string? y) =>
      string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
    public int GetHashCode(string obj) =>
      StringComparer.OrdinalIgnoreCase.GetHashCode(obj);
  }

  [Fact]
  public void IsReadOnly() {
    var list = new AutoList<string>();
    list.IsReadOnly.ShouldBeFalse();
  }

  [Fact]
  public void IEnumerableReturnsEnumerator() {
    var list = new AutoList<string>(["a", "b", "c"]);
    var enumerator = ((IEnumerable<string>)list).GetEnumerator();
    enumerator.MoveNext().ShouldBeTrue();
    enumerator.Current.ShouldBe("a");
    enumerator.Reset();
    enumerator.MoveNext().ShouldBeTrue();
    enumerator.Current.ShouldBe("a");
  }

  [Fact]
  public void CopyToCopiesItems() {
    var list = new AutoList<string>(["a", "b", "c"]);
    var arr = new string[3];
    list.CopyTo(arr, 0);
    arr.ShouldBe(["a", "b", "c"]);
  }

  [Fact]
  public void AddNotifiesBothAddCallbacksWithCorrectIndexAndBindingOrder() {
    var list = new AutoList<string>();

    var log = new List<string>();

    var b1 = list.Bind();
    b1.OnAdd(_ => log.Add("B1-R"));
    b1.OnAdd((_, idx) => log.Add($"B1-V@{idx}"));

    var b2 = list.Bind();
    b2.OnAdd(_ => log.Add("B2-R"));
    b2.OnAdd((_, idx) => log.Add($"B2-V@{idx}"));

    list.Add("one");

    log.ShouldBe(["B1-R", "B1-V@0", "B2-R", "B2-V@0"]);
  }

  [Fact]
  public void InsertPreservesBindingOrder() {
    var list = new AutoList<string>(["a", "b"]);
    var log = new List<string>();

    var b1 = list.Bind();
    b1.OnAdd((s, i) => log.Add($"B1:{s}@{i}"));

    var b2 = list.Bind();
    b2.OnAdd((s, i) => log.Add($"B2:{s}@{i}"));

    list.Insert(1, "X"); // a, X, b

    log.ShouldBe(["B1:X@1", "B2:X@1"]);
    list[0].ShouldBe("a");
    list[1].ShouldBe("X");
    list[2].ShouldBe("b");
  }

  [Fact]
  public void UpdateNotifiesCallbacks() {
    var list = new AutoList<string>(["foo"]);

    var seen = new List<string>();
    var b = list.Bind();

    b.OnUpdate((prev, cur) => seen.Add($"{prev}->{cur}"));
    b.OnUpdate((prev, cur, idx) => seen.Add($"{idx}:{prev}->{cur}"));

    list[0] = "bar"; // different
    seen.ShouldBe(["foo->bar", "0:foo->bar"]);
  }

  [Fact]
  public void UpdateDoesNotNotifyWhenComparerSaysEqual() {
    var list = new AutoList<string>(["Foo"], new CaseInsensitiveComparer());

    var called = 0;
    var b = list.Bind();
    b.OnUpdate((_, _) => called++);

    list[0] = "FOO"; // equal under custom comparer
    called.ShouldBe(0);

    list[0] = "Food"; // different
    called.ShouldBe(1);
  }

  [Fact]
  public void RemoveNotifiesBothRemoveCallbacksWithCorrectIndex() {
    var list = new AutoList<string>(["a", "b", "c"]);
    var log = new List<string>();

    var b = list.Bind();
    b.OnRemove(s => log.Add($"R:{s}"));
    b.OnRemove((s, i) => log.Add($"V:{s}@{i}"));

    list.Remove("b");
    log.ShouldBe(["R:b", "V:b@1"]);
    list.ShouldBe(["a", "c"]);
  }

  [Fact]
  public void ClearNotifiesOnce() {
    var list = new AutoList<string>(["a", "b"]);

    var clears = 0;
    var removes = 0;

    var b = list.Bind();
    b.OnClear(() => clears++);
    b.OnRemove(_ => removes++);

    list.Clear();

    clears.ShouldBe(1);
    removes.ShouldBe(0);
    list.Count.ShouldBe(0);

    Should.NotThrow(list.Clear); // should not throw on empty
  }

  [Fact]
  public void AddIsDeferredToNextBroadcastRound() {
    var list = new AutoList<string>();
    var log = new List<string>();

    var b1 = list.Bind();
    var b2 = list.Bind();

    b1.OnAdd((_, idx) => {
      log.Add($"B1@{idx}");
      if (idx == 0) {
        list.Add("two"); // should not interleave with the current broadcast
      }
    });
    b2.OnAdd((_, idx) => log.Add($"B2@{idx}"));

    list.Add("one");

    log.ShouldBe(["B1@0", "B2@0", "B1@1", "B2@1"]);
    list.ShouldBe(["one", "two"]);
  }

  [Fact]
  public void RemoveIsDeferredToAfterCurrentRemove() {
    var list = new AutoList<string>(["a", "b"]);
    var log = new List<string>();

    var b1 = list.Bind();
    var b2 = list.Bind();

    b1.OnRemove((_, idx) => {
      log.Add($"B1@{idx}");
      if (idx == 0) {
        list.Remove("b"); // should process after current remove completes
      }
    });
    b2.OnRemove((_, idx) => log.Add($"B2@{idx}"));

    list.Remove("a");

    // After removing "a", "b" shifts to index 0; the second removal sees idx 0.
    log.ShouldBe(["B1@0", "B2@0", "B1@0", "B2@0"]);
    list.Count.ShouldBe(0);
  }

  [Fact]
  public void ClearIsDeferredToAfterCurrentAdd() {
    var list = new AutoList<string>();
    var log = new List<string>();

    var b1 = list.Bind();
    var b2 = list.Bind();

    b1.OnAdd((_, idx) => {
      log.Add($"B1-Add@{idx}");
      list.Clear(); // should not cancel callbacks for the current add
    });
    b2.OnAdd((_, idx) => log.Add($"B2-Add@{idx}"));

    b1.OnClear(() => log.Add("B1-Clear"));
    b2.OnClear(() => log.Add("B2-Clear"));

    list.Add("x");

    log.ShouldBe(["B1-Add@0", "B2-Add@0", "B1-Clear", "B2-Clear"]);
    list.Count.ShouldBe(0);
  }

  [Fact]
  public void EnumeratorThrowsIfModifiedDuringEnumeration() {
    var list = new AutoList<string>(["a", "b", "c"]);
    var e = list.GetEnumerator();

    e.MoveNext().ShouldBeTrue();
    e.Current.ShouldBe("a");

    list.Add("d"); // modifies version

    var ex = Should.Throw<InvalidOperationException>(() => e.MoveNext());
    ex.Message.ShouldBe("AutoList collection was modified during enumeration.");
  }

  [Fact]
  public void EnumeratorYieldsInOrder() {
    var list = new AutoList<string>(["a", "b", "c"]);
    var seen = new List<string>();

    foreach (var s in list) {
      seen.Add(s);
    }

    seen.ShouldBe(["a", "b", "c"]);
  }

  [Fact]
  public void ContainsAndIndexOfRespectCustomComparer() {
    var list = new AutoList<string>(["Foo"], new CaseInsensitiveComparer());
    list.Contains("foo").ShouldBeTrue();
    list.IndexOf("FOO").ShouldBe(0);

    list.Add("");
    list.Contains("").ShouldBeTrue();
    list.IndexOf("").ShouldBe(1);
  }

  [Fact]
  public void ThrowsOnInvalidIndices() {
    var list = new AutoList<string>(["x"]);

    Should.Throw<ArgumentOutOfRangeException>(() => list.Insert(-1, "y"));
    Should.Throw<ArgumentOutOfRangeException>(() => list.Insert(2, "y"));
    Should.Throw<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
    Should.Throw<ArgumentOutOfRangeException>(() => list.RemoveAt(1));
    Should.Throw<ArgumentOutOfRangeException>(() => { list[-1] = "z"; });
    Should.Throw<ArgumentOutOfRangeException>(() => { list[1] = "z"; });
  }

  [Fact]
  public void RemoveAtRespectsIndex() {
    var list = new AutoList<string>(["a", "b", "c"]);

    var log = new List<string>();
    var b = list.Bind();
    b.OnRemove((s, i) => log.Add($"R:{s}@{i}"));

    list.RemoveAt(1); // removes "b"

    log.ShouldBe(["R:b@1"]);
    list.ShouldBe(["a", "c"]);
  }

  [Fact]
  public void FakeBindingBroadcastsCorrectly() {
    var b = AutoList<string>.CreateFakeBinding();

    var addRef = 0;
    var addVal = new List<(string item, int index)>();
    var upd = new List<(string prev, string cur, int index)>();
    var removeRef = 0;
    var removeVal = new List<(string item, int index)>();
    var clear = 0;

    b.OnAdd(_ => addRef++);
    b.OnAdd((s, i) => addVal.Add((s, i)));

    b.OnUpdate((p, c) => upd.Add((p, c, -1)));
    b.OnUpdate((p, c, i) => upd.Add((p, c, i)));

    b.OnRemove(_ => removeRef++);
    b.OnRemove((s, i) => removeVal.Add((s, i)));

    b.OnClear(() => clear++);

    b.Add("A", 3);
    b.Update("A", "B", 3);
    b.Remove("B", 2);
    b.Clear();

    addRef.ShouldBe(1);
    addVal.ShouldBe([("A", 3)]);

    // Two update callbacks (both value-type) fire with same payload.
    upd.ShouldBe([("A", "B", -1), ("A", "B", 3)]);

    removeRef.ShouldBe(1);
    removeVal.ShouldBe([("B", 2)]);

    clear.ShouldBe(1);
  }

  [Fact]
  public void FakeBindingDoesNothingAfterDispose() {
    var b = AutoList<string>.CreateFakeBinding();

    b.Dispose();

    Should.NotThrow(() => b.Add("A", 0));
    Should.NotThrow(() => b.Update("A", "B", 0));
    Should.NotThrow(() => b.Remove("B", 0));
    Should.NotThrow(b.Clear);
  }
}
