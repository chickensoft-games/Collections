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

    ValueTypeCallback<int> cb1 = (in int _) => { };
    ValueTypeCallback<int> cb2 = (in int _) => { };
    ValueTypeCallback<float> cb3 = (in float _) => { };

    var cb4Called = false;
    var cb5Called = false;
    var cb6Called = false;

    RefTypeCallback<string> cb4 = (string _) => cb4Called = true;
    RefTypeCallback<object> cb5 = (object _) => cb5Called = true;
    RefTypeCallback<object> cb6 = (object _) => cb6Called = true;

    binding.OnValue(cb1);
    binding.OnValue(cb2);
    binding.OnValue(cb3);

    binding.OnRef(cb4);
    binding.OnRef(cb5);
    binding.OnRef(cb6);

    binding._valueCallbacks[FakeBinding.VALUE_CHANNEL][typeof(int)]
      .ShouldBe([cb1, cb2]);

    binding._valueCallbacks[FakeBinding.VALUE_CHANNEL][typeof(float)]
      .ShouldBe([cb3]);

    binding._refCallbacks[FakeBinding.REF_CHANNEL][0].Callback("test");
    binding._refCallbacks[FakeBinding.REF_CHANNEL][1].Callback("test");
    binding._refCallbacks[FakeBinding.REF_CHANNEL][2].Callback("test");

    cb4Called.ShouldBeTrue();
    cb5Called.ShouldBeTrue();
    cb6Called.ShouldBeTrue();
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
}

#pragma warning restore IDE0350, IDE0039
