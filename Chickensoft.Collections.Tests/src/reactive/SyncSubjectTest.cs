namespace Chickensoft.Collections.Tests.Reactive;

using System;
using System.Collections.Generic;
using Chickensoft.Collections.Reactive;
using Shouldly;
using Xunit;

public class PublishSubjectTest {
  private readonly record struct ValueBroadcast<T>(int Channel, T Value) :
      IBroadcast where T : struct {
    public void Invoke(SyncBinding binding) =>
      binding.InvokeValueTypeCallbacks(Channel, Value);
  }

  private readonly record struct RefBroadcast(int Channel, object Value) :
      IBroadcast {
    public void Invoke(SyncBinding binding) =>
      binding.InvokeRefTypeCallbacks(Channel, Value);
  }

  private readonly record struct CustomBroadcast<TVal>(
    int ValueChannel, int RefChannel, TVal V, object O
  ) : IBroadcast where TVal : struct {
    public void Invoke(SyncBinding binding) {
      binding.InvokeValueTypeCallbacks(ValueChannel, V);
      binding.InvokeRefTypeCallbacks(RefChannel, O);
    }
  }

  [Fact]
  public void DiscardsWhenNoBindings() {
    var subject = new SyncSubject();

    // no bindings yet — should be discarded into the void
    subject.Broadcast(new ValueBroadcast<int>(FakeBinding.VALUE_CHANNEL, 1));

    var binding = new FakeBinding();
    var called = 0;
    binding.OnValue((in int _) => called++);

    binding.Bind(subject);

    // should now be observed
    subject.Broadcast(new ValueBroadcast<int>(FakeBinding.VALUE_CHANNEL, 2));

    called.ShouldBe(1);
  }

  [Fact]
  public void InvokesBindingsInAddOrder() {
    var subject = new SyncSubject();

    var log = new List<string>();

    var b1 = new FakeBinding();
    b1.OnValue((in int _) => log.Add("B1"));
    var b2 = new FakeBinding();
    b2.OnValue((in int _) => log.Add("B2"));

    b1.Bind(subject);
    b2.Bind(subject);

    subject.Broadcast(new ValueBroadcast<int>(FakeBinding.VALUE_CHANNEL, 42));

    log.ShouldBe(["B1", "B2"]);
  }

  [Fact]
  public void BroadcastsAtomically() {
    var subject = new SyncSubject();

    var log = new List<string>();

    var b1 = new FakeBinding();
    b1.OnValue((in int _) => log.Add("B1-V"));
    b1.OnRef((object _) => log.Add("B1-R"));

    var b2 = new FakeBinding();
    b2.OnValue((in int _) => log.Add("B2-V"));
    b2.OnRef((object _) => log.Add("B2-R"));

    b1.Bind(subject);
    b2.Bind(subject);

    subject.Broadcast(
      new CustomBroadcast<int>(
        FakeBinding.VALUE_CHANNEL,
        FakeBinding.REF_CHANNEL,
        7,
        "hello"
      )
    );

    // must finish both callbacks for b1 before any of b2
    log.ShouldBe(["B1-V", "B1-R", "B2-V", "B2-R"]);
  }

  [Fact]
  public void AddBindingDuringBroadcastTakesEffectNextBroadcast() {
    var subject = new SyncSubject();
    var log = new List<string>();

    var b1 = new FakeBinding();
    var b2 = new FakeBinding();

    b1.OnValue((in int _) => {
      log.Add("B1");
      // Add b2 during processing; should not see it this round.
      subject.AddBinding(b2);
    });
    b2.OnValue((in int _) => log.Add("B2"));

    b1.Bind(subject);

    subject.Broadcast(new ValueBroadcast<int>(FakeBinding.VALUE_CHANNEL, 1));
    log.ShouldBe(["B1"]);

    subject.Broadcast(new ValueBroadcast<int>(FakeBinding.VALUE_CHANNEL, 2));
    log.ShouldBe(["B1", "B1", "B2"]);
  }

