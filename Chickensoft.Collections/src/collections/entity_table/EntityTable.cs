namespace Chickensoft.Collections;

/// <summary>
/// Very simple wrapper over a concurrent dictionary that allows entities to
/// be associated to a string id and retrieved by that id as a specific type, if
/// the entity is of that type.
/// </summary>
public sealed class EntityTable : EntityTable<string> { }
