using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Moves a physical encounter-mill match without searching the current discard pile.</summary>
internal static class AbilityDiscardedRecruitment
{
    internal static AbilityDeckAndRevealResult Run(
        AbilityEffect.RecruitDiscarded effect, AbilityDeckAndRevealContext context)
    {
        var matches = effect.Trait is { } trait
            ? context.Discarded.Where(card => context.World.Facts.Kind(card.FaceId) == CardKind.Minion
                && context.World.Facts.PrintedTraits(card.FaceId).Contains(trait, StringComparer.Ordinal))
                .Take(1).ToArray()
            : context.Discarded.Where(card => card.FaceId == effect.Face).Distinct().ToArray();
        if (matches.Length > 1)
            throw new RulesNotImplementedException(
                $"'{context.Source.FaceId}' recruits multiple discarded copies of '{effect.Face}'");
        var result = new AbilityDeckAndRevealResult(true, matches.Length == 1, null,
            ImmutableDictionary<string, long>.Empty.Add("found", matches.Length));
        if (matches.Length == 0) return result;
        var found = matches[0];
        if (context.World.Facts.Kind(found.FaceId) != CardKind.Minion)
            throw new RulesNotImplementedException($"'{context.Source.FaceId}' recruits a non-minion");
        // rr:encounter-deck.1 resets immediately. The matching physical card can
        // therefore already be in the replacement deck when later text moves it.
        // rr:when-revealed-abilities.2: putting a card into play does not reveal it.
        return result with
        {
            Entry = EncounterEntryRequest.For(found, context.Player,
                new CardEntryPlacement(Verb: "Put_Into_Play")),
        };
    }
}