  [Fact]
  public void RemoveBindingDuringBroadcastTakesEffectAfterCurrentBroadcast() {
    var subject = new SyncSubject();
    var log = new List<string>();

    var b1 = new FakeBinding();
    var b2 = new FakeBinding();

    b1.OnValue((in int _) => {
      log.Add("B1");
      // Remove b2 for later
      subject.RemoveBinding(b2);
    });
    b2.OnValue((in int _) => log.Add("B2"));

    b1.Bind(subject);
    b2.Bind(subject);

    subject.Broadcast(new ValueBroadcast<int>(FakeBinding.VALUE_CHANNEL, 1));
    log.ShouldBe(["B1", "B2"]);

    subject.Broadcast(new ValueBroadcast<int>(FakeBinding.VALUE_CHANNEL, 2));
    log.ShouldBe(["B1", "B2", "B1"]);
  }

  [Fact]
  public void ClearBindingsDuringBroadcastTakesEffectAfterCurrentBroadcast() {
    var subject = new SyncSubject();
    var log = new List<string>();

    var b1 = new FakeBinding();
    var b2 = new FakeBinding();

    b1.OnValue((in int _) => {
      log.Add("B1");
      // Clear all during processing; b2 should still receive this broadcast.
      subject.ClearBindings();
    });
    b2.OnValue((in int _) => log.Add("B2"));

    b1.Bind(subject);
    b2.Bind(subject);

    subject.Broadcast(new ValueBroadcast<int>(FakeBinding.VALUE_CHANNEL, 1));
    log.ShouldBe(["B1", "B2"]);

    // After clear, nothing should run.
    subject.Broadcast(new ValueBroadcast<int>(FakeBinding.VALUE_CHANNEL, 2));
    log.ShouldBe(["B1", "B2"]);
  }

  [Fact]
  public void ExceptionsStopCurrentProcessingButAllowFutureBroadcasts() {
    var subject = new SyncSubject();
    var callsB1 = 0;
    var callsB2 = 0;

    var throwNow = true;

    var b1 = new FakeBinding();
    b1.OnValue((in int _) => {
      callsB1++;
      if (throwNow) {
        throw new InvalidOperationException("boom");
      }
    });

    var b2 = new FakeBinding();
    b2.OnValue((in int _) => callsB2++);

    b1.Bind(subject);
    b2.Bind(subject);

    Should.Throw<InvalidOperationException>(() =>
      subject.Broadcast(new ValueBroadcast<int>(FakeBinding.VALUE_CHANNEL, 1))
    );

    // B2 must not have been invoked for the failed broadcast.
    callsB1.ShouldBe(1);
    callsB2.ShouldBe(0);

    // Subsequent broadcasts proceed normally.
    throwNow = false;
    subject.Broadcast(new ValueBroadcast<int>(FakeBinding.VALUE_CHANNEL, 2));

    callsB1.ShouldBe(2);
    callsB2.ShouldBe(1);
  }

  [Fact]
  public void DisposingBindingRemovesIt() {
    var subject = new SyncSubject();
    var calls = 0;

    var b = new FakeBinding();
    b.OnValue((in int _) => calls++);

    b.Bind(subject);

    subject.Broadcast(new ValueBroadcast<int>(FakeBinding.VALUE_CHANNEL, 1));
    calls.ShouldBe(1);

    b.Dispose(); // should remove itself from subject

    subject.Broadcast(new ValueBroadcast<int>(FakeBinding.VALUE_CHANNEL, 2));
    calls.ShouldBe(1); // no further calls after dispose
  }

  [Fact]
  public void RefTypeBroadcastsRouteCorrectly() {
    var subject = new SyncSubject();

    var callsObj = 0;
    var callsString = 0;

    var b = new FakeBinding();
    b.OnRef((object _) => callsObj++);
    b.OnRef((string _) => callsString++);

    b.Bind(subject);

    subject.Broadcast(new RefBroadcast(FakeBinding.REF_CHANNEL, "hello"));
    callsObj.ShouldBe(1);
    callsString.ShouldBe(1);

    subject.Broadcast(new RefBroadcast(FakeBinding.REF_CHANNEL, new object()));
    callsObj.ShouldBe(2);
    callsString.ShouldBe(1);
  }
}
