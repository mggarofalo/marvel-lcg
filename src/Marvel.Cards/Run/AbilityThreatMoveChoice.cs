using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Offers ordered endpoints after evaluating both selector pools independently.</summary>
internal static class AbilityThreatMoveChoice
{
    internal static ImmutableArray<AbilityThreatMovePair> Pairs(
        IReadOnlyList<Card> sources, IReadOnlyList<Card> destinations, World world, long amount)
    {
        if (amount <= 0) return [];
        // rr:move.1: an element "cannot move to its same (current) placement".
        // Engine choice: object-id ordering gives every offered pair a deterministic handle.
        return [.. sources.Where(card => Scheme(card) && card.Tokens.GetValueOrDefault("k_threat") > 0
                && world.ThreatAbilities.CanRemoveThreat(world, card)).OrderBy(card => card.ObjectId)
            .SelectMany(from => destinations.Where(to => Scheme(to) && !ReferenceEquals(from, to))
                .OrderBy(to => to.ObjectId).Select(to => new AbilityThreatMovePair(
                    new(from, from.Area.Id, from.Incarnation), new(to, to.Area.Id, to.Incarnation))))];
    }

    private static bool Scheme(Card card) =>
        card.Area.Type is DeckType.SideSchemesArea or DeckType.MainSchemesArea;

    internal static Prompt Describe(AbilityResolutionState cast, AbilityEffect.MoveThreat move) =>
        new(cast.World.FirstPlayer, Question.Option, TimingPriority.Untimed, cast.Trigger,
            $"{cast.World.Facts.Title(cast.AbilityFace)}: choose where to move threat", false,
            [.. cast.ThreatMovePairs.Select((pair, index) => (pair, index))
                .Where(item => item.pair.Current).Select(item => Offer(cast, move, item.pair, item.index))]);

    private static Affordance Offer(AbilityResolutionState cast, AbilityEffect.MoveThreat move,
        AbilityThreatMovePair pair, int index)
    {
        string from = EffectiveCards.Title(pair.From.Card, cast.World.Facts);
        string to = EffectiveCards.Title(pair.To.Card, cast.World.Facts);
        long requested = move.All ? pair.From.Card.Tokens.GetValueOrDefault("k_threat")
            : AbilityStructuralQueries.Amount(move.Amount, cast.ExpressionContext());
        string amount = move.All ? $"all {requested} threat" : $"{requested} threat";
        string description = $"Move {amount} from {from} to {to}. Prevention and later effects can change the result.";
        return new(index, AbilityResolutionExecution.ChooseVerb, pair.From.Card.ObjectId,
            World.Scenario, $"{from} -> {to}", Description: description)
        { SelectionSourceId = cast.Source.ObjectId, DisplayLabel = $"{from} → {to}",
            CommitLabel = $"Move {amount} from {from} to {to}" };
    }

    internal static void Answer(AbilityResolutionExecution execution, AbilityResolutionState cast,
        AbilityEffect.MoveThreat move, Decision answer)
    {
        if (answer.IsDecline || answer.Affordance < 0 || answer.Affordance >= cast.ThreatMovePairs.Length
            || !cast.ThreatMovePairs[answer.Affordance].Current)
            throw new AbilityException("The offered threat-move endpoints are no longer current.");
        var pair = cast.ThreatMovePairs[answer.Affordance];
        cast.ThreatMovePairs = [];
        AbilityResolutionThreatMovement.Schedule(execution, move, cast, pair);
    }
}
