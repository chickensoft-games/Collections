namespace Chickensoft.Collections.Tests;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Xunit;

public class LinkedHashMapTest {
  [Fact]
  public void Initializes() {
    var map = new LinkedHashMap<string, string>();

    map.ShouldBeOfType<LinkedHashMap<string, string>>();

    map.KeyComparer.ShouldNotBeNull();
    map.ValueComparer.ShouldBeOfType<ReferenceComparer<string>>();
  }

  [Fact]
  public void GetsAndSetsKeys() {
    var map = new LinkedHashMap<string, int> {
      ["a"] = 1
    };
    map["a"].ShouldBe(1);
    map["a"] = 3;
    map["a"].ShouldBe(3);
  }

  [Fact]
  public void IsReadOnly() {
    var map = new LinkedHashMap<string, int>();
    map.IsReadOnly.ShouldBe(false);
  }

  [Fact]
  public void Count() {
    var map = new LinkedHashMap<string, int>();
    map.Count.ShouldBe(0);
    map["a"] = 1;
    map["b"] = 2;
    map.Count.ShouldBe(2);
    map.Remove("a").ShouldBeTrue();
    map.Remove(new KeyValuePair<string, int>("b", 2)).ShouldBeTrue();
    map.Remove(new KeyValuePair<string, int>("c", 3)).ShouldBeFalse();
    map.Remove("c").ShouldBeFalse();
    map.Count.ShouldBe(0);
  }

  [Fact]
  public void Enumerator() {
    var map = new LinkedHashMap<string, int> {
      ["a"] = 1,
      ["b"] = 2
    };
    var dictionaryEnumerator = (map as IEnumerable).GetEnumerator();
    dictionaryEnumerator.MoveNext().ShouldBeTrue();
    dictionaryEnumerator.MoveNext().ShouldBeTrue();
    dictionaryEnumerator.MoveNext().ShouldBeFalse();

    var enumerator = map.GetEnumerator();
    enumerator.MoveNext().ShouldBeTrue();
    enumerator.MoveNext().ShouldBeTrue();
    enumerator.MoveNext().ShouldBeFalse();

  }


  [Fact]
  public void CollectionInitializer() {
    var map = new LinkedHashMap<string, int> {
      { "a", 1 },
      { "b", 2 }
    };
    map.Keys.ToArray().ShouldBe(["a", "b"]);
    map.Values.ToArray().ShouldBe([1, 2]);
  }

  [Fact]
  public void Clear() {
    var map = new LinkedHashMap<string, int> {
      ["a"] = 1,
      ["b"] = 2
    };
    map.Clear();
    map.Count.ShouldBe(0);
  }

  [Fact]
  public void Remove() {
    var map = new LinkedHashMap<string, int> {
      ["a"] = 1,
      ["b"] = 2
    };
    map.Remove("a").ShouldBeTrue();
    map.Keys.ToArray().ShouldBe(["b"]);
    map.Remove("a").ShouldBeFalse();
  }

  [Fact]
  public void CopyTo() {
    var map = new LinkedHashMap<string, int> {
      ["a"] = 1,
      ["b"] = 2
    };

    var typedArray = new KeyValuePair<string, int>[2];

    map.CopyTo(typedArray, 0);

    typedArray[0].Key.ShouldBe("a");
    typedArray[0].Value.ShouldBe(1);

    typedArray[1].Key.ShouldBe("b");
    typedArray[1].Value.ShouldBe(2);
  }

  [Fact]
  public void KeysAreOrdered() {
    var map = new LinkedHashMap<string, int>() {
      ["b"] = 2,
      ["a"] = 1,
    };

    map["b"] = 3;
    map["c"] = 4;

    map.Keys.ToArray().ShouldBe(["b", "a", "c"]);
  }

  [Fact]
  public void Keys() {
    var map = new LinkedHashMap<string, int> {
      ["a"] = 1,
      ["b"] = 2
    };
    var keys = map.Keys.ToList();

    keys.ShouldBeAssignableTo<ICollection<string>>();

    keys[0].ShouldBe("a");
    keys[1].ShouldBe("b");
  }

  [Fact]
  public void Values() {
    var map = new LinkedHashMap<string, int> {
      ["a"] = 1,
      ["b"] = 2
    };

    var values = map.Values.ToList();

    values.ShouldBeAssignableTo<ICollection<int>>();

    values[0].ShouldBe(1);
    values[1].ShouldBe(2);
  }

