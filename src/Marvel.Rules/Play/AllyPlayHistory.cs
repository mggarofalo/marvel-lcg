using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Round history required by a constant discount for the first ally played.</summary>
public static class AllyPlayHistory
{
    /// <summary>Engine-owned constant-effect key for the printed first-ally discount.</summary>
    public const string Reduction = "firstAllyCostReduction";
    private const string Played = "allyPlayedThisRound";

    internal static void Record(World world, ICardPlayAbilities abilities, Card card)
    {
        if (EffectiveCards.Kind(card, world.Facts) != CardKind.Ally
            || !abilities.RequiresAllyPlayHistory(world) || HasPlayed(world)) return;
        // Printed 03001b and its official FAQ count the first ally even while the
        // discount's identity face is inactive. The marker has no departing source.
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: Played, Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound)));
    }

    internal static long AdjustCost(World world, Card card, long amount)
    {
        if (EffectiveCards.Kind(card, world.Facts) != CardKind.Ally
            || !world.CardPlayAbilities.RequiresAllyPlayHistory(world) || HasPlayed(world)) return amount;
        foreach (var grant in world.Effects.Active().Where(effect => effect.Kind == Reduction))
            amount = Math.Max(0, amount - grant.Amount);
        return amount;
    }

    private static bool HasPlayed(World world) => world.Effects.Active().Any(effect => effect.Kind == Played);
}
