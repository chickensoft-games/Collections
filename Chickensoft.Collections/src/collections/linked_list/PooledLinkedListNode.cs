namespace Chickensoft.Collections;

internal class PooledLinkedListNode<T> : IPooled {
  public T Value { get; set; } = default!;
  public PooledLinkedListNode<T>? Next { get; set; }
  public PooledLinkedListNode<T>? Previous { get; set; }

  public void Reset() {
    Value = default!;
    Next = null;
    Previous = null;
  }
}
