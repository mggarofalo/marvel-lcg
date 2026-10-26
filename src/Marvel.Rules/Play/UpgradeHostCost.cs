using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Prices upgrades against the exact physical attachment host.</summary>
public static class UpgradeHostCost
{
    /// <summary>Engine spelling for a constant reduction on upgrades played onto a host.</summary>
    public const string Reduction = "upgradeHostCostReduction";

    internal static long Adjust(World world, Card card, int? host, long amount)
    {
        if (host is null || EffectiveCards.Kind(card, world.Facts) != CardKind.Upgrade) return amount;
        var targets = world.CardPlayAbilities.AttachmentTargets(world, card);
        if (targets is null || !targets.Contains(host.Value)) return amount;
        // Printed 09039: "Reduce the cost to play each upgrade on Iron Man by 1."
        // The source is a physical card, not every character sharing its title or controller.
        foreach (var effect in world.Effects.Active().Where(effect =>
            effect.Kind == Reduction && effect.Affects == host && effect.Amount > 0))
            amount = Math.Max(0, amount - effect.Amount);
        return amount;
    }
}
