using static Marvel.Cards.Run.AbilitySelectorTrace;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityChoiceAnalysis;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityPowerTrace;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityProjection;
using static Marvel.Cards.Run.AbilityRepeatedEffectAnalysis;
using static Marvel.Cards.Run.AbilityResolutionAdmission;
using static Marvel.Cards.Run.AbilityInitiation;
using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

using static Marvel.Cards.Run.AbilityRepeatedDamageTrace;
using static Marvel.Cards.Run.AbilityRepeatedDamageAnalysis;
using static Marvel.Cards.Run.AbilityRepeatedSelectorTrace;
using static Marvel.Cards.Run.AbilityRepeatedStatusTrace;
namespace Marvel.Cards.Run;

internal static class AbilityRepeatedCharacteristicTrace
{
    internal static bool TraceHasTrait(
        Card current, string trait, AbilityAdmissionScope cast, HashSet<int> discarded,
        Dictionary<int, HashSet<string>>? traits = null)
    {
        if (traits?.TryGetValue(current.ObjectId, out var gained) == true
            && gained.Contains(trait))
        {
            return true;
        }
        if (EffectiveCards.InherentTraits(current, cast.World.Facts)
            .Contains(trait, StringComparer.Ordinal))
        {
            return true;
        }

        string grantedKind = Rules.State.Traits.Granted + trait;
        if (HasActiveGrantedTrait(current, grantedKind, cast, discarded))
        {
            return true;
        }
        return HasCarriedGrantedTrait(current, grantedKind, cast, discarded);
    }

    private static bool HasActiveGrantedTrait(
        Card current, string grantedKind, AbilityAdmissionScope cast,
        HashSet<int> discarded) =>
        cast.World.Effects.Active().Any(effect =>
            effect.Affects == current.ObjectId
            && ConstantSourceIsActive(effect, discarded)
            && string.Equals(effect.Kind, grantedKind, StringComparison.Ordinal));

    private static bool ConstantSourceIsActive(
        ContinuousEffect effect, HashSet<int> discarded) =>
        effect.Source != EffectSource.ConstantAbility
        || effect.Card is not int source
        || !discarded.Contains(source);

    private static bool HasCarriedGrantedTrait(
        Card current, string grantedKind, AbilityAdmissionScope cast,
        HashSet<int> discarded)
    {
        int boardVillain = AbilityProjectedAttachmentHosts.BoardVillain(current, cast);
        var carriedIds = TraceCarriedAttachments(current, cast, discarded)
            .Select(card => card.ObjectId).ToHashSet();
        return cast.World.Effects.Active().Any(effect =>
            effect.Affects == boardVillain
            && effect.Card is int source
            && carriedIds.Contains(source)
            && string.Equals(effect.Kind, grantedKind, StringComparison.Ordinal));
    }

    internal static List<Card> TraceCarriedAttachments(
        Card current, AbilityAdmissionScope cast, HashSet<int> discarded)
    {
        int boardVillain = AbilityProjectedAttachmentHosts.BoardVillain(current, cast);
        if (boardVillain < 0 || !string.Equals(
                cast.World.Facts.Title(cast.World.Cards[boardVillain].FaceId),
                cast.World.Facts.Title(current.FaceId),
                StringComparison.Ordinal))
        {
            return [];
        }
        return
        [
            .. cast.World.Areas
                .Where(area => area.Host == boardVillain
                    || area.Host == current.ObjectId)
                .SelectMany(area => area.Cards)
                .Where(card => !discarded.Contains(card.ObjectId)),
        ];
    }

    internal static long TraceModified(
        Card current, string field, AbilityAdmissionScope cast, HashSet<int> discarded,
        Dictionary<(int Card, string Field), long>? modifiers = null)
    {
        if (IsUnmodifiableDashPower(current, field, cast))
        {
            // `rr:dash-value.3`: a referenced dash is an unmodifiable zero.
            // Match StateFields.Modified's early return before applying either
            // trace-local or carried modifiers.
            return 0;
        }

        string? printed = field switch
        {
            "attack" => "ATK+",
            "scheme" => "SCH+",
            "thwart" => "THW+",
            _ => null,
        };
        long value = CardValues.EvaluateUnbounded(
                cast.World, current, field, cast.World.Facts, cast.World.Players).CurrentValue
            + (modifiers?.GetValueOrDefault((current.ObjectId, field)) ?? 0);
        value -= DiscardedAttachmentModifier(current, printed, cast, discarded);
        value -= DiscardedConstantModifier(current, field, cast, discarded);

        int boardVillain = AbilityProjectedAttachmentHosts.BoardVillain(current, cast);
        if (current.ObjectId == boardVillain)
        {
            return ApplyMaximum(current, field, cast, discarded, value);
        }

        var carried = TraceCarriedAttachments(current, cast, discarded);
        return ApplyMaximum(current, field, cast, discarded,
            value + CarriedModifier(carried, boardVillain, printed, field, cast));
    }

    private static long ApplyMaximum(Card current, string field, AbilityAdmissionScope cast,
        HashSet<int> discarded, long value)
    {
        var maximum = CardValues.Maximum(cast.World, current, field, discarded);
        return maximum is null ? value : Math.Min(value, maximum.Amount);
    }

    private static bool IsUnmodifiableDashPower(
        Card current, string field, AbilityAdmissionScope cast) =>
        StateFieldCatalog.FilledFrom.TryGetValue(field, out string? attribute)
        && attribute is "ATK" or "THW" or "DEF" or "REC" or "SCH"
        && !StateFieldCatalog.HasUsablePrintedPower(
            cast.World.Facts, current.FaceId, attribute);

    private static long DiscardedAttachmentModifier(
        Card current, string? printed, AbilityAdmissionScope cast,
        HashSet<int> discarded) =>
        printed is null ? 0 : cast.World.Areas
            .Where(area => area.Host == current.ObjectId)
            .SelectMany(area => area.Cards)
            .Where(card => discarded.Contains(card.ObjectId))
            .Sum(card => cast.World.Facts.PrintedValue(
                card.FaceId, printed, cast.World.Players));

    private static long DiscardedConstantModifier(
        Card current, string field, AbilityAdmissionScope cast,
        HashSet<int> discarded) =>
        cast.World.Effects.Active()
            .Where(effect => string.Equals(effect.Kind, field, StringComparison.Ordinal)
                && effect.AppliesTo(cast.World, current)
                && effect.Source == EffectSource.ConstantAbility
                && effect.Card is int source
                && discarded.Contains(source))
            .Sum(effect => effect.Amount);

    private static long CarriedModifier(
        List<Card> carried, int boardVillain, string? printed, string field,
        AbilityAdmissionScope cast)
    {
        long value = printed is null ? 0 : carried.Sum(card =>
            cast.World.Facts.PrintedValue(card.FaceId, printed, cast.World.Players));
        var carriedIds = carried.Select(card => card.ObjectId).ToHashSet();
        return value + cast.World.Effects.Active()
            .Where(effect => effect.Affects == boardVillain
                && effect.Card is int source
                && carriedIds.Contains(source)
                && string.Equals(effect.Kind, field, StringComparison.Ordinal))
            .Sum(effect => effect.Amount);
    }
}
