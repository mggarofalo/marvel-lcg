using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>Areas supported by ability searches.</summary>
public enum AbilitySearchArea
{
    /// <summary>The encounter deck.</summary>
    EncounterDeck,
    /// <summary>The encounter discard pile.</summary>
    EncounterDiscardPile,
    /// <summary>The scenario's set-aside area.</summary>
    ScenarioSetAside,
    /// <summary>The resolving player's deck.</summary>
    YourDeck,
}
