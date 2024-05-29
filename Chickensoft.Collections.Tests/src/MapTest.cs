namespace Chickensoft.Collections.Tests;

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Xunit;

public class MapTest {
  [Fact]
  public void Initializes()
    => new Map<string, string>().ShouldBeOfType<Map<string, string>>();

  [Fact]
  public void GetsAndSetsKeys() {
    var map = new Map<string, int> {
      ["a"] = 1
    };
    map["a"].ShouldBe(1);
  }

  [Fact]
  public void GetsAndSetsKeysByIndex() {
    var map = new Map<string, int> {
      ["a"] = 1
    };
    map[0].ShouldBe(1);
    map[0] = 2;
    map["a"].ShouldBe(2);
    map[0].ShouldBe(2);
  }

  [Fact]
  public void IsReadOnly() {
    var map = new Map<string, int>();
    map.IsReadOnly.ShouldBe(false);
  }

  [Fact]
  public void Count() {
    var map = new Map<string, int>();
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
    var map = new Map<string, int> {
      ["a"] = 1,
      ["b"] = 2
    };
    var dictionaryEnumerator = (map as IEnumerable).GetEnumerator();
    dictionaryEnumerator.MoveNext().ShouldBeTrue();
    dictionaryEnumerator.MoveNext().ShouldBeTrue();
    dictionaryEnumerator.MoveNext().ShouldBeFalse();

    var enumerator = (map as IDictionary<string, int>).GetEnumerator();
    enumerator.MoveNext().ShouldBeTrue();
    enumerator.MoveNext().ShouldBeTrue();
    enumerator.MoveNext().ShouldBeFalse();

  }

  [Fact]
  public void Insert() {
    var map = new Map<string, int> {
      ["a"] = 1,
      ["b"] = 2
    };
    map.Insert(1, "X", 100);
    map[1].ShouldBe(100);
    map.Values.ShouldBe([1, 100, 2]);
  }

  [Fact]
  public void RemoveAt() {
    var map = new Map<string, int> {
      ["a"] = 1,
      ["b"] = 2
    };
    map.RemoveAt(1);
    map.Values.ShouldBe([1]);
  }

  [Fact]
  public void CollectionInitializer() {
    var map = new Map<string, int> {
      { "a", 1 },
      { "b", 2 }
    };
    map.Keys.ShouldBe(["a", "b"]);
    map.Values.ShouldBe([1, 2]);
  }

  [Fact]
  public void Clear() {
    var map = new Map<string, int> {
      ["a"] = 1,
      ["b"] = 2
    };
    map.Clear();
    map.Count.ShouldBe(0);
  }

  [Fact]
  public void Remove() {
    var map = new Map<string, int> {
      ["a"] = 1,
      ["b"] = 2
    };
    map.Remove("a").ShouldBeTrue();
    map.Keys.ShouldBe(["b"]);
    map.Remove("a").ShouldBeFalse();
  }

  [Fact]
  public void CopyTo() {
    var map = new Map<string, int> {
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
    var map = new Map<string, int>() {
      ["b"] = 2,
      ["a"] = 1,
    };

    map["b"] = 3;

    map.Keys.ShouldBe(["b", "a"]);
  }

  [Fact]
  public void Keys() {
    var map = new Map<string, int> {
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
    var map = new Map<string, int> {
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
    var map = new Map<string, int> {
      ["a"] = 1
    };

    map.ContainsKey("a").ShouldBeTrue();
    map.ContainsKey("b").ShouldBeFalse();

    map.Remove("a");

    map.ContainsKey("a").ShouldBeFalse();
  }

  [Fact]
  public void TryGetValue() {
    var map = new Map<string, int> {
      ["a"] = 1
    };

    map.TryGetValue("a", out var value).ShouldBeTrue();
    value.ShouldBe(1);

    map.TryGetValue("b", out value).ShouldBeFalse();
    value.ShouldBe(0);
  }

  [Fact]
  public void Add() {
    var map = new Map<string, int> {
      ["a"] = 1
    };

    map.Add("b", 2);
    map.Add(new KeyValuePair<string, int>("c", 3));

    map["b"].ShouldBe(2);
    map["c"].ShouldBe(3);
  }

  [Fact]
  public void Contains() {
    var map = new Map<string, int> {
      ["a"] = 1
    };

    map.Contains(new KeyValuePair<string, int>("a", 1)).ShouldBeTrue();
    map.Contains(new KeyValuePair<string, int>("b", 2)).ShouldBeFalse();
  }


}