  [Fact]
  public void ContainsKey() {
    var map = new LinkedHashMap<string, int> {
      ["a"] = 1
    };

    map.ContainsKey("a").ShouldBeTrue();
    map.ContainsKey("b").ShouldBeFalse();

    map.Remove("a");

    map.ContainsKey("a").ShouldBeFalse();
  }

  [Fact]
  public void TryGetValue() {
    var map = new LinkedHashMap<string, int> {
      ["a"] = 1
    };

    map.TryGetValue("a", out var value).ShouldBeTrue();
    value.ShouldBe(1);

    map.TryGetValue("b", out value).ShouldBeFalse();
    value.ShouldBe(0);
  }

  [Fact]
  public void Add() {
    var map = new LinkedHashMap<string, int> {
      ["a"] = 1
    };

    map.Add("b", 2);
    map.Add(new KeyValuePair<string, int>("c", 3));

    map["b"].ShouldBe(2);
    map["c"].ShouldBe(3);
  }

  [Fact]
  public void Contains() {
    var map = new LinkedHashMap<string, int> {
      ["a"] = 1
    };

    map.Contains(new KeyValuePair<string, int>("a", 1)).ShouldBeTrue();
    map.Contains(new KeyValuePair<string, int>("b", 2)).ShouldBeFalse();
  }

  [Fact]
  public void CollectionSyntaxPreservesOrder() {
    var kvpList = new List<KeyValuePair<string, int>> {
      new("c", 3),
      new("b", 2),
      new("a", 1),
    };

    LinkedHashMap<string, int> map = [.. kvpList];

    map.Keys.ToArray().ShouldBe(["c", "b", "a"]);

    kvpList.Reverse();

    map = [.. kvpList];

    map.Keys.ToArray().ShouldBe(["a", "b", "c"]);
  }

  [Fact]
  public void TypeSafeEnumerator() {
    var map = new LinkedHashMap<string, int>() {
      ["a"] = 1,
      ["b"] = 2,
      ["c"] = 3
    };

    var kvpList = new List<KeyValuePair<string, int>>();

    foreach (var kvp in map) {
      kvpList.Add(kvp);
    }
  }

  [Fact]
  public void KvpConstructor() {
    var kvpList = new List<KeyValuePair<string, int>> {
      new("c", 3),
      new("b", 2),
      new("a", 1),
    };

    var map = new LinkedHashMap<string, int>(kvpList);

    map.Keys.ToArray().ShouldBe(["c", "b", "a"]);
    map.Values.ToArray().ShouldBe([3, 2, 1]);
  }

  [Fact]
  public void ThrowsIfKeyNotFoundFromIndexer() {
    var map = new LinkedHashMap<string, int>();

    Should.Throw<KeyNotFoundException>(() => {
      var value = map["a"];
    });
  }

  [Fact]
  public void ThrowsWhenAddingDuplicateKey() {
    var map = new LinkedHashMap<string, int> {
      ["a"] = 1
    };

    Should.Throw<ArgumentException>(() => map.Add("a", 2));

    Should.Throw<ArgumentException>(
      () => map.Add(new KeyValuePair<string, int>("a", 2))
    );
  }

  [Fact]
  public void EnumeratorAsIEnumerator() {
    IEnumerable<KeyValuePair<string, int>> map =
      new LinkedHashMap<string, int> {
        ["a"] = 1,
        ["b"] = 2
      };

    var list = map.ToList();

    list.ShouldBe([
      new KeyValuePair<string, int>("a", 1),
      new KeyValuePair<string, int>("b", 2)
    ]);
  }

  [Fact]
  public void ManualIEnumeratorEnumeration() {
    IEnumerable<KeyValuePair<string, int>> map =
      new LinkedHashMap<string, int> {
        ["a"] = 1,
        ["b"] = 2
      };

    var enumerator = map.GetEnumerator();

    enumerator.MoveNext().ShouldBeTrue();
    enumerator.Current.ShouldBeOfType<KeyValuePair<string, int>>();
    enumerator.Current.Key.ShouldBe("a");
    enumerator.Current.Value.ShouldBe(1);

    enumerator.MoveNext().ShouldBeTrue();
    enumerator.Current.Key.ShouldBe("b");
    enumerator.Current.Value.ShouldBe(2);

    enumerator.MoveNext().ShouldBeFalse();
  }

