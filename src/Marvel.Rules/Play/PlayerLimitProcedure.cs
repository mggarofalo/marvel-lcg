using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>A read-only projection of a forced would-be-defeated interrupt.</summary>

internal static class PlayerLimitProcedure
{
    internal static Prompt ChooseAlly(World world, ICardFacts facts, int player)
    {
        var allies = ControlledAllies(world, player);
        long limit = StateFields.Modified(
            world, world.Seats[player].IdentityCard, "ally_limit", facts, world.Players);
        if (allies.Count <= limit)
        {
            throw new InvalidOperationException(
                $"player {player} no longer exceeds their ally limit");
        }

        return new Prompt(
            player,
            Question.Element,
            TimingPriority.Untimed,
            Steps.ChooseAllyForLimit,
            $"{world.Seats[player].Name} chooses an ally to discard",
            false,
            [.. allies.Select(ally => new Affordance(
                ally.ObjectId,
                "Discard",
                ally.ObjectId,
                player,
                facts.Title(ally.FaceId)))]);
    }

    internal static void DiscardAlly(
        World world, ICardFacts facts, int player, Decision input, List<GameEvent> events)
    {
        var ally = ControlledAllies(world, player)
            .FirstOrDefault(card => card.ObjectId == input.Affordance)
            ?? throw new RulesNotImplementedException(
                $"card {input.Affordance} was not offered for the ally limit");
        Discard.Card(world, ally, Steps.ChooseAllyForLimit, events);

        long limit = StateFields.Modified(
            world, world.Seats[player].IdentityCard, "ally_limit", facts, world.Players);
        if (ControlledAllies(world, player).Count > limit)
        {
            world.Agenda.Then(new PhaseStep(
                Steps.ChooseAllyForLimit,
                world.Agenda.Current?.Round ?? 0,
                0,
                Seat: player,
                Plan: true));
        }
    }

    private static List<Card> ControlledAllies(World world, int player) =>
    [
        .. world.Areas
            .Where(area => area.Type == DeckType.AlliesArea
                && area.PlayArea == PlayArea.Of(player))
            .SelectMany(area => area.Cards)
            .OrderBy(card => card.ObjectId),
    ];

    internal static Prompt ChooseRestricted(World world, ICardFacts facts, int player)
    {
        var restricted = RestrictedCards(world, facts, player);
        if (restricted.Count <= StateFieldCatalog.RestrictedLimit)
        {
            throw new InvalidOperationException(
                $"player {player} no longer exceeds their restricted-card limit");
        }

        return new Prompt(
            player,
            Question.Element,
            TimingPriority.Untimed,
            Steps.ChooseRestrictedCard,
            $"{world.Seats[player].Name} chooses a restricted card to discard",
            false,
            [.. restricted.Select(card => new Affordance(
                card.ObjectId,
                "Discard",
                card.ObjectId,
                player,
                facts.Title(card.FaceId)))]);
    }

    internal static void DiscardRestricted(
        World world, ICardFacts facts, PhaseStep step, Decision input,
        List<GameEvent> events)
    {
        var offered = step.ProcedureCandidates ?? [];
        var card = RestrictedCards(world, facts, step.Seat)
            .FirstOrDefault(candidate => candidate.ObjectId == input.Affordance
                && offered.Contains(candidate.ObjectId))
            ?? throw new RulesNotImplementedException(
                $"card {input.Affordance} was not offered for the restricted-card limit");

        Discard.Card(world, card, Steps.ChooseRestrictedCard, events);

        var remaining = RestrictedCards(world, facts, step.Seat);
        if (remaining.Count > StateFieldCatalog.RestrictedLimit)
        {
            DefeatProcedure.ScheduleProcedureChoice(world, step with
            {
                ProcedureCandidates = [.. remaining.Select(candidate => candidate.ObjectId)],
                OccurrenceId = null,
            });
        }
    }

    private static List<Card> RestrictedCards(World world, ICardFacts facts, int player) =>
    [
        .. world.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Where(card => card.Owner == player
                && StateFields.Modified(
                    world, card, "restricted", facts, world.Players) > 0)
            .OrderBy(card => card.ObjectId),
    ];

}
