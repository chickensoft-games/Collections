namespace Chickensoft.Collections.Tests;

using System.Collections.Generic;
using System.Data;
using Shouldly;
using Xunit;

public class BlackboardTest
{
  [Fact]
  public void SetsAndGetsData()
  {
    var blackboard = new Blackboard();
    blackboard.Has<string>().ShouldBeFalse();
    blackboard.HasObject(typeof(string)).ShouldBeFalse();
    blackboard.Set("data");
    blackboard.Has<string>().ShouldBeTrue();
    blackboard.HasObject(typeof(string)).ShouldBeTrue();
    blackboard.Get<string>().ShouldBe("data");
    blackboard.GetObject(typeof(string)).ShouldBe("data");
    blackboard.Overwrite("string");
    blackboard.Get<string>().ShouldBe("string");
    blackboard.OverwriteObject(typeof(string), "overwritten");
    blackboard.GetObject(typeof(string)).ShouldBe("overwritten");
    blackboard.SetObject(typeof(int), 5);
    blackboard.GetObject(typeof(int)).ShouldBe(5);

    blackboard.Types.ShouldBe([typeof(string), typeof(int)], ignoreOrder: true);

    // Can't change values once set.
    Should.Throw<DuplicateNameException>(() => blackboard.Set("other"));
    Should.Throw<KeyNotFoundException>(() => blackboard.Get<string[]>());
    Should.Throw<KeyNotFoundException>(
      () => blackboard.GetObject(typeof(string[]))
    );
  }
}
