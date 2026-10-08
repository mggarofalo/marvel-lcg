namespace Marvel.Rules.State;

/// <summary>The actual physical attachment or control relationship; never an inferred effect recipient.</summary>
public sealed record PersistentCardRelation(string Kind, int? HostId, int? Controller);
