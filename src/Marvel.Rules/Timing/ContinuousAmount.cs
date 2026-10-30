using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>A live lasting magnitude with captured players and explicit board relations.</summary>
public abstract record ContinuousAmount
{
    private ContinuousAmount() { }

    /// <summary>Every upgrade currently controlled by the captured player.</summary>
    public sealed record ControlledUpgrades(int Player) : ContinuousAmount;

    /// <summary>Engaged minions currently carrying the specified effective trait.</summary>
    public sealed record EngagedMinionsWithTrait(int Player, string Trait) : ContinuousAmount;

    /// <summary>Every card currently in the captured player's named play-area zone.</summary>
    public sealed record PlayerAreaCards(int Player, DeckType Area) : ContinuousAmount;

    /// <summary>A named counter pool on the current main scheme.</summary>
    public sealed record MainSchemeCounters(string Counter) : ContinuousAmount;

    /// <summary>Reads the current board without changing the registration or bindings.</summary>
    public long Evaluate(World world) => this switch
    {
        ControlledUpgrades count => world.Cards.LongCount(card =>
            DeckTypes.IsInPlay(card.Area.Type)
            && EffectiveCards.Kind(card, world.Facts) == CardKind.Upgrade
            && CardControl.ControllerOf(world, card) == count.Player),
        EngagedMinionsWithTrait count => world.Cards.LongCount(card =>
            card.Area.Type == DeckType.EngagedEnemiesArea
            && card.Area.PlayArea == PlayArea.Of(count.Player)
            && EffectiveCards.Kind(card, world.Facts) == CardKind.Minion
            && Traits.Has(world, card, count.Trait, world.Facts)),
        PlayerAreaCards count => world.Areas
            .Where(area => area.Type == count.Area && area.PlayArea == PlayArea.Of(count.Player))
            .Sum(area => (long)area.Cards.Count),
        MainSchemeCounters count => CounterValue(world, count),
        _ => throw new RulesNotImplementedException("unknown live lasting amount"),
    };

    private static long CounterValue(World world, MainSchemeCounters count)
    {
        var card = world.TheCardIn(DeckType.MainSchemesArea);
        if (card is null) return 0;
        return count.Counter == "allPurpose"
            ? card.Tokens.Where(pair => pair.Key.StartsWith("c_", StringComparison.Ordinal))
                .Sum(pair => pair.Value)
            : card.Tokens.GetValueOrDefault("c_" + count.Counter);
    }
}
