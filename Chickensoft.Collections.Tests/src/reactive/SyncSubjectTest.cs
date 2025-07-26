namespace Chickensoft.Collections.Tests.Reactive;

using System;
using System.Collections.Generic;
using Chickensoft.Collections.Reactive;
using Shouldly;
using Xunit;

public sealed class SyncSubjectTest {
  [Fact]
  public void Initializes() {
    new SyncSubject().ShouldBeAssignableTo<SyncSubject>();

    var sync = new SyncLock();
    var subj = new SyncSubject();

    subj.HandleError(new InvalidOperationException())
      .ShouldBeTrue();
  }

  [Fact]
  public void AddRemoveBinding() {
    var subj = new SyncSubject();
    var binding = new FakeBinding();

    subj.AddBinding(binding);
    subj._bindings!.ShouldContain(binding);

    subj.RemoveBinding(binding);
    subj._bindings!.ShouldNotContain(binding);

    subj.Clear();
    subj._bindings.ShouldBeEmpty();
  }

  [Fact]
  public void RemoveListenerWithNoListeners() {
    var subj = new SyncSubject();
    var listener = new FakeListener();

    Should.NotThrow(() => subj.RemoveListener(listener));

    subj._listeners?.ShouldBeNull();
  }

  [Fact]
  public void RemoveBindingWithNoBindings() {
    var subj = new SyncSubject();
    var binding = new FakeBinding();

    Should.NotThrow(() => subj.RemoveBinding(binding));

    subj._bindings?.ShouldBeNull();
  }

  [Fact]
  public void ClearWithNoBindingsOrListeners() {
    var subj = new SyncSubject();

    Should.NotThrow(subj.Clear);

    subj._bindings?.ShouldBeNull();
    subj._listeners?.ShouldBeNull();
  }

  [Fact]
  public void Clear() {
    var subj = new SyncSubject();
    var binding1 = new FakeBinding();
    var binding2 = new FakeBinding();
    var listener1 = new FakeListener();
    var listener2 = new FakeListener();

    subj.AddBinding(binding1);
    subj.AddBinding(binding2);
    subj.AddListener(listener1);
    subj.AddListener(listener2);

    subj._bindings!.ShouldContain(binding1);
    subj._bindings!.ShouldContain(binding2);
    subj._listeners!.ShouldContain(listener1);
    subj._listeners!.ShouldContain(listener2);

    subj.Clear();

    subj._bindings.ShouldBeEmpty();
    subj._listeners.ShouldBeEmpty();
  }
}

public sealed class SyncSubjectValueTypesTest {
  [Fact]
  public void AnnounceValue() {
    var binding = new FakeBinding();

    var calls = 0;
    binding.OnValue((in int value) => calls++);

    var subj = new SyncSubject();
    subj.AddBinding(binding);

    subj.AnnounceValue(FakeBinding.VALUE_CHANNEL, 42);

    calls.ShouldBe(1);
  }

  [Fact]
  public void AnnounceValueThrows() {
    var binding = new FakeBinding();

    binding.OnValue((in int _) => throw new InvalidOperationException());

    var subj = new SyncSubject();
    subj.AddBinding(binding);

    Should.Throw<InvalidOperationException>(() =>
      subj.AnnounceValue(FakeBinding.VALUE_CHANNEL, 42)
    );
  }

  [Fact]
  public void AnnounceValueDefersReentrance() {
    var binding = new FakeBinding();
    var subj = new SyncSubject();

    var calls = 0;
    var values = new List<int>();
    binding.OnValue((in int value) => {
      values.Add(value);
      calls++;
      if (calls == 1) {
        subj.AnnounceValue(FakeBinding.VALUE_CHANNEL, value + 1);
      }
    });

    subj.AddBinding(binding);
    subj.AnnounceValue(FakeBinding.VALUE_CHANNEL, 1);

    calls.ShouldBe(2);
    values.ShouldBe([1, 2]);
  }

  [Fact]
  public void AnnounceValueDefersBindingAddition() {
    var binding1 = new FakeBinding();
    var binding2 = new FakeBinding();
    var subj = new SyncSubject();

    var calls1 = 0;
    var calls2 = 0;

    binding1.OnValue((in int _) => {
      calls1++;
      subj.AddBinding(binding2);
    });

    binding2.OnValue((in int _) => calls2++);

    subj.AddBinding(binding1);

    subj.AnnounceValue(FakeBinding.VALUE_CHANNEL, 1);
    subj.AnnounceValue(FakeBinding.VALUE_CHANNEL, 1);

    calls1.ShouldBe(2); // called every announcement
    calls2.ShouldBe(1); // called on second announcement after being added by 1

    subj._bindings!.ShouldContain(binding2);
  }

