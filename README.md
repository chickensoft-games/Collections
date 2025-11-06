# Chickensoft Collections

[![Chickensoft Badge][chickensoft-badge]][chickensoft-website] [![Discord][discord-badge]][discord] [![Read the docs][read-the-docs-badge]][docs] ![line coverage][line-coverage] ![branch coverage][branch-coverage]

Lightweight collections, utilities, and general interface types to help make maintainable code.

---

<p align="center">
<img alt="Cardboard Box with Chickensoft Logo" src="Chickensoft.Collections/icon.png" width="200">
</p>

## Install

```sh
dotnet add package Chickensoft.Collections
```

## LinkedHashSet<T>

A simple `LinkedHashSet` implementation is provided that provides basic set semantics (**not** the full `ISet<T>` interface) while guaranteeing insertion order (since the standard .NET `HashSet<T>` cannot guarantee insertion order).

> [!NOTE]
> While preserving insertion order can be useful in certain situations, the tradeoff is that a `LinkedList` and a `Dictionary` are both used under the hood as the backing store. While this is lightweight and leverages .NET's efficient collection implementations, the linked list still allocates on the heap for every insertion and violates cache locality. For all but the most intense use cases, this will generally be acceptable where maintaining insertion order is desired. Otherwise, just use .NET's HashSet<T>.

`LinkedHashSet` provides struct enumerators for efficient, allocation-free enumeration.

```csharp
  var set = new LinkedHashSet<string>()
  {
    "c",
    "b",
    "a",
  };

  set.Remove("c");
  set.Add("z");

  set.ToList().ShouldBe(["b", "a", "z"]);
```

## LinkedHashMap<TKey, TValue>

A simple `LinkedHashMap` implementation is provided that provides full dictionary semantics while guaranteeing insertion order (since the standard .NET `Dictionary<TKey, TValue>` cannot guarantee insertion order).

`LinkedHashMap` provides struct enumerators for efficient, allocation-free enumeration.

> [!NOTE]
> While preserving insertion order can be useful in certain situations, the tradeoff is that a `LinkedList` and a `Dictionary` are both used under the hood as the backing store. While this is lightweight and leverages .NET's efficient collection implementations, the linked list still allocates on the heap for every insertion and violates cache locality. For all but the most intense use cases, this will generally be acceptable where maintaining insertion order is desired. Otherwise, just use .NET's Dictionary<TKey, TValue>.

```csharp
  var map = new LinkedHashMap<string, int>()
  {
    ["b"] = 2,
    ["a"] = 1,
  };

  map.Keys.ShouldBe(["b", "a"]);

  map.Remove("b");
  map["z"] = 26;

  map.ToList().ShouldBe([new KeyValuePair("a", 1), new KeyValuePair("z", 26)]);
```

## Set and IReadOnlySet

For whatever reason, netstandard does not include `IReadOnlySet`. To workaround this, we've provided our own version of `IReadOnlySet` and a `Set` implementation that simply extends `HashSet` and adds the interface to it.

## Blackboard

A blackboard datatype is provided that allows reference values to be stored by type. It implements two interfaces, `IBlackboard` and `IReadOnlyBlackboard`.

```csharp
var blackboard = new Blackboard();

blackboard.Set("string value");
var stringValue = blackboard.Get<String>();

blackboard.Set(new MyObject());
var myObj = blackboard.Get<MyObject>();

// ...and various other convenience methods.
```

## EntityTable

The `EntityTable<TId>` is a simple wrapper over a `ConcurrentDictionary` that is provided to help you conveniently associate any type of value with an identifier. Table entries are requested by their identifier *and* type. If the value exists and matches the requested type, it is returned. Otherwise, `null` is returned.

```csharp
var table = new EntityTable<int>();

table.Set(42, "dolphins");

// Use pattern matching for an optimal experience.
if (table.Get<string>(42) is { } value)
{
  Console.WriteLine("Dolphins are present.");
}

table.Remove(42);
```

A default implementation that uses `string` is also provided:

```csharp
var table = new EntityTable();

table.Set("identifier", new object())

if (table.Get<object>("identifier") is { } value)
{
  Console.WriteLine("Object is present.");
}
```

## Boxless Queue

The boxless queue allows you to queue struct values on the heap without boxing them, and dequeue them without needing to unbox them.

To do so, you must make an object which implements the `IBoxlessValueHandler` interface. The `HandleValue` method will be invoked whenever the boxless queue dequeues a value.

```csharp
public class MyValueHandler : IBoxlessValueHandler
{
  public void HandleValue<TValue>(in TValue value) where TValue : struct
  {
    Console.WriteLine($"Received value {value}");
  }
}
```

Once you have implemented the `IBoxlessValueHandler`, you can create a boxless queue.

```csharp
    var handler = new MyValueHandler();

    var queue = new BoxlessQueue(handler);

    // Add something to the queue.
    queue.Enqueue(valueA);

    // See if anything is in the queue.
    if (queue.HasValues)
    {
      Console.WriteLine("Something in the queue.");
    }

    // Take something out of the queue. Calls our value handler.
    queue.Dequeue();
```

## Pool

A simple object pool implementation is provided that allows you to pre-allocate objects when memory churn is a concern. Internally, the pool is just a simple wrapper around a .NET concurrent dictionary that maps types to concurrent queues of objects. By leveraging .NET concurrent collections, we can create a type-safe and thread-safe object pool that's easy to use.

