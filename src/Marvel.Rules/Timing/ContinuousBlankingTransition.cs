using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Plans state-based changes when a transition changes printed blanking.</summary>
internal sealed class ContinuousBlankingTransition
{
    private readonly ContinuousEffects effects;
    private readonly int[] blanks;
    private readonly IReadOnlyDictionary<int, long> health;
    private readonly Card[] lostUses;

    internal ContinuousBlankingTransition(ContinuousEffects effects)
    {
        this.effects = effects;
        blanks = effects.CapturePrintedBlanks();
        health = effects.CaptureCharacterHealthCore();
        lostUses = effects.LostUsesCandidates();
    }

    internal ContinuousEffectDeparture? Preflight()
    {
        if (blanks.SequenceEqual(effects.CapturePrintedBlanks())) return null;
        var lethal = effects.LethalAfterDerivedChange(health);
        if (lethal.Length > 0 && effects.world.Agenda.Occurrence is null)
            throw new Marvel.Rules.Play.RulesNotImplementedException(
                "printed blanking health defeat requires an enclosing occurrence");
        var roots = effects.RestoredUsesAfter(lostUses)
            .Concat(lethal)
            .DistinctBy(card => card.ObjectId).ToArray();
        return new ContinuousEffectCascadePreflight(effects).PreflightDepartures(roots, includeHostedCards: true, moveRoots: true);
    }

    internal void RecheckLimits() => effects.RecheckAllyLimitsAfterBlanking(blanks);
}
