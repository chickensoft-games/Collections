namespace Chickensoft.Collections.Tests;

using System.Collections;
using System.Collections.Generic;
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
  public void GetsKeys() {
    var map = new Map<string, int>();
    map.Keys.ShouldBeAssignableTo<IEnumerable<string>>();
  }

  [Fact]
  public void GetsValues() {
    var map = new Map<string, int>();
    map.Values.ShouldBeAssignableTo<IEnumerable<int>>();
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
    map.Count.ShouldBe(1);
    map.Remove("a");
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
  public void Contains() {
    var map = new Map<string, int> {
      ["a"] = 1
    };
    map.Contains("a").ShouldBeTrue();
    map.Contains("b").ShouldBeFalse();
  }

  [Fact]
  public void Add() {
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
    map.Remove("a");
    map.Keys.ShouldBe(["b"]);
  }

  [Fact]
  public void CopyTo() {
    var map = new Map<string, int> {
      ["a"] = 1,
      ["b"] = 2
    };
    var array = new DictionaryEntry[2];
    map.CopyTo(array, 0);
    array[0].ShouldBeOfType<DictionaryEntry>();
    array[1].ShouldBeOfType<DictionaryEntry>();
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
}
