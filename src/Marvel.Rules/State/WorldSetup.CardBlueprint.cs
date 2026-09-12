
namespace Marvel.Rules.State;

/// <summary>Where a card starts. What the rules need from a deal order.</summary>

/// <summary>One card to make, and where it goes.</summary>
/// <param name="Spec">Comma-separated face ids. One card, however many faces.</param>
/// <param name="Slot">Where it starts.</param>
/// <param name="Seat">The seat it belongs to, or -1 for the scenario.</param>
/// <remarks>
/// Coarser than the content layer's <c>CreationSource</c>, and deliberately: a
/// hero's signature cards and their aspect cards are two different questions
/// about deck-building and the same answer about where they go.
/// </remarks>
public sealed record CardBlueprint(string Spec, SetupSlot Slot, int Seat);