  [Fact]
  public void AnnounceValueDefersBindingRemoval() {
    var binding1 = new FakeBinding();
    var binding2 = new FakeBinding();
    var subj = new SyncSubject();

    var calls1 = 0;
    var calls2 = 0;

    binding1.OnValue((in int _) => {
      calls1++;
      subj.RemoveBinding(binding2);
    });

    binding2.OnValue((in int _) => calls2++);

    subj.AddBinding(binding1);
    subj.AddBinding(binding2);

    subj.AnnounceValue(FakeBinding.VALUE_CHANNEL, 1);
    subj.AnnounceValue(FakeBinding.VALUE_CHANNEL, 1);

    calls1.ShouldBe(2); // called every announcement
    calls2.ShouldBe(1); // still gets called once during first announcement

    subj._bindings!.ShouldNotContain(binding2);
  }

  [Fact]
  public void SubsequentErrorsAreHandledUntilOneThrows() {
    var binding = new FakeBinding();

    var e1 = new InvalidOperationException("First error");
    var e2 = new ArgumentException("Second error");
    var errors = new List<Exception>();

    var subj = new SyncSubject() {
      HandleError = e => {
        errors.Add(e);
        return e is ArgumentException;
      }
    };

    var calls = 0;

    binding.OnValue((in int value) => throw e1);
    binding.OnValue((in int value) => throw e2);
    // should not be called since the second error is thrown
    binding.OnValue((in int value) => calls++);

    subj.AddBinding(binding);

    Should.Throw<ArgumentException>(
      () => subj.AnnounceValue(FakeBinding.VALUE_CHANNEL, 42)
    );

    calls.ShouldBe(0);
    errors.ShouldBe([e1, e2]);
  }

  [Fact]
  public void UsesListeners() {
    var listener = new FakeListener();
    var subj = new SyncSubject();

    subj.AddListener(listener);
    subj.AnnounceValue(FakeBinding.VALUE_CHANNEL, 1);

    listener.ValueTypeValues.ShouldBe([1]);

    subj.RemoveListener(listener);

    subj.AnnounceValue(FakeBinding.VALUE_CHANNEL, 2);

    listener.ValueTypeValues.ShouldBe([1]);
  }

  [Fact]
  public void SubsequentListenerErrorsAreHandledUntilOneThrows() {
    var e1 = new InvalidOperationException("First error");
    var e2 = new ArgumentException("Second error");

    var listener1 = new ManualFakeListener() {
      OnValue = value => throw e1
    };

    var listener2 = new ManualFakeListener() {
      OnValue = value => throw e2
    };

    var listener3 = new FakeListener();

    var errors = new List<Exception>();

    var subj = new SyncSubject() {
      HandleError = e => {
        errors.Add(e);
        return e is ArgumentException;
      }
    };

    subj.AddListener(listener1);
    subj.AddListener(listener2);
    subj.AddListener(listener3);

    Should.Throw<ArgumentException>(
      () => subj.AnnounceValue(FakeBinding.VALUE_CHANNEL, 42)
    );

    errors.ShouldBe([e1, e2]);

    // should never run since the second listener throws
    listener3.ValueTypeValues.ShouldBeEmpty();
  }

  [Fact]
  public void ThrowsIfListenerErrorHandlerThrows() {
    var eListener = new ArgumentException("listener");
    var eHandler = new InvalidOperationException("Error handler failed");
    var listener = new ManualFakeListener() { OnValue = _ => throw eListener };

    var subj = new SyncSubject() { HandleError = e => throw eHandler };

    subj.AddListener(listener);

    Should.Throw<InvalidOperationException>(
      () => subj.AnnounceValue(FakeBinding.VALUE_CHANNEL, 42)
    );
  }

  [Fact]
  public void ThrowsIfBindingErrorHandlerThrows() {
    var eBinding = new ArgumentException("binding");
    var eHandler = new InvalidOperationException("Error handler failed");
    var binding = new FakeBinding();

    binding.OnValue((in int _) => throw eBinding);

    var subj = new SyncSubject() { HandleError = e => throw eHandler };

    subj.AddBinding(binding);

    Should.Throw<InvalidOperationException>(
      () => subj.AnnounceValue(FakeBinding.VALUE_CHANNEL, 42)
    );
  }

