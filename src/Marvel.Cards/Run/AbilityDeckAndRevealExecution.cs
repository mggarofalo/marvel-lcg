using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Executes effects that inspect or change decks and prepare reveals.</summary>
internal static class AbilityDeckAndRevealExecution
{
    internal static AbilityDeckAndRevealResult Run(
        AbilityEffect effect, AbilityDeckAndRevealContext context)
    {
        return effect switch
        {
            AbilityEffect.CardAction { Instruction: AbilityCardInstruction.Reveal } reveal =>
                RevealCard(Find(reveal.Selection, context), context),
            AbilityEffect.CardAction { Instruction: AbilityCardInstruction.RevealFromVillainDeck } reveal =>
                RevealFromVillainDeck(reveal, context),
            AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.RevealTop } =>
                RevealCard(EncounterDeck.TakeTop(
                    context.World, context.Trigger, context.Events), context),
            AbilityEffect.DealEncounterCard deal => DealEncounterCard(deal, context),
            AbilityEffect.DealEncounterCards deal => DealEncounterCards(deal, context),
            AbilityEffect.EngageTopAsMinion drones => EngageTopAsMinion(drones, context),
            AbilityEffect.DiscardUntil discard => AbilityDiscardUntilExecution.Run(discard, context),
            AbilityEffect.RecoverDiscardedByResource recovery =>
                RecoverDiscardedByResource(recovery, context),
            AbilityEffect.AttachCards attach => AbilityAttachmentExecution.Run(attach, context),
            AbilityEffect.ReleaseBeneath release => AbilityBeneathReleaseExecution.Run(release, context),
            AbilityEffect.StoreBeneath store => AbilityBeneathExecution.Run(store, context),
            AbilityEffect.InitializeAuxiliaryDeck or AbilityEffect.RevealAuxiliaryTop =>
                AbilityAuxiliaryDeckExecution.Run(effect, context),
            AbilityEffect.CardAction { Instruction: AbilityCardInstruction.PlaceOnBottomOfDeck } bottom =>
                AbilityDeckBottomMovement.Run(bottom.Selection, context),
            AbilityEffect.Shuffle shuffle => Shuffle(shuffle, context),
            AbilityEffect.ShuffleInto shuffle => AbilityShuffleIntoExecution.Run(shuffle, context),
            AbilityEffect.Search search => Search(search, context),
            AbilityEffect.RecruitDiscarded recruit => AbilityDiscardedRecruitment.Run(recruit, context),
            AbilityEffect.PutIntoPlay placement => PutIntoPlay(placement, context),
            _ => AbilityDeckAndRevealResult.NotHandled,
        };
    }

    private static AbilityDeckAndRevealResult RevealFromVillainDeck(
        AbilityEffect.CardAction effect, AbilityDeckAndRevealContext context)
    {
        var villain = Find(effect.Selection, context);
        return RevealCard(villain is null ? null : EncounterDeck.TakeTopFromVillain(
            context.World, villain, context.Trigger, context.Events), context);
    }

    internal static AbilityDeckAndRevealResult RevealCard(
        Card? card, AbilityDeckAndRevealContext context)
    {
        var reveal = AbilityEncounterReveal.Prepare(card, context);
        return new(true, reveal is not null, reveal,
            ImmutableDictionary<string, long>.Empty);
    }

    /// <summary>Deals a specific encounter card facedown â€” <c>rr:deal</c>.</summary>
    private static AbilityDeckAndRevealResult DealEncounterCard(
        AbilityEffect.DealEncounterCard deal, AbilityDeckAndRevealContext context)
    {
        Rules.Play.Deal.EncounterCard(
            context.World,
            Find(deal.Card, context) ?? throw new RulesNotImplementedException(
                $"'{context.Source.FaceId}' cannot find the encounter card to deal"),
            Player(deal.Player, context),
            context.Trigger,
            context.Events);
        return AbilityDeckAndRevealResult.Handled;
    }

    /// <summary>Deals facedown encounter cards in player order â€” <c>rr:deal</c>.</summary>
    private static AbilityDeckAndRevealResult DealEncounterCards(
        AbilityEffect.DealEncounterCards deal, AbilityDeckAndRevealContext context)
    {
        var players = Players(deal.Players, context).ToList();
        for (long dealt = 0; dealt < deal.Count; dealt++)
        {
            foreach (int player in players)
            {
                if (Rules.Play.Deal.EncounterCard(
                        context.World, player, context.Trigger, context.Events) is null)
                    return AbilityDeckAndRevealResult.Handled;
            }
        }
        return AbilityDeckAndRevealResult.Handled;
    }

    private static AbilityDeckAndRevealResult EngageTopAsMinion(
        AbilityEffect.EngageTopAsMinion drones, AbilityDeckAndRevealContext context)
    {
        foreach (int player in Players(drones.Players, context))
        {
            for (long created = 0; created < drones.Count; created++)
                FacedownMinions.EngageTop(context.World, player,
                    context.Program.Profiles[drones.Profile], context.Trigger,
                    "Create_Facedown_Minion", context.Events);
        }
        return AbilityDeckAndRevealResult.Handled;
    }

    private static AbilityDeckAndRevealResult RecoverDiscardedByResource(
        AbilityEffect.RecoverDiscardedByResource recovery,
        AbilityDeckAndRevealContext context)
    {
        var hand = context.World.Seats[context.Player].Hand;
        foreach (var card in context.Discarded.Where(card =>
                     Resources.GeneratedBy(card.FaceId, context.World.Facts)
                         .Contains(recovery.Resource)))
        {
            var from = card.Area;
            World.MoveToTop(card, hand);
            context.Events.Add(new CardsMoved(
                Places.Reference(from), Places.Reference(hand),
                [new Landing(card.ObjectId, hand.Cards.Count - 1)])
            {
                Trigger = context.Trigger,
                Verb = "Add_To_Hand",
            });
        }
        return AbilityDeckAndRevealResult.Handled;
    }

    /// <summary>Shuffles one named deck â€” <c>rr:shuffle</c>.</summary>
    /// <remarks>
    /// This is a separate effect after an answered search choice because
    /// <c>rr:search.3</c> shuffles upon completion of that search.
    /// </remarks>
    private static AbilityDeckAndRevealResult Shuffle(
        AbilityEffect.Shuffle shuffle, AbilityDeckAndRevealContext context) =>
        new(true, context.World.Shuffle(Area(shuffle.Area, context)), null,
            ImmutableDictionary<string, long>.Empty);

    /// <summary>Searches without moving inspected cards â€” <c>rr:search.2</c>.</summary>
    /// <remarks>
    /// A found card leaves before <c>rr:search.3</c> shuffles the searched deck.
    /// Several matches require the player choice in <c>rr:search.1</c>, which
    /// remains an explicit unsupported boundary.
    /// </remarks>
    private static AbilityDeckAndRevealResult Search(
        AbilityEffect.Search search, AbilityDeckAndRevealContext context)
    {
        var areas = search.Areas.Select(where => Area(where, context)).ToList();
        var found = areas.SelectMany(area => area.Cards)
            .Where(card => string.Equals(
                card.FaceId, search.Face, StringComparison.Ordinal))
            .ToList();
        if (found.Count > 1)
        {
            throw new RulesNotImplementedException(
                $"'{context.Source.FaceId}' searched and found {found.Count} copies of "
                + $"'{search.Face}'; rr:search.1 gives the player that choice and asking is "
                + "not implemented");
        }
        context.World.RecordInformation(InformationKind.Search);
        var reveal = found.Count == 1 ? AbilityEncounterReveal.Prepare(found[0], context) : null;
        bool applied = reveal is not null;
        foreach (var deck in areas.Where(area => area.Type == DeckType.EncounterDeck))
            applied |= context.World.Shuffle(deck);
        return new(true, applied, reveal,
            ImmutableDictionary<string, long>.Empty.Add("found", found.Count));
    }

    /// <summary>Puts a card into play without triggering When Revealed.</summary>
    /// <remarks>
    /// <c>rr:when-revealed-abilities.2</c> suppresses that ability, while
    /// <c>rr:enters-play</c> still applies the card's enters-play rules.
    /// </remarks>
    private static AbilityDeckAndRevealResult PutIntoPlay(
        AbilityEffect.PutIntoPlay placement, AbilityDeckAndRevealContext context)
    {
        var card = Find(placement.Card, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' would put a card into play that is not there");
        int player = placement.FirstPlayer ? context.World.FirstPlayer : context.Player;
        if (placement.PrintedDestination)
        {
            return AbilityDeckAndRevealResult.Handled with
            {
                Entry = EncounterEntryRequest.For(card, context.World.FirstPlayer,
                    new CardEntryPlacement(Verb: "Put_Into_Play")),
            };
        }
        if (EffectiveCards.Kind(card, context.World.Facts) == CardKind.Minion)
            return AbilityDeckAndRevealResult.Handled with
            {
                Entry = EncounterEntryRequest.For(card, player,
                    new CardEntryPlacement(Verb: "Put_Into_Play")),
            };
        AbilityCardStateExecution.PutIntoPlay(card, player, CardStateContext(context));
        return AbilityDeckAndRevealResult.Handled;
    }

    private static AbilityCardStateContext CardStateContext(
        AbilityDeckAndRevealContext context) =>
        new(context.Expressions, context.Trigger, context.Events,
            context.CardPlayAbilities, context.Readiness,
            new AbilityCardStateResult());

    private static Card? Find(
        AbilityCardSelection selection, AbilityDeckAndRevealContext context)
    {
        var evaluation = new AbilitySelectorEvaluation(context.Expressions.Bindings);
        return Publish(evaluation.Result(evaluation.Find(selection)), context.World);
    }

    private static Area Area(
        AbilitySearchArea area, AbilityDeckAndRevealContext context) => area switch
        {
            AbilitySearchArea.EncounterDeck => context.World.AreaOf(DeckType.EncounterDeck),
            AbilitySearchArea.EncounterDiscardPile =>
                context.World.AreaOf(DeckType.EncounterDiscardPile),
            AbilitySearchArea.ScenarioSetAside => context.World.AreaOf(DeckType.AsideDeck),
            AbilitySearchArea.YourDeck => context.World.Seats[context.Player].Deck,
        AbilitySearchArea.YourHand => context.World.Seats[context.Player].Hand,
        AbilitySearchArea.YourDiscardPile => context.World.AreaOf(DeckType.DiscardPile, PlayArea.Of(context.Player), cardOwner: context.Player),
            _ => throw new InvalidOperationException("Unknown compiled search area"),
        };

    private static IEnumerable<int> Players(
        AbilityPlayerSelection players, AbilityDeckAndRevealContext context) => players switch
        {
            AbilityPlayerSelection.AllPlayers => context.World.PlayerOrder,
            AbilityPlayerSelection.OnePlayer one =>
                [new AbilityExpressionEvaluation(context.Expressions,
                new AbilitySelectorEvaluation(context.Expressions.Bindings)).Seat(one.Player)],
            _ => throw new InvalidOperationException("Unknown compiled player selection"),
        };

    private static int Player(
        AbilityPlayer player, AbilityDeckAndRevealContext context) =>
        new AbilityExpressionEvaluation(context.Expressions,
            new AbilitySelectorEvaluation(context.Expressions.Bindings)).Seat(player);

    private static T Publish<T>(AbilityQueryResult<T> result, World world)
    {
        foreach (var observation in result.Information)
            world.RecordInformation(observation);
        return result.Value;
    }
}