  [Fact]
  public void RemoveKvpChecksEquality() {
    var map = new LinkedHashMap<string, int> {
      ["a"] = 1,
      ["b"] = 2
    };

    map.Remove(new KeyValuePair<string, int>("a", 1)).ShouldBeTrue();
    map.ContainsKey("a").ShouldBeFalse();

    map.Remove(new KeyValuePair<string, int>("b", 3)).ShouldBeFalse();
    map.ContainsKey("b").ShouldBeTrue();

    map.Remove(new KeyValuePair<string, int>("c", 3)).ShouldBeFalse();
  }

  [Fact]
  public void StructEnumeration() {
    var map = new LinkedHashMap<string, int> {
      ["a"] = 1,
      ["b"] = 2,
      ["c"] = 3
    };

    var keys = new List<string>();

    foreach (var key in map.Keys) {
      keys.Add(key);
    }

    keys.ShouldBe(["a", "b", "c"]);

    var values = new List<int>();

    foreach (var value in map.Values) {
      values.Add(value);
    }

    values.ShouldBe([1, 2, 3]);

    var items = new List<KeyValuePair<string, int>>();

    foreach (var kvp in map) {
      items.Add(kvp);
    }

    items.ShouldBe([
      new KeyValuePair<string, int>("a", 1),
      new KeyValuePair<string, int>("b", 2),
      new KeyValuePair<string, int>("c", 3)
    ]);
  }

  [Fact]
  public void KeyEnumerationThrowsIfModified() {
    var map = new LinkedHashMap<string, int> {
      ["a"] = 1,
      ["b"] = 2
    };

    var enumerator = map.Keys.GetEnumerator();

    enumerator.MoveNext().ShouldBeTrue();
    (enumerator as IEnumerator).Current.ShouldBe("a");
    enumerator.Current.ShouldBe("a");

    map["c"] = 3; // modify the map

    Should.Throw<InvalidOperationException>(() => {
      enumerator.MoveNext();
    });
  }

  [Fact]
  public void ValueEnumerationThrowsIfModified() {
    var map = new LinkedHashMap<string, int> {
      ["a"] = 1,
      ["b"] = 2
    };

    var enumerator = map.Values.GetEnumerator();
    enumerator.MoveNext().ShouldBeTrue();
    (enumerator as IEnumerator).Current.ShouldBe(1);
    enumerator.Current.ShouldBe(1);

    map["c"] = 3; // modify the map

    Should.Throw<InvalidOperationException>(() => {
      enumerator.MoveNext();
    });
  }

  [Fact]
  public void KeyValuePairEnumerationThrowsIfModified() {
    var map = new LinkedHashMap<string, int> {
      ["a"] = 1,
      ["b"] = 2
    };

    var enumerator = map.GetEnumerator();
    enumerator.MoveNext().ShouldBeTrue();
    (enumerator as IEnumerator).Current.ShouldBe(
      new KeyValuePair<string, int>("a", 1)
    );
    enumerator.Current.ShouldBe(
      new KeyValuePair<string, int>("a", 1)
    );

    map["c"] = 3; // modify the map

    Should.Throw<InvalidOperationException>(() => {
      enumerator.MoveNext();
    });
  }

  [Fact]
  public void KeyValuePairToArrayAndList() {
    var map = new LinkedHashMap<string, int> {
      ["a"] = 1,
      ["b"] = 2
    };

    var array = map.GetEnumerator().ToArray();

    array.ShouldBe([
      new KeyValuePair<string, int>("a", 1),
      new KeyValuePair<string, int>("b", 2)
    ]);

    var list = map.GetEnumerator().ToList();

    list.ShouldBe([
      new KeyValuePair<string, int>("a", 1),
      new KeyValuePair<string, int>("b", 2)
    ]);
  }

  [Fact]
  public void EnumeratorIsEnumerable() {
    var map = new LinkedHashMap<string, int> {
      ["a"] = 1,
      ["b"] = 2
    };

    var enumerator = map.GetEnumerator();

    var items = new List<KeyValuePair<string, int>>();

    foreach (var item in enumerator) {
      items.Add(item);
    }

    items.ShouldBe([
      new KeyValuePair<string, int>("a", 1),
      new KeyValuePair<string, int>("b", 2)
    ]);
  }
}
