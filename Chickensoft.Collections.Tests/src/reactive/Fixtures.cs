namespace Chickensoft.Collections.Tests.Reactive;

using System;
using System.Collections.Generic;
using Chickensoft.Collections.Reactive;

internal sealed class FakeBinding : SyncBinding {
  public const int VALUE_CHANNEL = 1; // custom channel (accepts value types)
  public const int REF_CHANNEL = 2; // custom channel (accepts reference types)

  public FakeBinding(bool skipChannelAdds = false) {
    if (!skipChannelAdds) {
      AddChannel(VALUE_CHANNEL);
      AddChannel(REF_CHANNEL);
    }
  }

  public void OnValue<T>(ValueTypeCallback<T> callback) where T : struct =>
    AddValueTypeCallback(VALUE_CHANNEL, callback);

  public void OnRef<T>(RefTypeCallback<T> callback) where T : class =>
    AddRefTypeCallback(REF_CHANNEL, callback);
}

internal class BaseClass { }
internal sealed class DerivedClass : BaseClass { }

public class FakeListener : ISyncListener {
  public List<object> ValueTypeValues { get; } = [];
  public List<object> RefTypeValues { get; } = [];

  public void OnValueType<TValue>(int channel, in TValue value)
      where TValue : struct => ValueTypeValues.Add(value);

  public void OnReferenceType<TValue>(int channel, TValue value)
    where TValue : class => RefTypeValues.Add(value);
}

public class ManualFakeListener : ISyncListener {
  public Action<object>? OnValue { get; set; }
  public Action<object>? OnRef { get; set; }

  public void OnValueType<TValue>(int channel, in TValue value)
      where TValue : struct {
    OnValue?.Invoke(value);
  }

  public void OnReferenceType<TValue>(int channel, TValue value)
    where TValue : class {
    OnRef?.Invoke(value);
  }
}
