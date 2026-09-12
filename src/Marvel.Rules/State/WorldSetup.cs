
namespace Marvel.Rules.State;

/// <summary>Where a card starts. What the rules need from a deal order.</summary>
public enum SetupSlot
{
#pragma warning disable CS1591, SA1602
    Rules,
    Challenge,
    Identity,
    Obligation,
    Nemesis,
    PlayerDeck,
    MainScheme,
    Villain,
    Encounter,

    /// <summary>
    /// Set aside before setup, rather than shuffled into a deck —
    /// <c>rr:permanent.2</c> and <c>rr:setup-keyword.1</c>.
    /// </summary>
    SetAside,
#pragma warning restore CS1591, SA1602
}
