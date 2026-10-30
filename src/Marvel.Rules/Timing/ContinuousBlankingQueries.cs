using Marvel.Rules.Play;

namespace Marvel.Rules.Timing;

/// <summary>Observes changes to blanking for state-based limit checks.</summary>
internal static class ContinuousBlankingQueries
{
    internal static int[] CapturePrintedBlanks(this ContinuousEffects effects) => effects.Active()
        .Where(effect => effect.Kind == PrintedText.Blanked && effect.Affects is not null)
        .Select(effect => effect.Affects!.Value).Distinct().Order().ToArray();

    internal static void RecheckAllyLimitsAfterBlanking(this ContinuousEffects effects, IReadOnlyList<int> before)
    {
        if (before.SequenceEqual(effects.CapturePrintedBlanks())) return;
        foreach (int player in effects.world.PlayerOrder)
            CardEntry.CheckAllyLimit(effects.world, effects.world.Facts, player);
    }
}
