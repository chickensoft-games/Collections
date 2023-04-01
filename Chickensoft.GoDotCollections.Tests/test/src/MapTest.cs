namespace Chickensoft.GoDotCollections.Tests;

using System.Collections;
using System.Collections.Generic;
using Godot;
using GoDotCollections;
using GoDotTest;
using Shouldly;

public class MapTest : TestClass {
  public MapTest(Node testScene) : base(testScene) { }

  [Test]
  public void Initializes()
    => new Map<string, string>().ShouldBeOfType<Map<string, string>>();

  [Test]
  public void GetsAndSetsKeys() {
    var map = new Map<string, int>();
    map["a"] = 1;
    map["a"].ShouldBe(1);
  }

  [Test]
  public void GetsAndSetsKeysByIndex() {
    var map = new Map<string, int>();
    map["a"] = 1;
    map[0].ShouldBe(1);
    map[0] = 2;
    map["a"].ShouldBe(2);
    map[0].ShouldBe(2);
  }

  [Test]
  public void GetsKeys() {
    var map = new Map<string, int>();
    map.Keys.ShouldBeAssignableTo<IEnumerable<string>>();
  }

  [Test]
  public void GetsValues() {
    var map = new Map<string, int>();
    map.Values.ShouldBeAssignableTo<IEnumerable<int>>();
  }

  [Test]
  public void IsReadOnly() {
    var map = new Map<string, int>();
    map.IsReadOnly.ShouldBe(false);
  }

  [Test]
  public void Count() {
    var map = new Map<string, int>();
    map.Count.ShouldBe(0);
    map["a"] = 1;
    map.Count.ShouldBe(1);
    map.Remove("a");
    map.Count.ShouldBe(0);
  }

  [Test]
  public void Enumerator() {
    var map = new Map<string, int>();
    map["a"] = 1;
    map["b"] = 2;
    var dictionaryEnumerator = map.GetEnumerator();
    dictionaryEnumerator.MoveNext().ShouldBeTrue();
    dictionaryEnumerator.MoveNext().ShouldBeTrue();
    dictionaryEnumerator.MoveNext().ShouldBeFalse();
  }

  [Test]
  public void Insert() {
    var map = new Map<string, int>();
    map["a"] = 1;
    map["b"] = 2;
    map.Insert(1, "X", 100);
    map[1].ShouldBe(100);
    map.Values.ShouldBe(new List<int> { 1, 100, 2 });
  }

  [Test]
  public void RemoveAt() {
    var map = new Map<string, int>();
    map["a"] = 1;
    map["b"] = 2;
    map.RemoveAt(1);
    map.Values.ShouldBe(new List<int> { 1 });
  }

  [Test]
  public void Contains() {
    var map = new Map<string, int>();
    map["a"] = 1;
    map.Contains("a").ShouldBeTrue();
    map.Contains("b").ShouldBeFalse();
  }

  [Test]
  public void Add() {
    var map = new Map<string, int>();
    map.Add("a", 1);
    map.Add("b", 2);
    map.Keys.ShouldBe(new List<string> { "a", "b" });
    map.Values.ShouldBe(new List<int> { 1, 2 });
  }

  [Test]
  public void Clear() {
    var map = new Map<string, int>();
    map["a"] = 1;
    map["b"] = 2;
    map.Clear();
    map.Count.ShouldBe(0);
  }

  [Test]
  public void Remove() {
    var map = new Map<string, int>();
    map["a"] = 1;
    map["b"] = 2;
    map.Remove("a");
    map.Keys.ShouldBe(new List<string> { "b" });
  }

  [Test]
  public void CopyTo() {
    var map = new Map<string, int>();
    map["a"] = 1;
    map["b"] = 2;
    var array = new DictionaryEntry[2];
    map.CopyTo(array, 0);
    array[0].ShouldBeOfType<DictionaryEntry>();
    array[1].ShouldBeOfType<DictionaryEntry>();
  }
}