  [Fact]
  public void AbortsWhenNoErrors() {
    var binding = new FakeBinding();
    var subj = new SyncSubject();

    binding.OnValue((in int value) => {
      // clears bindings, but is deferred til later since we're in a callback
      subj.Clear();
      // should remove pending clear operation above
      subj.Abort();
      // basically, nothing should happen
    });

    subj.AddBinding(binding);

    subj.AnnounceValue(FakeBinding.VALUE_CHANNEL, 1);

    subj._bindings!.ShouldContain(binding);
  }

  [Fact]
  public void AbortsWithThrowableError() {
    var binding = new FakeBinding();
    var subj = new SyncSubject();

    binding.OnValue((in int _) => {
      // clears bindings, but is deferred til later since we're in a callback
      subj.Clear();
      // should remove pending clear operation above
      subj.Abort();
      // throws an error, which will be thrown by default
      throw new InvalidOperationException("Test error");
    });

    subj.AddBinding(binding);

    Should.Throw<InvalidOperationException>(() =>
      subj.AnnounceValue(FakeBinding.VALUE_CHANNEL, 1)
    );

    subj._bindings!.ShouldContain(binding);
  }
}

public sealed class SyncSubjectReferenceTypesTest {
  [Fact]
  public void AnnounceReference() {
    var binding = new FakeBinding();

    var calls = 0;
    binding.OnRef((string value) => calls++);

    var subj = new SyncSubject();
    subj.AddBinding(binding);

    subj.AnnounceReference(FakeBinding.REF_CHANNEL, "hello");

    calls.ShouldBe(1);
  }

  [Fact]
  public void AnnounceReferenceThrows() {
    var binding = new FakeBinding();

    binding.OnRef((string _) => throw new InvalidOperationException());

    var subj = new SyncSubject();
    subj.AddBinding(binding);

    Should.Throw<InvalidOperationException>(() =>
      subj.AnnounceReference(FakeBinding.REF_CHANNEL, "oops")
    );
  }


  [Fact]
  public void AnnounceReferenceDefersReentrance() {
    var binding = new FakeBinding();
    var subj = new SyncSubject();

    var calls = 0;
    var values = new List<string>();
    binding.OnRef((string value) => {
      values.Add(value);
      calls++;
      if (calls == 1) {
        subj.AnnounceReference(FakeBinding.REF_CHANNEL, "second");
      }
    });

    subj.AddBinding(binding);
    subj.AnnounceReference(FakeBinding.REF_CHANNEL, "first");

    calls.ShouldBe(2);
    values.ShouldBe(["first", "second"]);
  }

  [Fact]
  public void AnnounceReferenceDefersBindingAddition() {
    var binding1 = new FakeBinding();
    var binding2 = new FakeBinding();
    var subj = new SyncSubject();

    var calls1 = 0;
    var calls2 = 0;

    binding1.OnRef((string _) => {
      calls1++;
      subj.AddBinding(binding2);
    });

    binding2.OnRef((string _) => calls2++);

    subj.AddBinding(binding1);

    subj.AnnounceReference(FakeBinding.REF_CHANNEL, "test");
    // second announcement should now invoke both bindings
    subj.AnnounceReference(FakeBinding.REF_CHANNEL, "test");

    calls1.ShouldBe(2);
    calls2.ShouldBe(1);

    subj._bindings!.ShouldContain(binding2);
  }

  [Fact]
  public void AnnounceReferenceDefersBindingRemoval() {
    var binding1 = new FakeBinding();
    var binding2 = new FakeBinding();
    var subj = new SyncSubject();

    var calls1 = 0;
    var calls2 = 0;

    binding1.OnRef((string _) => {
      calls1++;
      subj.RemoveBinding(binding2);
    });

    binding2.OnRef((string _) => calls2++);

    subj.AddBinding(binding1);
    subj.AddBinding(binding2);

    subj.AnnounceReference(FakeBinding.REF_CHANNEL, "test");
    subj.AnnounceReference(FakeBinding.REF_CHANNEL, "test");

    calls1.ShouldBe(2);
    calls2.ShouldBe(1);

    subj._bindings!.ShouldNotContain(binding2);
  }

  [Fact]
  public void SubsequentErrorsAreHandledUntilOneThrows() {
    var binding = new FakeBinding();

    var e1 = new InvalidOperationException("First error");
    var e2 = new ArgumentException("Second error");
    var errors = new List<Exception>();

    var subj = new SyncSubject() {
      HandleError = e => {
        errors.Add(e);
        return e is ArgumentException;
      }
    };

    var calls = 0;

    binding.OnRef((string value) => throw e1);
    binding.OnRef((string value) => throw e2);
    // should not be called since the second error is thrown
    binding.OnRef((string value) => calls++);

    subj.AddBinding(binding);

    Should.Throw<ArgumentException>(
      () => subj.AnnounceReference(FakeBinding.REF_CHANNEL, "hello")
    );

    calls.ShouldBe(0);
    errors.ShouldBe([e1, e2]);
  }

