using Marvel.Rules.State;
using Marvel.Rules.Play;

namespace Marvel.Rules.Timing;

/// <summary>Lifetime and read policy for directly blanked printed text.</summary>
public static class PrintedText
{
    /// <summary>The engine-chosen persisted spelling of a printed-text blank.</summary>
    public const string Blanked = "printedTextBlanked";

    /// <summary>Reads only registered blanks, avoiding recursive constant derivation.</summary>
    public static bool IsBlanked(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        return world.Effects.entries.Any(entry =>
            !world.Effects.suppressed.Contains(entry)
            && entry.Effect.Source == EffectSource.LastingEffect
            && entry.Effect.Kind == Blanked
            && entry.Effect.Affects == card.ObjectId
            && entry.Effect.TargetIncarnation == card.Incarnation
            && DeckTypes.IsInPlay(card.Area.Type));
    }

    /// <summary>Printed keyword quantities; basic powers and icons outside the text box remain.</summary>
    public static bool IsTextKeywordField(string field) => field is
        "alliance" or "guard" or "hinder" or "incite" or "patrol" or "peril"
        or "permanent" or "quickstrike" or "restricted" or "retaliate" or "stalwart"
        or "steady" or "surge" or "teamwork" or "temporary" or "toughness"
        or "victory" or "villainous" or "vulnerable";

    /// <summary>The supported direct blank is a live nonpermanent minion.</summary>
    public static bool CanBlank(World world, Card target) =>
        world.Cards.Contains(target) && DeckTypes.IsInPlay(target.Area.Type)
        && EffectiveCards.Kind(target, world.Facts) == CardKind.Minion
        && StateFields.Modified(world, target, "permanent", world.Facts, world.Players) == 0;

    /// <summary>Proves that ending this minion's constants can settle before costs are paid.</summary>
    public static bool CanBeginBlank(World world, Card target)
    {
        if (!CanBlank(world, target)) return false;
        _ = world.Effects.PreflightConstantsEnding(target, includeHostedCards: false);
        return true;
    }

    /// <summary>Creates immutable direct blanking data for the current in-play copy.</summary>
    public static ContinuousEffect UntilEndOfPhase(World world, Card source, Card target)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        if (!world.Cards.Contains(source) || !world.Cards.Contains(target)
            || !DeckTypes.IsInPlay(target.Area.Type))
            throw new RulesNotImplementedException("printed-text blanking requires a live target in this world");
        if (!CanBlank(world, target))
            throw new RulesNotImplementedException("printed-text blanking supports nonpermanent minions only");
        return new ContinuousEffect(EffectSource.LastingEffect, Blanked,
            Card: source.ObjectId, Affects: target.ObjectId,
            Lasts: Duration.UntilEndOf(TimingPoints.EndOfPhase))
        { TargetIncarnation = target.Incarnation };
    }
}
