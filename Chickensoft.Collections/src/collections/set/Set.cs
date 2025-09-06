namespace Chickensoft.Collections;

using System.Collections.Generic;

/// <summary>
/// Set implementation that simply extends <see cref="HashSet{T}" /> and
/// conforms to the <see cref="IReadOnlySet{T}" />
/// interface.
/// </summary>
/// <typeparam name="T">The type of elements in the set.</typeparam>
public class Set<T> : HashSet<T>, IReadOnlySet<T>;