  [Fact]
  public void UsesListeners() {
    var listener = new FakeListener();
    var subj = new SyncSubject();

    subj.AddListener(listener);
    subj.AnnounceReference(FakeBinding.REF_CHANNEL, "foo");

    // should have recorded the first announcement
    listener.RefTypeValues.ShouldBe(["foo"]);

    subj.RemoveListener(listener);
    subj.AnnounceReference(FakeBinding.REF_CHANNEL, "bar");

    // no new values after removal
    listener.RefTypeValues.ShouldBe(["foo"]);
  }

  [Fact]
  public void SubsequentListenerErrorsAreHandledUntilOneThrows() {
    var e1 = new InvalidOperationException("First error");
    var e2 = new ArgumentException("Second error");

    var listener1 = new ManualFakeListener() { OnRef = _ => throw e1 };
    var listener2 = new ManualFakeListener() { OnRef = _ => throw e2 };
    var listener3 = new FakeListener();

    var errors = new List<Exception>();
    var subj = new SyncSubject() {
      HandleError = e => {
        errors.Add(e);
        return e is ArgumentException; // stop on second error
      }
    };

    subj.AddListener(listener1);
    subj.AddListener(listener2);
    subj.AddListener(listener3);

    Should.Throw<ArgumentException>(
      () => subj.AnnounceReference(FakeBinding.REF_CHANNEL, "hello")
    );

    // only the first two errors are captured, third listener is never invoked
    errors.ShouldBe([e1, e2]);
    listener3.RefTypeValues.ShouldBeEmpty();
  }

  [Fact]
  public void ThrowsIfListenerErrorHandlerThrows() {
    var eListener = new ArgumentException("listener");
    var eHandler = new InvalidOperationException("handler failure");
    var listener = new ManualFakeListener() { OnRef = _ => throw eListener };

    var subj = new SyncSubject() { HandleError = e => throw eHandler };
    subj.AddListener(listener);

    Should.Throw<InvalidOperationException>(
      () => subj.AnnounceReference(FakeBinding.REF_CHANNEL, "oops")
    );
  }

  [Fact]
  public void ThrowsIfBindingErrorHandlerThrows() {
    var eBinding = new ArgumentException("binding");
    var eHandler = new InvalidOperationException("handler failure");
    var binding = new FakeBinding();
    binding.OnRef((string _) => throw eBinding);

    var subj = new SyncSubject() { HandleError = e => throw eHandler };
    subj.AddBinding(binding);

    Should.Throw<InvalidOperationException>(
      () => subj.AnnounceReference(FakeBinding.REF_CHANNEL, "oops")
    );
  }

  [Fact]
  public void AbortsWhenNoErrors() {
    var binding = new FakeBinding();
    var subj = new SyncSubject();

    binding.OnRef((string _) => {
      // queue a clear, then abort—nothing should be removed
      subj.Clear();
      subj.Abort();
    });

    subj.AddBinding(binding);
    subj.AnnounceReference(FakeBinding.REF_CHANNEL, "keep-me");

    subj._bindings!.ShouldContain(binding);
  }

  [Fact]
  public void AbortsWithThrowableError() {
    var binding = new FakeBinding();
    var subj = new SyncSubject();

    binding.OnRef((string _) => {
      subj.Clear();
      subj.Abort();
      throw new InvalidOperationException("oops");
    });

    subj.AddBinding(binding);

    Should.Throw<InvalidOperationException>(
      () => subj.AnnounceReference(FakeBinding.REF_CHANNEL, "error")
    );

    // even after the exception, the binding list must remain intact
    subj._bindings!.ShouldContain(binding);
  }

  [Fact]
  public void OnlyInvokesRelevantCallbacks() {
    var binding = new FakeBinding();
    var subj = new SyncSubject();

    var baseCalls = 0;
    binding.OnRef((BaseClass _) => baseCalls++);
    var derivedCalls = 0;
    binding.OnRef((DerivedClass _) => derivedCalls++);

    subj.AddBinding(binding);

    subj.AnnounceReference(FakeBinding.REF_CHANNEL, new DerivedClass());
    baseCalls.ShouldBe(1);
    derivedCalls.ShouldBe(1);

    subj.AnnounceReference(FakeBinding.REF_CHANNEL, new BaseClass());
    baseCalls.ShouldBe(2);
    derivedCalls.ShouldBe(1);
  }
}
