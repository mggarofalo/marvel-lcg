using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>A fixed player relation or each active player in player order.</summary>
public abstract record AbilityPlayerSelection
{
    private AbilityPlayerSelection() { }
    /// <summary>One player identified by a supported relation.</summary>
    public sealed record OnePlayer(AbilityPlayer Player) : AbilityPlayerSelection;
    /// <summary>Every active player in player order.</summary>
    public sealed record AllPlayers : AbilityPlayerSelection;
}
