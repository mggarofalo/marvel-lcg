using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>Supported scheme-selection procedures.</summary>
public enum AbilityThwartSelection
{
    /// <summary>Thwart all selected schemes.</summary>
    All,
    /// <summary>Choose different schemes with the operation's Aerial allowance.</summary>
    Different,
    /// <summary>Discard cards from hand to determine the thwart amount.</summary>
    LegalPractice,
}
