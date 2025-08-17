namespace Chickensoft.Collections.Tests.Reactive;

using System;
using Chickensoft.Collections.Reactive;
using Shouldly;
using Xunit;

#pragma warning disable IDE0350, IDE0039 // breaks with `in` parameters

public class SyncBindingTest {
  [Fact]
  public void AddChannelDoesNothingIfChannelAlreadyExists() {
    var binding = new FakeBinding();

    Should.NotThrow(() => binding.AddChannel(FakeBinding.VALUE_CHANNEL));
  }

  [Fact]
  public void AllowsMultipleCallbacksPerType() {
    var binding = new FakeBinding();

    var cb1Called = 0;
    var cb2Called = 0;
    var cb3Called = 0;

    ValueTypeCallback<int> cb1 = (in int _) => cb1Called++;
    ValueTypeCallback<int> cb2 = (in int _) => cb2Called++;
    ValueTypeCallback<float> cb3 = (in float _) => cb3Called++;

    var cb4Called = 0;
    var cb5Called = 0;
    var cb6Called = 0;

    RefTypeCallback<string> cb4 = (string _) => cb4Called++;
    RefTypeCallback<object> cb5 = (object _) => cb5Called++;
    RefTypeCallback<object> cb6 = (object _) => cb6Called++;

    binding.OnValue(cb1);
    binding.OnValue(cb2);
    binding.OnValue(cb3);

    binding.OnRef(cb4);
    binding.OnRef(cb5);
    binding.OnRef(cb6);

    binding.InvokeRefTypeCallbacks(FakeBinding.REF_CHANNEL, "test");

    cb4Called.ShouldBe(1); // string
    cb5Called.ShouldBe(1); // string matches object
    cb6Called.ShouldBe(1); // string matches object

    binding.InvokeRefTypeCallbacks(FakeBinding.REF_CHANNEL, new object());

    cb4Called.ShouldBe(1); // string
    cb5Called.ShouldBe(2); // object
    cb6Called.ShouldBe(2); // object

    binding.InvokeValueTypeCallbacks(FakeBinding.VALUE_CHANNEL, 42);

    cb1Called.ShouldBe(1); // int
    cb2Called.ShouldBe(1); // int
    cb3Called.ShouldBe(0); // float

    binding.InvokeValueTypeCallbacks(FakeBinding.VALUE_CHANNEL, 3.14f);

    cb1Called.ShouldBe(1); // int
    cb2Called.ShouldBe(1); // int
    cb1Called.ShouldBe(1); // float
  }

  [Fact]
  public void AllowsValueTypePredicates() {
    var binding = new FakeBinding();

    var evenCalls = 0;
    var oddCalls = 0;

    binding.OnValue((in int v) => evenCalls++, v => v % 2 == 0);
    binding.OnValue((in int v) => oddCalls++, v => v % 2 != 0);

    for (var i = 0; i < 10; i++) {
      binding.InvokeValueTypeCallbacks(FakeBinding.VALUE_CHANNEL, i);
    }

    evenCalls.ShouldBe(5); // 0,2,4,6,8
    oddCalls.ShouldBe(5); // 1,3,5,7,9
  }

  [Fact]
  public void AllowsRefTypePredicates() {
    var binding = new FakeBinding();

    var stringCalls = 0;
    var objCalls = 0;

    binding.OnRef((string _) => stringCalls++, s => s.Length > 3);
    binding.OnRef((object _) => objCalls++); // no predicate, always called

    binding.InvokeRefTypeCallbacks(FakeBinding.REF_CHANNEL, "hi");
    binding.InvokeRefTypeCallbacks(FakeBinding.REF_CHANNEL, "hello");
    binding.InvokeRefTypeCallbacks(FakeBinding.REF_CHANNEL, new object());

    stringCalls.ShouldBe(1); // only "hello" passes the length > 3 predicate
    objCalls.ShouldBe(3); // all three invocations
  }

  [Fact]
  public void RegisterCallbackFailsIfChannelIsNotAdded() {
    var binding = new FakeBinding(skipChannelAdds: true);

    Should.Throw<ArgumentException>(
      () => binding.OnValue((in int value) => { })
    );

    Should.Throw<ArgumentException>(
      () => binding.OnRef((string value) => { })
    );
  }

  [Fact]
  public void GetCallbacksFailsIfChannelIsNotAdded() {
    var binding = new FakeBinding(skipChannelAdds: true);

    Should.Throw<ArgumentException>(
      () => binding.GetValueTypeCallbacks<int>(FakeBinding.VALUE_CHANNEL)
    );

    Should.Throw<ArgumentException>(
      () => binding.GetRefTypeCallbacks(FakeBinding.REF_CHANNEL)
    );
  }

  [Fact]
  public void GetValueTypeCallbacksReturnsEmptyIfNoCallbacksForType() {
    var binding = new FakeBinding();

    binding.AddChannel(FakeBinding.VALUE_CHANNEL);

    var callbacks = binding.GetValueTypeCallbacks<int>(
      FakeBinding.VALUE_CHANNEL
    );
    callbacks.ShouldBeEmpty();
  }

  [Fact]
  public void CanOnlyBeBoundOnceAndOnlyIfNotDisposed() {
    var subject = new SyncSubject();
    var binding = new FakeBinding();

    Should.NotThrow(() => binding.Bind(subject));
    Should.Throw<InvalidOperationException>(() => binding.Bind(subject));

    binding.Dispose();

    Should.NotThrow(binding.Dispose);
    Should.NotThrow(binding.Dispose);

    Should.Throw<ObjectDisposedException>(() => binding.Bind(subject));
  }

  [Fact]
  public void AutoBindsOnCreation() {
    var subject = new SyncSubject();
    var binding = new FakeBinding(subject: subject);

    // cuz it's already bound
    Should.Throw<InvalidOperationException>(() => binding.Bind(subject));
  }
}

#pragma warning restore IDE0350, IDE0039