Any object you wish to store in a pool must conform to `IPooled` and implement the required `Reset` method. The reset method is called when the object is returned to the pool, allowing you to reset the object's state.

```csharp
  public abstract class Shape : IPooled
  {
    public void Reset() { }
  }

  public class Cube : Shape { }
  public class Sphere : Shape { }
```

A pool can be easily created. Each derived type that you wish to pool can be "registered" with the pool. The pool will create instances of each type registered with it according to the provided capacity.

```csharp
  var pool = new Pool<Shape>();

  pool.Register<Cube>(10); // Preallocate 10 cubes.
  pool.Register<Sphere>(5); // Preallocate 5 spheres.

  // Borrow a cube and a sphere, removing them from the pool:
  var cube = pool.Get<Cube>();

  // You can also get the an object without a generic type:
  var cube2 = pool.Get(typeof(Cube));
  var cube3 = pool.Get(cube.GetType());

  var sphere = pool.Get<Sphere>();

  // Return them to the pool (their Reset() methods will be called):
  pool.Return(cube);
  pool.Return(sphere);
```

## Map [**Deprecated**]

> [!CAUTION]
> `Map<TKey, TValue>` has been deprecated in favor of `LinkedHashMap<TKey, TValue>` (see below).

A typed facade over `OrderedDictionary`. Provides a basic mechanism to store strongly typed keys and values while preserving key insertion order.

```csharp
  var map = new Map<string, int>()
  {
    ["b"] = 2,
    ["a"] = 1,
  };

  map.Keys.ShouldBe(["b", "a"]);
```

## AutoProp [**Deprecated**]

> [!CAUTION]
> `AutoProp<T>` has been deprecated in favor of `AutoValue<T>` from [Chickensoft.Sync][chickensoft-sync].

AutoProp allows you to make observable properties in the style of `IObservable`, but is implemented over plain C# events and modifies the API to be more ergonomic, *a la Chickensoft style*.

AutoProps are basically a simplified version of a `BehaviorSubject` that only updates when the new value is not equal to the previous value, as determined by the equality comparer (or the default one if you don't provide one). They operate synchronously and make guarantees about the order of changes in a very simple, easy to reason about manner.

```csharp
using Chickensoft.Collections;

public class MyObject : IDisposable
{
  // Read-only version exposed as interface.
  public IAutoProp<bool> MyValue => _myValue;

  // Read-write version.
  private readonly AutoProp<bool> _myValue = new AutoProp<bool>(false);

  public void Update()
  {
    // Update our values based on new information.
    _myValue.OnNext(true);

    // ...

    // Check the latest value.
    if (_myValue.Value)
    {
      // ...
    }

    // Subscribe to all future changes, **AND** get called immediately with the
    // current value.
    _myValue.Sync += OnMyValueChanged;

    // Subscribe to all future changes.
    _myValue.Changed += OnMyValueChanged;

    // Subscribe to completed
    _myValue.Completed += OnMyValueCompleted;

    // Subscribe to errors
    _myValue.Error += OnMyValueError;

    // Optional: inform completed listeners we're done updating values
    _myValue.OnCompleted();

    // Optional: send error listeners an error value
    _myValue.OnError(new System.InvalidOperationException());

    // ...

    // Always unsubscribe C# events when you're done :)
    _myValue.Sync -= OnMyValueChanged;
    _myValue.Changed -= OnMyValueChanged;
    _myValue.Completed -= OnMyValueCompleted;
    _myValue.Error -= OnMyValueError;

    // Or clear all subscriptions at once:
    _myValue.Clear();

    // When your object is disposing:
    _myValue.Dispose();
  }

  private void OnMyValueChanged(bool value) { }
  private void OnMyValueCompleted() { }
  private void OnMyValueError(Exception err) { }

  // ...
}
```

- ✅ Uses plain C# events.

  Observers are called one-at-a-time, in-order of subscription, on the invoking thread, and synchronously (and will always be that way unless Microsoft tampers with the underlying Multicast delegate implementation that powers C# events).

  Chickensoft prefers to keep everything synchronous and deterministic in game development, only adding parallelization or asynchronicity where it's absolutely necessary for performance. Otherwise, simpler is better.

- ✅ Familiar API.

  If you've ever used `IObservable` and/or `BehaviorSubject`, you basically already know how to use this.

- ✅ Guarantees order of events and allows updates from handlers.

  If you change the value from a changed event handler, it will queue up the next value and process it synchronously afterwards. This allows it to pass through each desired value, guaranteeing callbacks will be called in the correct order for each value it passes through.

- ✅ Doesn't update if the value hasn't changed.

---

🐣 Created with love by Chickensoft 🐤 — <https://chickensoft.games>

[chickensoft-badge]: https://chickensoft.games/img/badges/chickensoft_badge.svg
[chickensoft-website]: https://chickensoft.games
[discord-badge]: https://chickensoft.games/img/badges/discord_badge.svg
[discord]: https://discord.gg/gSjaPgMmYW
[read-the-docs-badge]: https://chickensoft.games/img/badges/read_the_docs_badge.svg
[docs]: https://chickensoft.games/docsickensoft%20Discord-%237289DA.svg?style=flat&logo=discord&logoColor=white
[line-coverage]: Chickensoft.Collections.Tests/badges/line_coverage.svg
[branch-coverage]: Chickensoft.Collections.Tests/badges/branch_coverage.svg
[chickensoft-sync]: https://github.com/chickensoft-games/Sync
