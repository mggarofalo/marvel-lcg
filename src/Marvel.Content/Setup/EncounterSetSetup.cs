namespace Marvel.Content.Setup;

/// <summary>One named encounter set from the authored setup dataset.</summary>
/// <param name="Name">The printed display name.</param>
/// <param name="Cards">The set's cards, in printed composition order.</param>
public sealed record EncounterSetSetup(string Name, IReadOnlyList<string> Cards);
