# GoDotCollections

[![Chickensoft Badge][chickensoft-badge]][chickensoft-website] [![Discord][discord-badge]][discord] [![Read the docs][read-the-docs-badge]][docs] ![line coverage][line-coverage] ![branch coverage][branch-coverage]

Chickensoft's collections collection.

---

<p align="center">
<img alt="Cardboard Box with Chickensoft Logo" src="Chickensoft.GoDotCollections/icon.png" width="200">
</p>

Sometimes you need a collection abstraction that just isn't present in dotnet. Well, here you are.

## Install

Currently, there are no Godot-specific dependencies. This is just a netstandard2.0 package — use with any Godot version!

## Map

A typed facade over `OrderedDictionary`. Provides a basic mechanism to store strongly typed keys and values (preserving key insertion order).

## AutoProp

GoDotCollections includes a small reactive helper object that allows you to make observable properties in the style of `IObservable`, but is implemented over plain C# events and modifies the API to be more ergonomic *a la Chickensoft*.

AutoProps are basically a simplified version of a `BehaviorSubject` that only updates when the new value is not equal to the previous value, as determined by the equality comparer (or the default one if you don't provide one).

```csharp
using Chickensoft.GoDotCollections;

public class MyObject : IDisposable {
  // Read-only version exposed as interface.
  public IAutoProp<bool> MyValue => _myValue;

  // Read-write version.
  private readonly AutoProp<bool> _myValue = new AutoProp<bool>(false);

  public void Update() {
    // Update our values based on new information.
    _myValue.OnNext(true);

    // ...

    // Check the latest value.
    if (_myValue.Value) {
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

🐣 Package generated from a 🐤 Chickensoft Template — <https://chickensoft.games>

[chickensoft-badge]: https://raw.githubusercontent.com/chickensoft-games/chickensoft_site/main/static/img/badges/chickensoft_badge.svg
[chickensoft-website]: https://chickensoft.games
[discord-badge]: https://raw.githubusercontent.com/chickensoft-games/chickensoft_site/main/static/img/badges/discord_badge.svg
[discord]: https://discord.gg/gSjaPgMmYW
[read-the-docs-badge]: https://raw.githubusercontent.com/chickensoft-games/chickensoft_site/main/static/img/badges/read_the_docs_badge.svg
[docs]: https://chickensoft.games/docsickensoft%20Discord-%237289DA.svg?style=flat&logo=discord&logoColor=white
[line-coverage]: Chickensoft.GoDotCollections.Tests/badges/line_coverage.svg
[branch-coverage]: Chickensoft.GoDotCollections.Tests/badges/branch_coverage.svg
