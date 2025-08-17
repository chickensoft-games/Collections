namespace Chickensoft.Collections.Tests.Reactive;

using System;
using Chickensoft.Collections.Reactive;

internal sealed class FakeBinding : SyncBinding {
  public const int VALUE_CHANNEL = 1; // custom channel (accepts value types)
  public const int REF_CHANNEL = 2; // custom channel (accepts reference types)

  public FakeBinding(
    bool skipChannelAdds = false, SyncSubject? subject = null
  ) : base(subject) {
    if (!skipChannelAdds) {
      AddChannel(VALUE_CHANNEL);
      AddChannel(REF_CHANNEL);
    }
  }

  public void OnValue<T>(
    ValueTypeCallback<T> callback,
    Func<T, bool>? predicate = null
  ) where T : struct =>
    AddValueTypeCallback(VALUE_CHANNEL, callback, predicate);

  public void OnRef<T>(
    RefTypeCallback<T> callback,
    Func<T, bool>? predicate = null
  ) where T : class =>
    AddRefTypeCallback(REF_CHANNEL, callback, predicate);
}

internal class BaseClass { }
internal sealed class DerivedClass : BaseClass { }
