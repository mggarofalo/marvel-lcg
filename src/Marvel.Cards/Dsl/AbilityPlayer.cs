using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>Player relations implemented by the ability resolver.</summary>
public enum AbilityPlayer
{
    /// <summary>The occurrence's player.</summary>
    TriggerPlayer,
    /// <summary>The resolving player.</summary>
    You,
    /// <summary>The source's controller.</summary>
    Controller,
    /// <summary>The selected player's identity owner.</summary>
    ChosenPlayer,
    /// <summary>The player the source is engaged with.</summary>
    EngagedPlayer,
    /// <summary>The first player.</summary>
    FirstPlayer,
}
