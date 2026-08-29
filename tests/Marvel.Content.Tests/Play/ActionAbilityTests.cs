using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Triggering an "Action" ability — <c>rr:player-turn.5</c>.
/// </summary>
/// <remarks>
/// <para>
/// The fifth of the six things a turn offers, and until now the engine had
/// none of it. <b>966 cards in the pool print an Action ability</b> — 445 of
/// them events, which are reached only this way: <c>rr:player-turn.5.d</c>
/// plays an event "by playing that event", and <c>.2</c>'s list of what may be
/// played from hand does not include events at all.
/// </para>
/// <para>
/// <b>An action is not in a window.</b> It happens because a player says so on
/// their turn, so it is offered beside the basic powers rather than in an
/// interrupt or a response — which is why <c>AbilityTypes.PriorityOf</c>
/// refuses to give it a tier.
/// </para>
/// </remarks>
public sealed class ActionAbilityTests
{
    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:player-turn.5")]
    [Rule("rr:support.2")]
    [Fact]
    public void AnActionIsOfferedOnTheTurnAndDoesWhatItSays()
    {
        // "Alter-Ego Action: Exhaust Aunt May → heal 4 damage from Peter
        // Parker." The whole path: offered among the turn options, taken, cost
        // paid, effect resolved, and the turn goes on. A support is "active
        // while it is in play", which is why Aunt May offers that action.
        Card? may = null;
        var (game, world) = Playing(board =>
        {
            may = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(5);
        });

        var identity = world.Seats[0].IdentityCard;

        var action = Assert.Single(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
        Assert.Equal(may!.ObjectId, action.AnchorId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(1, identity.Damage);
        Assert.False(may!.Ready);

        // `rr:player-turn`: every option but changing form may be taken "as
        // many times as the player is able", so the turn is still going.
        Assert.NotNull(game.Pending);
        Assert.Equal(Question.TurnOption, game.Pending.Asking);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:initiating-abilities.3")]
    [Rule("rr:damage.step.7")]
    [Rule("rr:triggering-condition.2")]
    [Fact]
    public void ASourceDefeatedByItsActionCostStillFinishesThatAction()
    {
        // "Action: Exhaust War Machine and deal 2 damage to him → deal 1
        // damage to each enemy." The cost is paid before the effect. At two
        // remaining hit points that cost defeats and discards War Machine, but
        // rr:initiating-abilities.3 says leaving play does not stop the
        // sequence, and the post-arrow effect still deals damage to every
        // enemy.
        Card? warMachine = null;
        var (game, world) = Playing(board =>
        {
            warMachine = board.CreateCard(
                "01030",
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            warMachine.TakeDamage(2);
        });
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb
                && option.AnchorId == warMachine!.ObjectId);
        var resolved = game.Resolve(Decision.Take(action.Id));

        Assert.Equal(DeckType.DiscardPile, warMachine!.Area.Type);
        Assert.Equal(1, villain.Damage);
        Assert.All(resolved.Events, happened => Assert.Equal(Steps.TurnAction, happened.Trigger));

        int discarded = resolved.Events
            .Select((happened, index) => (happened, index))
            .First(pair => pair.happened is CardsMoved moved
                && moved.Cards.Any(card => card.Card == warMachine.ObjectId))
            .index;
        int damaged = resolved.Events
            .Select((happened, index) => (happened, index))
            .First(pair => pair.happened is FieldSet changed
                && changed.Card == villain.ObjectId
                && changed.Field == "health")
            .index;
        Assert.True(discarded >= 0);
        Assert.True(damaged > discarded);
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    [Rule("rr:cost.11")]
    [Fact]
    public void DealingDamageAsACostIsPaidWhenTheDamageIsPrevented()
    {
        // Focused Rage deals one damage as its cost. Tough prevents all of
        // that damage, but dealing damage is still considered paid and the
        // post-arrow draw resolves.
        Card? rage = null;
        var (game, world) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TurnTo("01019a");
                rage = board.CreateCard(
                    "01027",
                    board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
                Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Tough);
            },
            heroes: ["she_hulk"]);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == rage!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:player-elimination.5")]
    [Rule("rr:player-elimination.step.5")]
    [Rule("rr:upgrade.1")]
    [Fact]
    public void APlayerEliminatedByAnActionCostFinishesItAndTheirTurn()
    {
        // An upgrade is "active so long as it is in play". Focused Rage's Hero
        // Action deals one damage to its player as a cost.
        // At one remaining hit point that eliminates She-Hulk, but the ability
        // still completes. She no longer participates, so the engine asks the
        // next player instead of constructing another prompt for seat zero.
        Card? rage = null;
        var (game, world) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TurnTo("01019a");
                board.Seats[0].IdentityCard.TakeDamage(14);
                rage = board.CreateCard(
                    "01027",
                    board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            },
            heroes: ["she_hulk", "spider_man"]);

        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb
                && option.AnchorId == rage!.ObjectId);
        var resolved = game.Resolve(Decision.Take(action.Id));

        Assert.True(world.Seats[0].Eliminated);
        Assert.False(world.IsOver);
        Assert.Equal(1, game.Active);
        Assert.Equal(1, game.Pending!.Player);
        Assert.Equal(Question.TurnOption, game.Pending.Asking);
        Assert.All(resolved.Events, happened => Assert.Equal(Steps.TurnAction, happened.Trigger));

        game.Resolve(Decision.Decline);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.Equal(1, game.Pending!.Player);
    }

    [Rule("rr:player-turn.5")]
    [Rule("rr:event")]
    [Rule("rr:initiating-abilities.step.1")]
    [Rule("rr:initiating-abilities.step.7")]
    [Fact]
    public void AnEventActionResumesItsOccurrenceAfterChoosingATarget()
    {
        Card? kick = null;
        Card? genius = null;
        Card? energy = null;
        Card? minion = null;
        var (game, world) = Playing(
            board =>
            {
                foreach (var card in board.Seats[0].Hand.Cards.ToList())
                {
                    World.MoveToTop(card, board.Seats[0].Deck);
                }

                kick = board.CreateCard(AuthoredCards.SwingingWebKick, board.Seats[0].Hand);
                genius = board.CreateCard("01089", board.Seats[0].Hand);
                energy = board.CreateCard("01088", board.Seats[0].Hand);
                minion = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == kick!.ObjectId);

        var paying = new[] { genius!.ObjectId, energy!.ObjectId };
        var suspended = game.Resolve(Decision.Take(action.Id, [], paying));
        Assert.Equal(Question.Element, suspended.Prompt!.Asking);
        Assert.Equal(DeckType.RevealingArea, kick!.Area.Type);
        Assert.Equal(PlayArea.Of(0), kick.Area.PlayArea);
        Assert.True(kick.FaceUp);
        Assert.False(DeckTypes.IsInPlay(kick.Area.Type));

        var finished = game.Resolve(Decision.Take(minion!.ObjectId));

        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(DeckType.DiscardPile, kick.Area.Type);
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(Question.TurnOption, finished.Prompt!.Asking);
        var damage = Assert.Single(finished.Events.OfType<FieldSet>(), happened =>
            happened.Card == minion.ObjectId && happened.Field == "health");
        Assert.Equal(Steps.TurnAction, damage.Trigger);
        Assert.Equal(
            paying,
            suspended.Events.OfType<CardsMoved>()
                .SelectMany(moved => moved.Cards)
                .Select(card => card.Card)
                .Where(paying.Contains));
    }

    [Rule("rr:ability.2")]
    [Rule("rr:in-play-and-out-of-play.4")]
    [Rule("rr:in-play-and-out-of-play.7")]
    [Rule("rr:in-play-and-out-of-play.8")]
    [Rule("rr:ownership-and-control.4")]
    [Fact]
    public void ActionsComeFromInPlayCardsAndEventsInHand()
    {
        // Hero, alter-ego, ally, upgrade, and support abilities "may only be
        // used if the card is in play"; cards in a player's hand, deck, and
        // discard pile are out of play. Events implicitly resolve from an
        // out-of-play area, so the event in hand is offered while Focused Rage
        // is silent from each of those three areas. The live copy is the
        // control that proves the card's action itself is reachable.
        Card? discardedRage = null;
        Card? heldRage = null;
        Card? deckRage = null;
        Card? liveRage = null;
        Card? kick = null;
        var (game, _) = Playing(
            board =>
            {
                discardedRage = board.CreateCard(
                    "01027",
                    board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
                heldRage = board.CreateCard("01027", board.Seats[0].Hand);
                deckRage = board.CreateCard("01027", board.Seats[0].Deck);
                liveRage = board.CreateCard(
                    "01027",
                    board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
                kick = board.CreateCard(AuthoredCards.SwingingWebKick, board.Seats[0].Hand);
                board.Seats[0].IdentityCard.TakeDamage(1);
            },
            hero: true);

        int[] outOfPlay =
            [discardedRage!.ObjectId, heldRage!.ObjectId, deckRage!.ObjectId];
        Assert.DoesNotContain(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && outOfPlay.Contains(option.AnchorId));
        Assert.Contains(game.Pending.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == liveRage!.ObjectId);
        Assert.Contains(
            game.Pending.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == kick!.ObjectId);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void ARejectedActionCommandDoesNotRemainOnTheAgenda()
    {
        Card? kick = null;
        Card? genius = null;
        Card? energy = null;
        var (game, world) = Playing(
            board =>
            {
                foreach (var card in board.Seats[0].Hand.Cards.ToList())
                {
                    World.MoveToTop(card, board.Seats[0].Deck);
                }

                kick = board.CreateCard(AuthoredCards.SwingingWebKick, board.Seats[0].Hand);
                genius = board.CreateCard("01089", board.Seats[0].Hand);
                energy = board.CreateCard("01088", board.Seats[0].Hand);
            },
            hero: true);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == kick!.ObjectId);

        Assert.Throws<RulesNotImplementedException>(
            () => game.Resolve(Decision.Take(action.Id)));

        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(DeckType.HandsArea, kick!.Area.Type);
        Assert.Equal(DeckType.HandsArea, genius!.Area.Type);
        Assert.Equal(DeckType.HandsArea, energy!.Area.Type);

        var retry = game.Resolve(Decision.Take(
            action.Id, [], [genius.ObjectId, energy.ObjectId]));
        Assert.Equal(Question.Element, retry.Prompt!.Asking);
    }

    [Rule("rr:choose-game-element.3.1")]
    [Rule("rr:player-turn.5")]
    [Fact]
    public void ACardSpecificChoiceSuspendsInsideTheActionOccurrence()
    {
        Card? practice = null;
        Card? discard = null;
        var (game, world) = Playing(board =>
        {
            var scheme = board.TheCardIn(DeckType.MainSchemesArea)!;
            scheme.PlaceTokens("k_threat", 2);
            practice = board.CreateCard("01023", board.Seats[0].Hand);
            discard = board.CreateCard("01087", board.Seats[0].Hand);
        });
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == practice!.ObjectId);

        var suspended = game.Resolve(Decision.Take(action.Id));

        Assert.Equal(Question.Element, suspended.Prompt!.Asking);
        Assert.Equal(Steps.ChooseOption, world.Agenda.Current!.Value.What);
        Assert.True(world.Agenda.Current.Value.Plan);
        var occurrence = Assert.IsType<Occurrence>(world.Agenda.Occurrence);
        Assert.True(occurrence.Is(Steps.TurnAction));
        Assert.Equal(practice!.ObjectId, occurrence.Subject);
        Assert.Contains(discard!, world.Seats[0].Hand.Cards);
    }

    [Rule("rr:player-turn.5.1")]
    [Rule("rr:ability.13")]
    [Fact]
    public void AnAlterEgoActionIsNotOfferedToAHero()
    {
        // "If the action ability is preceded by **Hero** or **Alter-Ego**, the
        // player must be in the specified form in order to trigger the
        // ability." 728 of the 966 in the pool are preceded by one.
        var (game, _) = Playing(
            board =>
            {
                InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(5);
            },
            hero: true);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
    }

    [Rule("rr:exhausted.2")]
    [Rule("rr:initiating-abilities.step.3")]
    [Fact]
    public void AnExhaustedCardCannotPayToExhaustItself()
    {
        // `rr:initiating-abilities.step.3` checks "the player's ability to pay"
        // before anything happens, and step 5 aborts "without paying any
        // costs". So an ability whose cost cannot be met is not offered at all
        // -- an affordance that would abort is a trap, not an offer.
        var (game, _) = Playing(board =>
        {
            InPlay(board, AuthoredCards.AuntMay).Exhaust();
            board.Seats[0].IdentityCard.TakeDamage(5);
        });

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
    }

    [Rule("rr:player-turn.5")]
    [Rule("rr:action.1")]
    [Fact]
    public void AnotherPlayersActionIsOfferedDirectlyAndResolvedByThem()
    {
        // "Ask another player to trigger any Action ability that player could
        // trigger on their own turn." The engine treats taking this direct
        // affordance as the other player accepting or offering. It is still
        // their action: their alter-ego form makes it legal, their Aunt May
        // exhausts, and their identity is healed.
        Card? may = null;
        var (game, world) = Playing(
            board =>
            {
                may = board.CreateCard(
                    AuthoredCards.AuntMay,
                    board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
                board.Seats[1].IdentityCard.TakeDamage(5);
            },
            heroes: ["spider_man", "she_hulk"]);

        var action = Assert.Single(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
        Assert.Equal(may!.ObjectId, action.AnchorId);
        Assert.Equal(1, action.AnchorPlayer);

        game.Resolve(Decision.Take(action.Id));

        Assert.False(may.Ready);
        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:player-turn.5.1")]
    [Rule("rr:player-turn.6")]
    [Rule("rr:limit")]
    [Fact]
    public void AnotherPlayersFormPaymentAndLimitRemainTheirs()
    {
        // Captain Marvel's Rechannel can be offered during Spider-Man's turn,
        // but Captain Marvel remains the resolving player. Her hero face makes
        // it available, her hand pays it, her damage is healed and her
        // once-per-round limit removes the re-offer.
        Card? energy = null;
        var (game, world) = Playing(
            board =>
            {
                board.Seats[1].IdentityCard.TurnTo("01010a");
                board.Seats[1].IdentityCard.TakeDamage(2);
                energy = board.CreateCard("01087", board.Seats[1].Hand);
            },
            heroes: ["spider_man", "captain_marvel"]);

        int captain = world.Seats[1].IdentityCard.ObjectId;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == captain);
        var price = Assert.Single(action.CostOptions);

        Assert.Equal(1, action.AnchorPlayer);
        Assert.Contains(price.Generators, source => source.Effect == energy!.ObjectId);
        Assert.DoesNotContain(
            price.Generators,
            source => world.Seats[0].Hand.Cards.Any(card => card.ObjectId == source.Effect));

        game.Resolve(Decision.Take(action.Id, [], [energy!.ObjectId]));

        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.DoesNotContain(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == captain);
    }

    [Rule("rr:alliance")]
    [Rule("rr:alliance.1")]
    [Rule("rr:alliance.2")]
    [Fact]
    public void AnAllianceEventUsesTheWholeTablesResourcesButItsPlayerResolvesIt()
    {
        // Any player may contribute while paying an Alliance card's costs,
        // but "only the player playing the card ... is considered to be
        // resolving that card." Player one supplies two of the three
        // resources; the effect still draws for player zero.
        const string alliance = "25036"; // Cosmic Alliance, cost 3.
        Card? card = null;
        Card? mine = null;
        Card? theirs = null;
        var runner = Runner(
            alliance,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""");
        var (game, world) = Playing(
            board =>
            {
                Hand(board, player: 0, Physicals, count: 0);
                Hand(board, player: 1, Mentals, count: 0);
                card = board.CreateCard(alliance, board.Seats[0].Hand);
                mine = board.CreateCard(Physicals, board.Seats[0].Hand);
                theirs = board.CreateCard("01088", board.Seats[1].Hand); // two energy
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == card!.ObjectId);
        var price = Assert.Single(action.CostOptions);
        Assert.Contains(price.Generators, source => source.Effect == mine!.ObjectId);
        Assert.Contains(price.Generators, source => source.Effect == theirs!.ObjectId);
        int playerZeroHand = world.Seats[0].Hand.Cards.Count;
        int playerOneHand = world.Seats[1].Hand.Cards.Count;

        game.Resolve(Decision.Take(
            action.Id, [], [mine!.ObjectId, theirs!.ObjectId]));

        Assert.Equal(DeckType.DiscardPile, card!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, mine.Area.Type);
        Assert.Equal(DeckType.DiscardPile, theirs.Area.Type);
        Assert.Equal(playerZeroHand - 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(playerOneHand - 1, world.Seats[1].Hand.Cards.Count);
    }

    [Rule("rr:alliance")]
    [Rule("rr:resource-ability.1")]
    [Fact]
    public void AnAllianceHelperCanUseTheirResourceAbility()
    {
        // Player one controls Peter Parker's Scientist resource ability. It is
        // offered as their contribution and is used by them, while player zero
        // remains the event's resolver.
        const string alliance = "25036"; // Cosmic Alliance, cost 3.
        Card? card = null;
        Card? doubleEnergy = null;
        var runner = Runner(
            alliance,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            includeAuthored: true);
        var (game, world) = Playing(
            board =>
            {
                Hand(board, player: 0, Physicals, count: 0);
                Hand(board, player: 1, Mentals, count: 0);
                card = board.CreateCard(alliance, board.Seats[0].Hand);
                doubleEnergy = board.CreateCard("01088", board.Seats[0].Hand);
            },
            heroes: ["captain_marvel", "spider_man"],
            abilities: runner);

        int scientist = world.Seats[1].IdentityCard.ObjectId;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == card!.ObjectId);
        var price = Assert.Single(action.CostOptions);
        Assert.Contains(price.Generators, source => source.Effect == scientist);

        game.Resolve(Decision.Take(
            action.Id, [], [doubleEnergy!.ObjectId, scientist]));

        Assert.Equal(DeckType.DiscardPile, card!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, doubleEnergy.Area.Type);
        Assert.DoesNotContain(
            runner.ResourceAbilities(world, 1), source => source.Effect == scientist);
    }

    [Fact]
    public void AnEventWithPrintedAndArrowCostsIsRejectedBeforePayment()
    {
        const string alliance = "25036";
        Card? card = null;
        var runner = Runner(
            alliance,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "spend": "Y" }""");
        var (game, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 4);
                card = board.CreateCard(alliance, board.Seats[0].Hand);
            },
            abilities: runner);
        var eventCard = Assert.IsType<Card>(card);

        Assert.DoesNotContain(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == eventCard.ObjectId);
        int[] payment = [.. world.Seats[0].Hand.Cards
            .Where(candidate => candidate.ObjectId != eventCard.ObjectId)
            .Select(candidate => candidate.ObjectId)];

        Assert.Throws<RulesNotImplementedException>(() => runner.Act(
            world,
            new PendingAbility(eventCard.ObjectId, AbilityType.Action, 0),
            payment,
            []));
        Assert.Same(world.Seats[0].Hand, eventCard.Area);
        Assert.All(payment, id => Assert.Same(world.Seats[0].Hand, world.Cards[id].Area));
    }

    [Fact]
    public void AWindowEventWithPrintedAndArrowCostsIsNotOfferedOrResolved()
    {
        Card? card = null;
        Card? energy = null;
        var runner = Runner(
            AuthoredCards.Backflip,
            "Interrupt",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "spend": "Y" }""",
            eventName: "WhenDamageWouldBeDealt");
        var (_, world) = Playing(
            board =>
            {
                Hand(board, AuthoredCards.Backflip, 0);
                card = board.CreateCard(AuthoredCards.Backflip, board.Seats[0].Hand);
                energy = board.CreateCard("01088", board.Seats[0].Hand);
            },
            hero: true,
            abilities: runner);
        var eventCard = Assert.IsType<Card>(card);
        var payment = Assert.IsType<Card>(energy);
        var occurrence = new Occurrence(
            1,
            ["WhenDamageWouldBeDealt"],
            Player: 0,
            Target: world.Seats[0].IdentityCard.ObjectId);

        Assert.Empty(runner.Waiting(world, occurrence, WindowKind.Interrupt));
        Assert.Throws<RulesNotImplementedException>(() => runner.Resolve(
            world,
            occurrence,
            new PendingAbility(eventCard.ObjectId, AbilityType.Interrupt, 0),
            [payment.ObjectId],
            []));
        Assert.Same(world.Seats[0].Hand, eventCard.Area);
        Assert.Same(world.Seats[0].Hand, payment.Area);
    }

    [Fact]
    public void UnlikeOverpaymentForAResourceSensitiveEventIsRejectedBeforePayment()
    {
        const string relentlessAssault = "01053"; // cost 2; physical payment grants overkill.
        Card? card = null;
        Card? doubleMental = null;
        Card? physical = null;
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                board.Seats[0].IdentityCard.TurnTo("01010a");
                card = board.CreateCard(relentlessAssault, board.Seats[0].Hand);
                doubleMental = board.CreateCard("01089", board.Seats[0].Hand);
                physical = board.CreateCard(Physicals, board.Seats[0].Hand);
                board.CreateCard(
                    AuthoredCards.Shocker,
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            heroes: ["captain_marvel"],
            abilities: runner);
        foreach (var extra in world.Seats[0].Hand.Cards
                     .Where(candidate => candidate != card
                         && candidate != doubleMental
                         && candidate != physical)
                     .ToList())
        {
            World.MoveToTop(extra, world.Seats[0].Deck);
        }
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);

        Assert.Contains(
            price.Generators, source => source.Effect == doubleMental!.ObjectId);
        Assert.DoesNotContain(
            price.Generators, source => source.Effect == physical!.ObjectId);

        Assert.Throws<RulesNotImplementedException>(() => runner.Act(
            world, action, [doubleMental!.ObjectId, physical!.ObjectId],
            [world.Cards.First(candidate => candidate.FaceId == AuthoredCards.Shocker).ObjectId]));

        Assert.Same(world.Seats[0].Hand, card!.Area);
        Assert.Same(world.Seats[0].Hand, doubleMental!.Area);
        Assert.Same(world.Seats[0].Hand, physical!.Area);
    }

    [Fact]
    public void WildOverpaymentForAResourceSensitiveEventIsRejectedBeforePayment()
    {
        const string relentlessAssault = "01053";
        Card? card = null;
        Card? doubleMental = null;
        Card? wild = null;
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                card = board.CreateCard(relentlessAssault, board.Seats[0].Hand);
                doubleMental = board.CreateCard("01089", board.Seats[0].Hand);
                wild = board.CreateCard("01011", board.Seats[0].Hand);
                board.CreateCard(
                    AuthoredCards.Shocker,
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        int target = world.Cards.First(
            candidate => candidate.FaceId == AuthoredCards.Shocker).ObjectId;

        Assert.Throws<RulesNotImplementedException>(() => runner.Act(
            world, action, [doubleMental!.ObjectId, wild!.ObjectId], [target]));

        Assert.Same(world.Seats[0].Hand, card!.Area);
        Assert.Same(world.Seats[0].Hand, doubleMental!.Area);
        Assert.Same(world.Seats[0].Hand, wild!.Area);
    }

    [Fact]
    public void AllWildOverpaymentStillNeedsEachWildsDeclaredType()
    {
        // The player may declare every paid wild as a non-physical type. The
        // source-only wire cannot infer Relentless Assault's physical bonus.
        const string relentlessAssault = "01053";
        Card? card = null;
        var wilds = new List<Card>();
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                board.Seats[0].IdentityCard.TurnTo("01010a");
                card = board.CreateCard(relentlessAssault, board.Seats[0].Hand);
                for (int index = 0; index < 3; index++)
                {
                    wilds.Add(board.CreateCard("01011", board.Seats[0].Hand));
                }
                board.CreateCard(
                    AuthoredCards.Shocker,
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            heroes: ["captain_marvel"],
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        int target = world.Cards.First(
            candidate => candidate.FaceId == AuthoredCards.Shocker).ObjectId;

        Assert.Throws<RulesNotImplementedException>(() => runner.Act(
            world,
            action,
            [.. wilds.Select(source => source.ObjectId)],
            [target]));

        Assert.Same(world.Seats[0].Hand, card!.Area);
        Assert.All(wilds, source => Assert.Same(world.Seats[0].Hand, source.Area));
    }

    [Fact]
    public void ExactWildPaymentStillNeedsTheWildsDeclaredType()
    {
        const string relentlessAssault = "01053";
        Card? card = null;
        Card? wild = null;
        Card? mental = null;
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                card = board.CreateCard(relentlessAssault, board.Seats[0].Hand);
                wild = board.CreateCard("01011", board.Seats[0].Hand);
                mental = board.CreateCard(Mentals, board.Seats[0].Hand);
                board.CreateCard(
                    AuthoredCards.Shocker,
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        int target = world.Cards.First(
            candidate => candidate.FaceId == AuthoredCards.Shocker).ObjectId;

        Assert.Throws<RulesNotImplementedException>(() => runner.Act(
            world, action, [wild!.ObjectId, mental!.ObjectId], [target]));

        Assert.Same(world.Seats[0].Hand, card!.Area);
        Assert.Same(world.Seats[0].Hand, wild!.Area);
        Assert.Same(world.Seats[0].Hand, mental!.Area);
    }

    [Rule("rr:requirement-resources")]
    [Fact]
    public void ARequirementCanForceAnUnambiguousWildDeclaration()
    {
        const string requiredEvent = "27016"; // cost 2, requirement physical.
        Card? card = null;
        Card? doubleWild = null;
        var runner = Runner(
            requiredEvent,
            "Action",
            """{ "if": { "test": { "paidWithResource": "R" }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        var (_, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                card = board.CreateCard(requiredEvent, board.Seats[0].Hand);
                doubleWild = board.CreateCard("01044", board.Seats[0].Hand);
            },
            heroes: ["captain_marvel"],
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);

        runner.Act(world, action, [doubleWild!.ObjectId], []);

        Assert.Equal(DeckType.DiscardPile, card!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, doubleWild.Area.Type);
    }

    [Rule("rr:requirement-resources")]
    [Fact]
    public void AForcedWildDeclarationIsCarriedIntoPaidResourceTests()
    {
        const string requiredEvent = "27016";
        Card? card = null;
        Card? wild = null;
        Card? mental = null;
        var runner = Runner(
            requiredEvent,
            "Action",
            """{ "if": { "test": { "paidWithResource": "Y" }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        var (_, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                card = board.CreateCard(requiredEvent, board.Seats[0].Hand);
                wild = board.CreateCard("01011", board.Seats[0].Hand);
                mental = board.CreateCard(Mentals, board.Seats[0].Hand);
            },
            heroes: ["captain_marvel"],
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        int held = world.Seats[0].Hand.Cards.Count;

        runner.Act(world, action, [wild!.ObjectId, mental!.ObjectId], []);

        // The requirement declared the wild physical, so the energy branch
        // did not draw. The event and both generators have left the hand.
        Assert.Equal(held - 3, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void EveryRepresentablePaidResourceChoiceRemainsAdvertised()
    {
        const string forJustice = "01060";
        Card? card = null;
        Card? doubleEnergy = null;
        Card? doubleMental = null;
        Card? safeTriple = null;
        Card? ambiguousTriple = null;
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                card = board.CreateCard(forJustice, board.Seats[0].Hand);
                doubleEnergy = board.CreateCard("01088", board.Seats[0].Hand);
                doubleMental = board.CreateCard("01089", board.Seats[0].Hand);
                safeTriple = board.CreateCard("01014", board.Seats[0].Hand);
                ambiguousTriple = board.CreateCard("21183", board.Seats[0].Hand);
                board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);

        Assert.Contains(price.Generators, source => source.Effect == doubleEnergy!.ObjectId);
        Assert.Contains(price.Generators, source => source.Effect == doubleMental!.ObjectId);
        Assert.Contains(price.Generators, source => source.Effect == safeTriple!.ObjectId);
        Assert.DoesNotContain(
            price.Generators, source => source.Effect == ambiguousTriple!.ObjectId);
    }

    [Fact]
    public void ARedundantSourceRemainsAdvertisedWhenItCannotChangeThePaidOutcome()
    {
        const string relentlessAssault = "01053";
        Card? card = null;
        Card? triplePhysical = null;
        Card? mental = null;
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                card = board.CreateCard(relentlessAssault, board.Seats[0].Hand);
                triplePhysical = board.CreateCard("10007", board.Seats[0].Hand);
                mental = board.CreateCard(Mentals, board.Seats[0].Hand);
                board.CreateCard(
                    AuthoredCards.Shocker,
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner);
        foreach (var extra in world.Seats[0].Hand.Cards
                     .Where(candidate => candidate != card
                         && candidate != triplePhysical
                         && candidate != mental)
                     .ToList())
        {
            World.MoveToTop(extra, world.Seats[0].Deck);
        }
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);

        Assert.Contains(
            price.Generators, source => source.Effect == triplePhysical!.ObjectId);
        Assert.Contains(price.Generators, source => source.Effect == mental!.ObjectId);

        runner.Act(
            world,
            action,
            [triplePhysical!.ObjectId, mental!.ObjectId],
            [world.Cards.First(candidate =>
                candidate.FaceId == AuthoredCards.Shocker).ObjectId]);

        Assert.Equal(DeckType.DiscardPile, triplePhysical.Area.Type);
        Assert.Equal(DeckType.DiscardPile, mental.Area.Type);
    }

    [Fact]
    public void ARemainingWildDeclarationChoiceIsNotOfferedOrInferred()
    {
        const string requiredEvent = "27016"; // one of two wilds must be physical.
        Card? card = null;
        Card? doubleWild = null;
        var runner = Runner(
            requiredEvent,
            "Action",
            """{ "if": { "test": { "paidWithResource": "G" }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        var (_, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                card = board.CreateCard(requiredEvent, board.Seats[0].Hand);
                doubleWild = board.CreateCard("01044", board.Seats[0].Hand);
            },
            heroes: ["captain_marvel"],
            abilities: runner);

        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        Assert.DoesNotContain(
            Assert.Single(runner.Describe(world, action).CostOptions).Generators,
            source => source.Effect == doubleWild!.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => runner.Act(
            world,
            action,
            [doubleWild!.ObjectId],
            []));
        Assert.Same(world.Seats[0].Hand, card!.Area);
        Assert.Same(world.Seats[0].Hand, doubleWild!.Area);
    }

    [Rule("rr:cost.5")]
    [Rule("rr:cost.5.1")]
    [Fact]
    public void OneDoubleResourceCanBeDividedBetweenSimultaneousCosts()
    {
        // Two energy costs on one ability are paid simultaneously. A single
        // card generating two energy icons supplies one to each cost and is
        // discarded once.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "seq": [ { "spend": "Y" }, { "spend": "Y" } ] }""");
        Card? source = null;
        Card? doubleEnergy = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                Hand(board, AuthoredCards.Backflip, 0);
                doubleEnergy = board.CreateCard("01088", board.Seats[0].Hand);
            },
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);
        var price = Assert.Single(action.CostOptions);

        Assert.Equal("2", price.Cost);
        Assert.Equal(["YY"], price.Rule);
        game.Resolve(Decision.Take(action.Id, [], [doubleEnergy!.ObjectId]));

        Assert.Equal(DeckType.DiscardPile, doubleEnergy.Area.Type);
        Assert.NotNull(game.Pending);
        Assert.Equal(Question.TurnOption, game.Pending.Asking);
    }

    [Rule("rr:cost.5")]
    [Rule("rr:cost.10")]
    [Fact]
    public void AnUnpayableSimultaneousCostChangesNoState()
    {
        // The resource half is invalid, so the exhaust half is not paid first.
        // The forged action is rejected with the source still ready.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "seq": [ { "exhaust": "this" }, { "spend": "B" } ] }""");
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                Hand(board, Physicals, 1);
            },
            abilities: runner);
        var forged = new PendingAbility(source!.ObjectId, AbilityType.Action, 0);

        Assert.Throws<RulesNotImplementedException>(
            () => runner.Act(world, forged, [world.Seats[0].Hand.Cards[0].ObjectId], []));
        Assert.True(source.Ready);
    }

    [Rule("rr:player-turn.5")]
    [Rule("rr:player-turn.6")]
    [Fact]
    public void ASharedEncounterActionIsOfferedForEveryEligiblePlayer()
    {
        // The request is implied, but the acting seat is material: that player
        // supplies the resources and resolves every reference to "you".
        Card? horn = null;
        var (game, _) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
                board.Seats[1].IdentityCard.TurnTo("01010a");
                horn = board.CreateCard(
                    AuthoredCards.IvoryHorn, board.AreaOf(DeckType.RevealingArea));
                board.Abilities = AuthoredCards.Runner();
                Reveal.Resolve(board, Cards, horn, 0, []);

                foreach (var card in board.Seats[0].Hand.Cards.ToList())
                {
                    World.MoveToTop(card, board.Seats[0].Deck);
                }

                Hand(board, player: 1, Physicals, count: 3);
            },
            heroes: ["spider_man", "captain_marvel"]);

        var actions = game.Pending!.Affordances
            .Where(option => option.Verb == Game.ActionVerb
                && option.AnchorId == horn!.ObjectId)
            .ToList();
        Assert.Equal([0, 1], actions.Select(action => action.AnchorPlayer));
        Assert.Equal(2, actions.Select(action => action.Id).Distinct().Count());
    }

    [Rule("rr:ability.8.2")]
    [Rule("rr:action.1.1")]
    [Fact]
    public void OnlyThePlayerHoldingAnObligationMayTriggerItsAction()
    {
        // An obligation remains an encounter card, but these clauses are the
        // exception to the general permission to use encounter-card actions.
        // The second player's turn can request another player's ordinary
        // action; it cannot request the obligation sitting in player zero's
        // play area.
        var runner = Runner(
            AuthoredCards.Hunted,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""");
        Card? obligation = null;
        var (_, world) = Playing(
            board => obligation = board.CreateCard(
                AuthoredCards.Hunted,
                board.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0))),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(runner.Actions(world, 0), action => action.Card == obligation!.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 1), action => action.Card == obligation!.ObjectId);
    }

    [Rule("rr:ability.8.1")]
    [Fact]
    public void OnlyTheHostControllerMayUseAnAttachmentAbilityThatSaysYou()
    {
        // The attachment belongs to the scenario. Its host is a player card,
        // and “you” makes that host's controller the only permitted player.
        var runner = Runner(
            AuthoredCards.PrelateArmor,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""");
        Card? attachment = null;
        var (_, world) = Playing(
            board => attachment = board.CreateCard(
                AuthoredCards.PrelateArmor,
                board.AreaOf(
                    DeckType.UpgradesArea,
                    PlayArea.Of(0),
                    host: board.Seats[0].IdentityCard.ObjectId)),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(runner.Actions(world, 0), action => action.Card == attachment!.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 1), action => action.Card == attachment!.ObjectId);
    }

    [Rule("rr:ability.8.1")]
    [Fact]
    public void YourInTheAttachmentInstructionRestrictsEveryAbilityOnTheCard()
    {
        // All Tied Up says “Attach to your identity card,” while its Action is
        // only “spend resources → discard this card.” The permission belongs
        // to the attachment's whole printed text, not only the selected
        // ability, so another player cannot trigger that Action.
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                """
                { "cards": [ { "card": "02048", "attachTo": { "query": "yourIdentity" }, "abilities": [ {
                    "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                    "effect": { "discard": "this" }
                } ] } ] }
                """));
        Card? attachment = null;
        var (_, world) = Playing(
            board => attachment = board.CreateCard(
                "02048",
                board.AreaOf(
                    DeckType.UpgradesArea,
                    PlayArea.Of(0),
                    host: board.Seats[0].IdentityCard.ObjectId)),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(runner.Actions(world, 0), action => action.Card == attachment!.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 1), action => action.Card == attachment!.ObjectId);
    }

    [Rule("rr:ability.8.1")]
    [Rule("rr:the-golden-rules")]
    [Fact]
    public void AnExplicitAnyPlayerPermissionOverridesTheAttachmentRestriction()
    {
        // Obedience Potion says “Attach to your identity,” then its Hero
        // Action ends “Any player can do this.” The printed exception lets the
        // other player initiate the ability and pay from their own hand.
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                """
                { "cards": [ { "card": "16123", "attachTo": { "query": "yourIdentity" }, "abilities": [ {
                    "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game", "form": "hero" },
                    "anyPlayer": true,
                    "cost": { "spend": "BB" },
                    "effect": { "discard": "this" }
                } ] } ] }
                """));
        Card? attachment = null;
        Card[] payment = [];
        var (_, world) = Playing(
            board =>
            {
                board.Seats[1].IdentityCard.TurnTo("01010a");
                attachment = board.CreateCard(
                    "16123",
                    board.AreaOf(
                        DeckType.UpgradesArea,
                        PlayArea.Of(0),
                        host: board.Seats[0].IdentityCard.ObjectId));
                Hand(board, player: 1, Mentals, count: 2);
                payment = [.. board.Seats[1].Hand.Cards.Take(2)];
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        var action = Assert.Single(
            runner.Actions(world, 1), option => option.Card == attachment!.ObjectId);
        runner.Act(world, action, [.. payment.Select(card => card.ObjectId)], []);

        Assert.Equal(DeckType.EncounterDiscardPile, attachment!.Area.Type);
        Assert.All(payment, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
    }

    [Rule("rr:player-turn.5")]
    [Fact]
    public void AnyPlayerMayUseAPlayerCardThatPrintsThatPermission()
    {
        // Player-turn option 5.c is exactly Plot Convenience's last line:
        // “Any player may trigger this ability.” The permission makes another
        // player's support visible, that player initiates it, and its printed
        // exhaust cost is paid before the effect resolves for that player.
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                """
                { "cards": [ { "card": "44050", "abilities": [
                  {
                    "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                    "anyPlayer": true,
                    "cost": { "exhaust": "this" },
                    "effect": { "draw": { "player": "you", "count": 1 } }
                  },
                  {
                    "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                    "effect": { "draw": { "player": "you", "count": 2 } }
                  }
                ] } ] }
                """));
        Card? support = null;
        var (_, world) = Playing(
            board => support = InPlay(board, "44050"),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        int held = world.Seats[1].Hand.Cards.Count;

        var action = Assert.Single(
            runner.Actions(world, 1), option => option.Card == support!.ObjectId);
        runner.Act(world, action, [], []);

        Assert.False(support!.Ready);
        Assert.Equal(held + 1, world.Seats[1].Hand.Cards.Count);
    }

    [Rule("rr:interrupt.1")]
    [Rule("rr:response.1")]
    [Fact]
    public void AnyPlayerWindowAbilitiesAreEvaluatedForEachPlayer()
    {
        var runner = Runner(
            "44050",
            "Response",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "spend": "B" }""",
            eventName: Steps.DamageDealt,
            anyPlayer: true);
        Card? support = null;
        Card[] payment = [];
        var (_, world) = Playing(
            board =>
            {
                support = InPlay(board, "44050");
                Hand(board, player: 0, Physicals, count: 0);
                Hand(board, player: 1, Mentals, count: 1);
                payment = [.. board.Seats[1].Hand.Cards];
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        int held = world.Seats[1].Hand.Cards.Count;

        var response = Assert.Single(
            runner.Waiting(
                world,
                new Occurrence(1, [Steps.DamageDealt], Player: 0),
                WindowKind.Response),
            option => option.Player == 1);

        Assert.Equal(1, response.Player);
        runner.Act(world, response, [payment[0].ObjectId], []);
        Assert.Equal(DeckType.DiscardPile, payment[0].Area.Type);
        Assert.Equal(held, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(support!.ObjectId, response.Card);
    }

    [Rule("rr:in-player-order.1")]
    [Rule("rr:response.1")]
    [Fact]
    public void AnyPlayerWindowAnswerResumesForThePlayerWhoAcceptedIt()
    {
        var runner = Runner(
            "44050",
            "Response",
            """{ "draw": { "player": "you", "count": 1 } }""",
            eventName: Steps.DamageDealt,
            anyPlayer: true);
        var (_, world) = Playing(
            board => InPlay(board, "44050"),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var occurrence = new Occurrence(1, [Steps.DamageDealt], Player: 0);
        var events = new List<GameEvent>();
        int firstHeld = world.Seats[0].Hand.Cards.Count;
        int secondHeld = world.Seats[1].Hand.Cards.Count;

        var first = Offering.Work(
            world, runner, occurrence, WindowKind.Response, events)!;
        Assert.Equal(0, first.Player);
        Sequence.Answer(world, Cards, runner, first, Decision.Decline, events);

        var second = Offering.Work(
            world, runner, occurrence, WindowKind.Response, events)!;
        Assert.Equal(1, second.Player);
        Sequence.Answer(
            world, Cards, runner, second,
            Decision.Take(Assert.Single(second.Affordances).Id), events);

        Assert.Equal(firstHeld, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(secondHeld + 1, world.Seats[1].Hand.Cards.Count);
    }

    [Rule("rr:ability.8")]
    [Fact]
    public void TriggerPlayerStillNarrowsAnAnyPlayerWindow()
    {
        var runner = Runner(
            "44050",
            "Response",
            """{ "draw": { "player": "you", "count": 1 } }""",
            eventName: Steps.DamageDealt,
            player: "trigger.player",
            anyPlayer: true);
        var (_, world) = Playing(
            board => InPlay(board, "44050"),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        var waiting = runner.Waiting(
            world,
            new Occurrence(1, [Steps.DamageDealt], Player: 0),
            WindowKind.Response);

        Assert.Equal(0, Assert.Single(waiting).Player);
    }

    [Rule("rr:ability.8.1")]
    [Fact]
    public void AnAttachmentsYouTriggerMatchesOnlyItsHostController()
    {
        var runner = Runner(
            AuthoredCards.PrelateArmor,
            "Response",
            """{ "draw": { "player": "you", "count": 1 } }""",
            eventName: "WhenAttacked",
            player: "you");
        Card? attachment = null;
        var (_, world) = Playing(
            board => attachment = board.CreateCard(
                AuthoredCards.PrelateArmor,
                board.AreaOf(
                    DeckType.UpgradesArea,
                    PlayArea.Of(0),
                    host: board.Seats[0].IdentityCard.ObjectId)),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        var ours = runner.Waiting(
            world, new Occurrence(1, ["WhenAttacked"], Player: 0), WindowKind.Response);
        var theirs = runner.Waiting(
            world, new Occurrence(2, ["WhenAttacked"], Player: 1), WindowKind.Response);

        Assert.Equal(0, Assert.Single(ours).Player);
        Assert.Empty(theirs);
    }

    [Rule("rr:ability.8.1")]
    [Rule("rr:ability.8.2")]
    [Fact]
    public void RestrictedResourceAbilitiesBelongOnlyToTheirPermittedPlayer()
    {
        var obligationRunner = Runner(
            AuthoredCards.Hunted,
            "Resource",
            """{ "generate": "E" }""");
        Card? obligation = null;
        var (_, obligationWorld) = Playing(
            board => obligation = board.CreateCard(
                AuthoredCards.Hunted,
                board.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0))),
            heroes: ["spider_man", "captain_marvel"],
            abilities: obligationRunner);

        Assert.Contains(
            obligationRunner.ResourceAbilities(obligationWorld, 0),
            source => source.Effect == obligation!.ObjectId);
        Assert.DoesNotContain(
            obligationRunner.ResourceAbilities(obligationWorld, 1),
            source => source.Effect == obligation!.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => obligationRunner.UseResource(
            obligationWorld, 1, obligation!.ObjectId, []));

        // Compound bindings are still printed “you/your”: this query means
        // allies controlled by the resolving player and restricts a
        // player-hosted attachment just as the bare word “you” does.
        var attachmentRunner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [ { "card": "{{AuthoredCards.PrelateArmor}}", "abilities": [ {
                    "trigger": { "event": "WhenActionTriggered", "timing": "Resource", "subject": "game" },
                    "when": { "exists": { "query": "alliesYouControl" } },
                    "effect": { "generate": "B" }
                } ] } ] }
                """));
        Card? attachment = null;
        var (_, attachmentWorld) = Playing(
            board =>
            {
                board.CreateCard(
                    "01002",
                    board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                attachment = board.CreateCard(
                    AuthoredCards.PrelateArmor,
                    board.AreaOf(
                        DeckType.UpgradesArea,
                        PlayArea.Of(0),
                        host: board.Seats[0].IdentityCard.ObjectId));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: attachmentRunner);

        Assert.Contains(
            attachmentRunner.ResourceAbilities(attachmentWorld, 0),
            source => source.Effect == attachment!.ObjectId);
        Assert.DoesNotContain(
            attachmentRunner.ResourceAbilities(attachmentWorld, 1),
            source => source.Effect == attachment!.ObjectId);

        // Player-relative semantics can also be the node name rather than a
        // word value. Test it in a real response occurrence whose target
        // makes `isYourIdentity` true for the host controller and false for
        // the other player.
        var kindRunner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [ { "card": "{{AuthoredCards.PrelateArmor}}", "abilities": [ {
                    "trigger": { "event": "WhenDamageWouldBeDealt", "timing": "Response", "subject": "game" },
                    "when": { "isYourIdentity": "trigger.target" },
                    "effect": { "draw": { "player": "you", "count": 1 } }
                } ] } ] }
                """));
        Card? kindAttachment = null;
        var (_, kindWorld) = Playing(
            board => kindAttachment = board.CreateCard(
                AuthoredCards.PrelateArmor,
                board.AreaOf(
                    DeckType.UpgradesArea,
                    PlayArea.Of(0),
                    host: board.Seats[0].IdentityCard.ObjectId)),
            heroes: ["spider_man", "captain_marvel"],
            abilities: kindRunner);

        var ours = kindRunner.Waiting(
            kindWorld,
            new Occurrence(
                1,
                ["WhenDamageWouldBeDealt"],
                Player: 0,
                Target: kindWorld.Seats[0].IdentityCard.ObjectId),
            WindowKind.Response);
        var theirs = kindRunner.Waiting(
            kindWorld,
            new Occurrence(
                2,
                ["WhenDamageWouldBeDealt"],
                Player: 1,
                Target: kindWorld.Seats[1].IdentityCard.ObjectId),
            WindowKind.Response);

        Assert.Equal(kindAttachment!.ObjectId, Assert.Single(ours).Card);
        Assert.Empty(theirs);
    }

    [Rule("rr:interrupt.1.1")]
    [Rule("rr:response.1.1")]
    [Theory]
    [InlineData("Interrupt")]
    [InlineData("Response")]
    public void AnotherPlayersObligationIsExcludedFromAbilityWindows(string timing)
    {
        var runner = Runner(
            AuthoredCards.Hunted,
            timing,
            """{ "draw": { "player": "you", "count": 1 } }""",
            eventName: "WhenAttacked");
        var (_, world) = Playing(
            board => board.CreateCard(
                AuthoredCards.Hunted,
                board.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0))),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        world.FirstPlayer = 1;
        var occurrence = new Occurrence(1, ["WhenAttacked"], Player: 1);

        var prompt = Offering.Work(
            world,
            runner,
            occurrence,
            timing == "Interrupt" ? WindowKind.Interrupt : WindowKind.Response,
            []);

        Assert.NotNull(prompt);
        Assert.Equal(0, prompt.Player);
    }

    [Rule("rr:action.2")]
    [Rule("rr:action.2.1")]
    [Rule("rr:forced.2")]
    [Fact]
    public void ALegalForcedActionMustResolveBeforeThePlayerPhaseEnds()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "ForcedAction",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);

        // It may be used at any ordinary action opportunity.
        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);

        // At the boundary it is no longer optional: the phase stays in the
        // player turn and the only answer is to resolve the Forced Action.
        game.Resolve(Decision.Decline);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.False(game.Pending!.Cancellable);
        var forced = Assert.Single(game.Pending.Affordances);

        int held = world.Seats[0].Hand.Cards.Count;
        game.Resolve(Decision.Take(forced.Id));

        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);

        // Its exhaust cost is now unpayable, so resolution continues directly
        // to the ordinary end phase. It must not reopen a normal turn.
        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.DoesNotContain(
            game.Pending!.Affordances,
            option => option.Verb is Game.ChangeForm or Game.ActionVerb);
    }

    [Rule("rr:action.2")]
    [Rule("rr:ability.8.2")]
    [Fact]
    public void APlayersForcedActionAsksThatPlayerToChooseItsPayment()
    {
        var runner = Runner(
            AuthoredCards.Hunted,
            "ForcedAction",
            """{ "discard": "this" }""",
            cost: """{ "discardFromHand": 1 }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = board.CreateCard(
                AuthoredCards.Hunted,
                board.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1))),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Decline);

        Assert.Equal(1, game.Pending!.Player);
        var forced = Assert.Single(game.Pending.Affordances);
        Assert.NotNull(forced.Targets);
        var targets = forced.Targets;
        Assert.All(
            targets.Legal,
            id => Assert.Contains(world.Cards[id], world.Seats[1].Hand.Cards));
        int p0Held = world.Seats[0].Hand.Cards.Count;
        var paid = world.Cards[targets.Legal[0]];

        game.Resolve(Decision.Take(forced.Id, [paid.ObjectId], []));

        Assert.Equal(p0Held, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, paid.Area.Type);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
    }

    [Rule("rr:action.2")]
    [Fact]
    public void ACostlessForcedActionNeedOnlyResolveOnceBeforeThePhaseEnds()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "ForcedAction",
            """{ "draw": { "player": "you", "count": 1 } }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);

        game.Resolve(Decision.Decline);
        var forced = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);
        int held = world.Seats[0].Hand.Cards.Count;

        game.Resolve(Decision.Take(forced.Id));

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
    }

    [Rule("rr:action.2")]
    [Rule("rr:player-elimination.step.5")]
    [Fact]
    public void EliminatingTheFirstPlayerDuringTheGateMovesTheEndPhaseToTheSurvivor()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "ForcedAction",
            """{ "dealDamage": { "cards": "you", "amount": 99 } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Decline);
        var forced = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(forced.Id));

        Assert.True(world.Seats[0].Eliminated);
        Assert.Equal(1, world.FirstPlayer);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.Equal(1, game.Pending!.Player);
    }

    [Rule("rr:action.2")]
    [Fact]
    public void ForcedActionsOnTwoFacesOfOneIdentityAreDistinctAbilities()
    {
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [
                  { "card": "{{AuthoredCards.SpiderMan}}", "abilities": [ {
                    "trigger": { "event": "WhenActionTriggered", "timing": "ForcedAction", "subject": "game" },
                    "limitPerRound": 1,
                    "effect": { "changeForm": { "player": "you", "to": "alter-ego" } }
                  } ] },
                  { "card": "01001b", "abilities": [ {
                    "trigger": { "event": "WhenActionTriggered", "timing": "ForcedAction", "subject": "game" },
                    "limitPerRound": 1,
                    "effect": { "draw": { "player": "you", "count": 1 } }
                  } ] }
                ] }
                """));
        var (game, world) = Playing(_ => { }, hero: true, abilities: runner);

        game.Resolve(Decision.Decline);
        var heroAction = Assert.Single(game.Pending!.Affordances);
        game.Resolve(Decision.Take(heroAction.Id));

        Assert.Equal("01001b", world.Seats[0].IdentityCard.FaceId);
        var alterEgoAction = Assert.Single(game.Pending!.Affordances);
        int held = world.Seats[0].Hand.Cards.Count;
        game.Resolve(Decision.Take(alterEgoAction.Id));

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
    }

    [Rule("rr:action.2")]
    [Rule("rr:leaves-play.1")]
    [Fact]
    public void AForcedActionOnACardThatLeavesAndReturnsBelongsToTheNewCopy()
    {
        var runner = Runner(
            "01101",
            "ForcedAction",
            """{ "seq": [ { "discard": "this" }, { "putIntoPlay": { "card": "this", "where": "engagedWithYou" } } ] }""",
            limit: 1);
        Card? source = null;
        var (game, _) = Playing(
            board => source = board.CreateCard(
                "01101",
                board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))),
            abilities: runner);
        int firstCopy = source!.Incarnation;

        game.Resolve(Decision.Decline);
        var forced = Assert.Single(game.Pending!.Affordances);
        game.Resolve(Decision.Take(forced.Id));

        Assert.True(source.Incarnation > firstCopy);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.False(game.Pending!.Cancellable);
        Assert.Contains(game.Pending.Affordances, option => option.AnchorId == source.ObjectId);
    }

    [Rule("rr:limit")]
    [Fact]
    public void LimitedAbilitiesOnOneCardHaveIndependentUses()
    {
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [ { "card": "{{AuthoredCards.AuntMay}}", "abilities": [
                  {
                    "trigger": { "event": "WhenActionTriggered", "timing": "ForcedAction", "subject": "game" },
                    "limitPerRound": 1,
                    "effect": { "draw": { "player": "you", "count": 1 } }
                  },
                  {
                    "trigger": { "event": "WhenActionTriggered", "timing": "ForcedAction", "subject": "game" },
                    "limitPerRound": 1,
                    "effect": { "draw": { "player": "you", "count": 1 } }
                  }
                ] } ] }
                """));
        var (game, world) = Playing(
            board => InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;

        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Take(game.Pending!.Affordances[0].Id));

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.False(game.Pending!.Cancellable);
        game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances).Id));

        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
    }

    [Rule("rr:event.1")]
    [Fact]
    public void PlayingAnEventChoosesOneOfItsTriggeredAbilities()
    {
        // Both actions belong to the same event. Their affordance ids retain
        // the printed ordinal, so choosing the second resolves only its two-card
        // draw and does not also resolve the first ability.
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                """
                { "cards": [ { "card": "01003", "abilities": [
                  { "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                    "effect": { "draw": { "player": "you", "count": 1 } } },
                  { "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                    "effect": { "draw": { "player": "you", "count": 2 } } }
                ] } ] }
                """));
        Card? eventCard = null;
        var (game, world) = Playing(
            board =>
            {
                Hand(board, AuthoredCards.Backflip, 0);
                eventCard = board.CreateCard(AuthoredCards.Backflip, board.Seats[0].Hand);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var choices = game.Pending!.Affordances
            .Where(option => option.AnchorId == eventCard!.ObjectId)
            .ToList();

        Assert.Equal(2, choices.Count);
        game.Resolve(Decision.Take(choices[1].Id));

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, eventCard!.Area.Type);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:event.3")]
    [Fact]
    public void AnEventWithNoValidTargetCannotBeOfferedOrForged()
    {
        // An event requiring a minion target cannot be initiated on a board
        // with no minions. The same check runs again at execution, before the
        // event leaves the hand or a payment source can be spent.
        var runner = Runner(
            AuthoredCards.Backflip,
            "Action",
            """{ "chooseCard": { "from": { "query": "minions" }, "effect": { "discard": "chosen" } } }""");
        Card? card = null;
        var (_, world) = Playing(
            board =>
            {
                Hand(board, AuthoredCards.Backflip, 0);
                card = board.CreateCard(AuthoredCards.Backflip, board.Seats[0].Hand);
            },
            hero: true,
            abilities: runner);

        Assert.DoesNotContain(
            runner.Actions(world, 0), action => action.Card == card!.ObjectId);

        var forged = new PendingAbility(card!.ObjectId, AbilityType.Action, 0);
        Assert.Throws<RulesNotImplementedException>(
            () => runner.Act(world, forged, [], []));
        Assert.Same(world.Seats[0].Hand, card.Area);
    }

    [Rule("rr:limit.1")]
    [Fact]
    public void ACancelledLimitedAttackStillUsesItsLimit()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "chooseCard": { "from": { "query": "attackableEnemies" }, "effect": { "attack": { "target": "chosen", "effect": { "dealAttackDamage": { "cards": "chosen", "amount": 1 } } } } } }""",
            limit: 1);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Stunned);
            },
            hero: true,
            abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(villain.ObjectId));

        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(0, villain.Damage);
        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:lasting-effects.6")]
    [Fact]
    public void AnUntilEndOfAttackEffectCannotBeginOutsideAnAttack()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "grantUntil": { "card": "this", "keyword": "attack", "amount": 1, "until": "EndOfAttack" } }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:action.2.2")]
    [Rule("rr:forced.3")]
    [Rule("rr:forced.3.1")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnIllegalForcedActionDoesNotPreventPhaseCompletion(bool targetless)
    {
        string effect = targetless
            ? """{ "chooseCard": { "from": { "query": "minions" }, "effect": { "draw": { "player": "you", "count": 1 } } } }"""
            : """{ "draw": { "player": "you", "count": 1 } }""";
        string? cost = targetless ? null : """{ "spend": "BBBBBBBBBBBB" }""";
        var runner = Runner(AuthoredCards.AuntMay, "ForcedAction", effect, cost: cost);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Decline);

        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.True(source!.Ready);
    }

    [Rule("rr:cost")]
    [Rule("rr:player-turn.5")]
    [Fact]
    public void AnEncounterCardsActionIsAnybodysAndCostsResources()
    {
        // "Attach to Rhino. **Hero Action**: Spend [physical][physical][physical]
        // resources → discard this card." An encounter card in play, so
        // `rr:player-turn.5.b` is what lets a player trigger it -- it is
        // nobody's card and everybody's action.
        Card? horn = null;
        var (game, world) = Playing(
            board =>
            {
                horn = board.CreateCard(
                    AuthoredCards.IvoryHorn, board.AreaOf(DeckType.RevealingArea));

                // Through the reveal: `rr:attach-to` makes "Attach to Rhino" a
                // rule about the card entering play rather than a "When
                // Revealed" ability, so the route in is what attaches it.
                board.Abilities = AuthoredCards.Runner();
                Reveal.Resolve(board, Cards, horn, 0, []);
                Physical(board, 3);
            },
            hero: true);

        var action = Assert.Single(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);

        // The cost is on the wire, with what could pay it -- a resource cost is
        // a choice of *which* cards, so the client has to be told.
        var price = Assert.Single(action.CostOptions);
        Assert.Equal("3", price.Cost);
        Assert.True(price.Generators.Count(source => source.Generates == "R") >= 3);

        int[] paying = [.. world.Seats[0].Hand.Cards
            .Where(card => card.FaceId == Physicals)
            .Take(3)
            .Select(card => card.ObjectId)];

        game.Resolve(Decision.Take(action.Id, [], paying));

        Assert.Equal(DeckType.EncounterDiscardPile, horn!.Area.Type);
        Assert.All(
            paying, id => Assert.Equal(DeckType.DiscardPile, world.Cards[id].Area.Type));
    }

    [Rule("rr:initiating-abilities.step.3")]
    [Fact]
    public void AnActionIsNotOfferedToAHandThatCannotPayIt()
    {
        // The whole hand cannot make three physicals, so the action is not
        // offered at all. `rr:cost.4` permits generating beyond the cost, so
        // asking the whole hand is the right question rather than an
        // approximation: if everything together cannot pay, no choice among it
        // can.
        var (game, _) = Playing(
            board =>
            {
                var horn = board.CreateCard(
                    AuthoredCards.IvoryHorn, board.AreaOf(DeckType.RevealingArea));
                AuthoredCards.Runner().WhenRevealed(board, horn, 0);
                Physical(board, 2);
            },
            hero: true);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
    }

    [Rule("rr:and")]
    [Rule("rr:and.1")]
    [Rule("rr:and.2")]
    [Rule("rr:first-player.3")]
    [Fact]
    public void AndEffectsResolveIndependentlyInsideOneAbility()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "and": [ { "dealDamage": { "cards": "you", "amount": 1 } }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Tough);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(Question.Order, game.Pending.Asking);
        game.Resolve(new Decision(order.Id, [0, 1]));

        // Tough independently prevents the damage effect. The draw connected
        // by “and” still resolves, and no response prompt separated the two.
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:first-player.3")]
    [Fact]
    public void FirstPlayerChoosesTheOrderOfAndEffects()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "and": [ { "exhaust": "this" }, { "ready": "this" } ] }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [1, 0]));

        Assert.False(source!.Ready);
    }

    [Rule("rr:first-player.3")]
    [Fact]
    public void AndOrderResumesThroughTheActiveConditionBranch()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "and": [ { "exhaust": "this" }, { "ready": "this" } ] }, "else": { "and": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } } }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [1, 0]));

        Assert.False(source!.Ready);
    }

    [Rule("rr:and.1")]
    [Fact]
    public void AndResumesAfterAReachableNestedSuspender()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "and": [ { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "exhaust": "this" }, "else": { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } } } }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));

        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:then")]
    [Rule("rr:then.1")]
    [Rule("rr:then.2")]
    [Rule("rr:resolve.1")]
    [Theory]
    [InlineData(2, true)]
    [InlineData(1, false)]
    public void ThenRequiresThePrecedingEffectToResolveInFull(
        int threat, bool draws)
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "removeThreat": { "scheme": { "query": "mainScheme" }, "amount": 2 } }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", threat);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!
            .Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + (draws ? 1 : 0), world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:then.2")]
    [Fact]
    public void ThenIgnoresCharactersThatAreNotValidExhaustTargets()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "exhaust": { "query": "charactersYouControl" } }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        Card? ally = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                ally = board.CreateCard(
                    "01002",
                    board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                ally.Exhaust();
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.False(ally!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:then.2")]
    [Fact]
    public void ThenIgnoresSchemesThatAreNotValidThreatTargets()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "removeThreat": { "scheme": { "query": "sideSchemes" }, "amount": 2 } }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        Card? empty = null;
        Card? threatened = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                empty = board.CreateCard("01107", board.AreaOf(DeckType.SideSchemesArea));
                threatened = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
                threatened.PlaceTokens("k_threat", 2);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(0, empty!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, threatened!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AThreatPlacementBeforeThenRaisesBeforeChangingTheBoard()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "placeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("none/partial/full", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:otherwise")]
    [Rule("rr:otherwise.1")]
    [Rule("rr:otherwise.1.2")]
    [Rule("rr:otherwise.2")]
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void OtherwiseRequiresThePrecedingEffectToResolveNotAtAll(
        int threat, bool draws)
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            // `effect` explicitly delimits the preceding effect. That preserves
            // rr:otherwise.2's semicolon/sentence boundary without recovering
            // punctuation after printed text has become an ability tree.
            """{ "otherwise": { "effect": { "removeThreat": { "scheme": { "query": "mainScheme" }, "amount": 2 } }, "otherwise": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", threat);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!
            .Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + (draws ? 1 : 0), world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:otherwise.1.2")]
    [Fact]
    public void OtherwiseResolvesWhenThreatRemovalIsProhibited()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "otherwise": { "effect": { "removeThreat": { "scheme": { "titled": "Countdown to Oblivion" }, "amount": 2 } }, "otherwise": { "draw": { "player": "you", "count": 1 } } } }""",
            includeAuthored: true);
        Card? source = null;
        Card? scheme = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                scheme = board.CreateCard("01139b", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 2);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(2, scheme!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:otherwise")]
    [Fact]
    public void OtherwiseTreatsAMissingPrecedingTargetAsNoResolution()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "otherwise": { "effect": { "discard": { "titled": "Missing Card" } }, "otherwise": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:otherwise.1.1")]
    [Fact]
    public void OtherwiseResolvesWhenThePrecedingConditionIsFalse()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "otherwise": { "effect": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "exhaust": "this" } } }, "otherwise": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.True(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:otherwise.1.2")]
    [Fact]
    public void AnUnusedOtherwiseBranchDoesNotAskForItsChoice()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "otherwise": { "effect": { "removeThreat": { "scheme": { "query": "mainScheme" }, "amount": 2 } }, "otherwise": { "choose": { "options": [ { "exhaust": "this" }, { "draw": { "player": "you", "count": 1 } } ] } } } }""",
            limit: 1);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 1);
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!
            .Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.True(source!.Ready);
    }

    [Rule("rr:then.1")]
    [Fact]
    public void AChoiceInAnInactiveConditionBranchDoesNotSuspendThen()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "exhaust": "this" }, "else": { "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } } } }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ADependentEffectResumesAfterItsChoice()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "choose": { "options": [ { "exhaust": "this" }, { "draw": { "player": "you", "count": 1 } } ] } }, "then": { "discard": "this" } } }""");
        Card? source = null;

        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(0));

        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
    }

    [Fact]
    public void ADependentEachPlayerEffectResumesTheAbility()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "draw": { "player": "you", "count": 1 } }, "then": { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.False(source!.Ready);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ADependentFirstActivationPersistsItsOuterContinuation()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "WhenRevealed",
            """{ "seq": [ { "then": { "effect": { "draw": { "player": "you", "count": 1 } }, "then": { "enemyAttacks": { "enemies": { "query": "villain" }, "first": "true" } } } }, { "draw": { "player": "you", "count": 1 } } ] }""",
            eventName: Steps.CardRevealed);
        Card? source = null;

        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;

        runner.WhenRevealed(world, source!, 0);
        var attack = Assert.Single(
            world.Agenda.Outstanding, pending => pending.What == Steps.Attack);

        runner.ActivationCompleted(world, new EnemyActivation(
            attack.Subject, attack.Seat, Attacking: true, attack.ActivationId, Made: false));

        Assert.True(source!.Ready);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AChoiceOptionCanSuspendInsideAnd()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "and": [ { "draw": { "player": "you", "count": 1 } }, { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } } ] } ] } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(1));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));

        Assert.False(source!.Ready);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ACostCanSwitchAnIfIntoAResumableBranch()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "and": [ { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } }, { "draw": { "player": "you", "count": 1 } } ] } } }""",
            cost: """{ "discard": "this" }""");
        Card? source = null;

        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));

        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ACostCannotSwitchAnIfIntoAnUnknownDependentOutcome()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "then": { "effect": { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }, "then": { "draw": { "player": "you", "count": 1 } } } } } }""",
            cost: """{ "discard": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("none/partial/full resolution", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void ACostCannotSwitchADependentPredecessorToAnUnknownOutcome()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } } } }, "then": { "draw": { "player": "you", "count": 1 } } } }""",
            cost: """{ "discard": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("none/partial/full resolution", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void AnEarlierStepCannotSwitchADependentPredecessorToAnUnknownOutcome()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "seq": [ { "changeForm": { "player": "you", "to": "alterEgo" } }, { "then": { "effect": { "if": { "test": { "inForm": { "player": "you", "form": "alterEgo" } }, "then": { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }, "else": { "draw": { "player": "you", "count": 1 } } } }, "then": { "draw": { "player": "you", "count": 1 } } } } ] }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("none/partial/full resolution", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Fact]
    public void ACostCanActivateAResumableOtherwiseBranch()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "otherwise": { "effect": { "exhaust": "this" }, "otherwise": { "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(0));

        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AnEarlierStepCanActivateAResumableOtherwiseBranch()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "seq": [ { "exhaust": "this" }, { "otherwise": { "effect": { "exhaust": "this" }, "otherwise": { "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } } } } ] }""");
        Card? source = null;

        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(0));

        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ANestedSequenceResumesPastAnEarlierMutationBoundary()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "seq": [ { "exhaust": "this" }, { "seq": [ { "otherwise": { "effect": { "exhaust": "this" }, "otherwise": { "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } } } } ] } ] }""");
        Card? source = null;

        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(0));

        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ADirectSequencePersistsItsNestedContinuationAfterItsFirstMutation()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "WhenRevealed",
            """{ "seq": [ { "draw": { "player": "you", "count": 1 } }, { "and": [ { "draw": { "player": "you", "count": 1 } }, { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } } ] } ] }""",
            eventName: Steps.CardRevealed);
        Card? source = null;
        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;

        runner.WhenRevealed(world, source!, 0);

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(Steps.ChooseOption, waiting.What);
        Assert.Equal(["seq:1"], waiting.AbilityPath);
    }

    [Rule("rr:and.1")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void AnOrderedCardAttackResumesTheRemainingSimultaneousEffect()
    {
        // The first player chooses the order of the independent effects. The
        // attack has its own agenda procedure, so the later draw must wait for
        // that procedure and then resume from the exact `and` branch.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "and": [ { "attack": { "target": { "query": "villain" }, "effect": { "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } } } }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));

        Assert.Equal(1, villain.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void LeavingTheFinalEachPlayerFrameRestoresTheOriginalResolver()
    {
        // Each frame reads "you" as that frame's player. Text after the frame
        // belongs to the player resolving the ability, not whichever player
        // the first player put last in the chosen order.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "seq": [ { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        int firstHeld = world.Seats[0].Hand.Cards.Count;
        int secondHeld = world.Seats[1].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(
            order.Id,
            [world.Seats[0].IdentityCard.ObjectId, world.Seats[1].IdentityCard.ObjectId]));

        Assert.Equal(firstHeld + 2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(secondHeld + 1, world.Seats[1].Hand.Cards.Count);
    }

    [Rule("rr:and.1")]
    [Fact]
    public void UnsupportedThreatPlacementOrderingRaisesBeforeTheActionCost()
    {
        // Threat placement has its own interrupt and response windows. Until
        // that agenda record carries a structural card continuation, accepting
        // an order that places it first would silently skip the later effect.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "and": [ { "placeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } }, { "draw": { "player": "you", "count": 1 } } ] }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("threat placement continuation", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void NestedEachPlayerFramesRaiseBeforeTheActionCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "choose": { "options": [ { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } }, { "draw": { "player": "you", "count": 1 } } ] } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("nests one each-player", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void ACardPowerThatWouldSuspendRaisesBeforeTheActionCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "chooseCard": { "from": { "query": "attackableEnemies" }, "effect": { "attack": { "target": "chosen", "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LegalPracticePowerThatWouldSuspendRaisesBeforeTheActionCost()
    {
        // A labeled thwart ability "is considered to be a thwart made by that
        // player's identity." Its whole nested power must be preflighted.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "legalPractice": { "schemes": { "query": "thwartableSchemes" }, "power": { "thwart": { "target": "chosen", "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DifferentSchemesPowerThatWouldSuspendRaisesBeforeTheActionCost()
    {
        // A labeled thwart ability "is considered to be a thwart made by that
        // player's identity." Its whole nested power must be preflighted.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "thwartDifferentSchemes": { "schemes": { "query": "thwartableSchemes" }, "power": { "thwart": { "target": "chosen", "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void DelayedPowerThatWouldSuspendRaisesBeforeTheActionCost()
    {
        // The delayed subtree is still the action's effect. Preflight must see
        // its labeled attack before any cost is paid or activation state changes.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "afterActivation": { "effect": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Activation = new EnemyActivation(
                    board.TheCardIn(DeckType.VillainArea)!.ObjectId,
                    Player: 0,
                    Attacking: true,
                    Id: 41);
            },
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Fact]
    public void AStableFormBranchIgnoresAnUnreachableSuspendingPower()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:form-change-form")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void APredecessorMutationPreflightsEveryReachableDependentBranch()
    {
        // Changing form flips which dependent branch resolves. The unsupported
        // labeled attack must be found before the form change or cost occurs.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "changeForm": { "player": "you", "to": "alterEgo" } }, "then": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void EachPlayerPreflightUsesEveryPlayersCurrentForm()
    {
        // "For each player" reaches both identities. A safe branch for the
        // initiating hero cannot hide an unsupported branch for an alter-ego.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:choose-game-element.3")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void ChosenCardContextIsPreflightedBeforeTheActionCost()
    {
        // The choice binds "chosen" before its effect resolves. Every branch
        // that binding can open must be checked before the cost is paid.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "chooseCard": { "from": { "query": "attackableEnemies" }, "effect": { "if": { "test": { "exists": "chosen" }, "then": { "attack": { "target": "chosen", "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } }, "else": { "draw": { "player": "you", "count": 1 } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Fact]
    public void ChosenCardDoesNotDestabilizeAnUnrelatedFormBranch()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "chooseCard": { "from": { "query": "attackableEnemies" }, "effect": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "attack": { "target": "chosen", "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void ChosenPlayerBindingIsPreflightedAfterSelection()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "chooseCard": { "from": { "query": "identities" }, "effect": { "if": { "test": { "inForm": { "player": "chosenPlayer", "form": "hero" } }, "then": { "draw": { "player": "chosenPlayer", "count": 1 } }, "else": { "draw": { "player": "chosenPlayer", "count": 1 } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:then")]
    [Fact]
    public void ChosenTargetCanMakeADependentContinuationReachable()
    {
        // "Then" resolves only after its predecessor resolves in full. Binding
        // the threatened scheme makes that predecessor reachable and mutable.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "chooseCard": { "from": { "query": "thwartableSchemes" }, "effect": { "then": { "effect": { "removeThreat": { "scheme": "chosen", "amount": 1 } }, "then": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? scheme = null;
        long threat = -1;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                scheme = board.TheCardIn(DeckType.MainSchemesArea)!;
                threat = scheme.Tokens.GetValueOrDefault("k_threat");
            },
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(threat, scheme!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void EachPlayerPreflightIncludesMutationsFromEarlierFrames()
    {
        // The first player can remove the source before another player's frame
        // tests whether its title remains in play.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "discard": "this" }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void EachPlayerDrawDoesNotDestabilizeEveryPlayersHeroForm()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[1].IdentityCard.TurnTo("01010a");
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void ACardTitleContainingChosenIsNotAChoiceBinding()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "chooseCard": {
              "from": { "query": "attackableEnemies" },
              "effect": { "if": {
                "test": { "titleInPlay": "Kang's Chosen" },
                "then": { "attack": {
                  "target": "chosen",
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } },
                "else": { "draw": { "player": "you", "count": 1 } }
              } }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void EachPlayerPreflightIncludesDefeatFromEarlierFrames()
    {
        // Damage tokens equal to remaining hit points defeat a minion. A later
        // player's title test therefore sees a different in-play board.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "titleInPlay": "Hydra Mercenary" }, "then": { "dealDamage": { "cards": { "titled": "Hydra Mercenary" }, "amount": 3 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? minion = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(0, minion!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void EachPlayerPreflightUnionsEveryPlayersActiveMutationPath()
    {
        // The first player chooses the order. A hero can discard the source
        // before an alter-ego's different branch tests whether it remains.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "you", "form": "hero" } },
              "then": { "discard": "this" },
              "else": { "if": {
                "test": { "titleInPlay": "Aunt May" },
                "then": { "draw": { "player": "you", "count": 1 } },
                "else": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } }
              } }
            } } } }
            """);
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void RepeatedMutationAnalysisDoesNotReadAnUnansweredChosenPlayer()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "titleInPlay": "Aunt May" },
              "then": { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "if": {
                  "test": { "inForm": { "player": "chosenPlayer", "form": "hero" } },
                  "then": { "draw": { "player": "chosenPlayer", "count": 1 } },
                  "else": { "draw": { "player": "chosenPlayer", "count": 1 } }
                } }
              } },
              "else": { "draw": { "player": "you", "count": 1 } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:form-change-form")]
    [Fact]
    public void RepeatedMutationAnalysisIncludesLabelledPowerEffects()
    {
        // The first player can order another hero first. That hero's attack
        // flips the first player before the first player's own frame resolves.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "you", "form": "hero" } },
              "then": { "attack": {
                "target": { "query": "villain" },
                "effect": { "changeForm": { "player": "firstPlayer", "to": "alterEgo" } }
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[1].IdentityCard.TurnTo("01010a");
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void RepeatedMutationAnalysisReachesAFixedPoint()
    {
        // One frame removes the title, making a form change reachable; that
        // form change makes the unsupported third-frame branch reachable.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "titleInPlay": "Aunt May" },
              "then": { "discard": "this" },
              "else": { "if": {
                "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
                "then": { "changeForm": { "player": "firstPlayer", "to": "alterEgo" } },
                "else": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } }
              } }
            } } } }
            """);
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void RepeatedMutationAnalysisIsBoundedByRemainingFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "titleInPlay": "Aunt May" },
              "then": { "discard": "this" },
              "else": { "if": {
                "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
                "then": { "changeForm": { "player": "firstPlayer", "to": "alterEgo" } },
                "else": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void RemovingThreatDoesNotChangeWhichTitlesAreInPlay()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "removeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void LethalDamageCanMoveTheFirstPlayerBindingBetweenFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "firstPlayer", "form": "hero" } }, "then": { "dealDamage": { "cards": "you", "amount": 99 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(0, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Fact]
    public void VillainDamageCannotMoveTheFirstPlayerBinding()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "firstPlayer", "form": "hero" } }, "then": { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void NonlethalIdentityDamageCannotMoveTheFirstPlayerBinding()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "firstPlayer", "form": "hero" } }, "then": { "dealDamage": { "cards": "you", "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void MovingLethalDamageCanMoveTheFirstPlayerBinding()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "firstPlayer", "form": "hero" } }, "then": { "moveDamage": { "from": { "query": "villain" }, "to": "you", "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        World? world = null;
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                villain.TakeDamage(1);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(1, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void RemovingLastThreatCanDefeatATitleBetweenFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "titleInPlay": "Bomb Scare" }, "then": { "removeThreat": { "scheme": { "titled": "Bomb Scare" }, "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? scheme = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                InPlay(board, AuthoredCards.AuntMay);
                scheme = board.CreateCard(
                    "01109", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 1);
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(1, scheme!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void CumulativeThreatRemovalCanDefeatATitleBetweenFrames()
    {
        // All threat removed during one player's frame counts when deciding
        // whether a later player's title-dependent branch can become active.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "titleInPlay": "Bomb Scare" },
              "then": { "seq": [
                { "removeThreat": { "scheme": { "titled": "Bomb Scare" }, "amount": 1 } },
                { "removeThreat": { "scheme": { "titled": "Bomb Scare" }, "amount": 1 } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? scheme = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                scheme = board.CreateCard(
                    "01109", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 2);
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(2, scheme!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void CumulativeDamageCanEliminateAPlayerBetweenFrames()
    {
        // Individually nonlethal damage instances combine before the next
        // player's frame and can move the first-player binding.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": "you", "amount": 1 } },
                { "dealDamage": { "cards": "you", "amount": 1 } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            cost: """{ "exhaust": "this" }""");
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void OrderedMutationCanExposeLethalDamageWithinARepeatedFrame()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "discard": "this" },
                { "if": {
                  "test": { "not": { "titleInPlay": "Aunt May" } },
                  "then": { "dealDamage": { "cards": "you", "amount": 2 } },
                  "else": { "draw": { "player": "you", "count": 1 } }
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void DamageToFixedTargetsAccumulatesAcrossRepeatedFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "dealDamage": {
                "cards": { "query": "identities" }, "amount": 1
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Theory]
    [InlineData("\"yourHero\"")]
    [InlineData("{ \"query\": \"charactersYouControl\" }")]
    [InlineData("{ \"withTrait\": { \"cards\": \"yourHero\", \"trait\": \"AVENGER\" } }")]
    [InlineData("{ \"withTrait\": { \"cards\": { \"query\": \"charactersYouControl\" }, \"trait\": \"AVENGER\" } }")]
    [InlineData("{ \"maxBy\": { \"of\": { \"query\": \"charactersYouControl\" }, \"by\": \"attack\" } }")]
    public void PlayerRelativeDamageTargetsApplyOnlyInTheirPlayersFrame(
        string targets)
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            $$"""
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "dealDamage": { "cards": {{targets}}, "amount": 1 } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void MovedDamageCannotBeReusedAcrossRepeatedFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "moveDamage": {
                "from": { "query": "villain" },
                "to": { "titled": "Spider-Man" },
                "amount": 1
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void DamageBeforeAMoveReplenishesItsRepeatedSource()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "titled": "Hydra Mercenary" }, "amount": 2 } },
                { "discard": "this" },
                { "if": {
                  "test": { "not": { "titleInPlay": "Aunt May" } },
                  "then": { "moveDamage": {
                    "from": { "titled": "Hydra Mercenary" },
                    "to": { "titled": "Spider-Man" },
                    "amount": 2
                  } },
                  "else": { "moveDamage": {
                    "from": { "query": "villain" },
                    "to": { "titled": "Spider-Man" },
                    "amount": 1
                  } }
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;
        Card? minion = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
        Assert.Equal(0, minion!.Damage);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void DamageAfterAMoveIsAvailableOnlyToLaterRepeatedFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } },
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void DamageMovedAwayDoesNotAccumulateAcrossRepeatedFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "titled": "Spider-Man" }, "amount": 1 } },
                { "moveDamage": {
                  "from": { "titled": "Spider-Man" },
                  "to": { "query": "villain" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void DamageHealedAfterEachMoveDoesNotAccumulateAcrossFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } },
                { "heal": { "card": { "titled": "Spider-Man" }, "amount": 1 } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(3);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void DistinctPlayersCanSupplyDamageAcrossRepeatedFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "moveDamage": {
                "from": "you",
                "to": { "titled": "Spider-Man" },
                "amount": 1
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
                board.Seats[1].IdentityCard.TakeDamage(1);
                board.Seats[2].IdentityCard.TakeDamage(1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk", "iron_man"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(1, world.Seats[2].IdentityCard.Damage);
    }

    [Fact]
    public void PlayerRelativeDamageRebindsBetweenFixedTargetFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "titled": "Spider-Man" }, "amount": 1 } },
                { "dealDamage": { "cards": "you", "amount": 1 } },
                { "heal": { "card": { "titled": "Spider-Man" }, "amount": 1 } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(7);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:tough.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void ToughPreventsLethalDamageInAnEarlierRepeatedFrame()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "dealDamage": {
                "cards": { "titled": "Spider-Man" }, "amount": 1
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
                Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Tough);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:tough.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void ToughGrantedBeforeEachRepeatedDamagePreventsIt()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "giveStatus": {
                  "card": { "titled": "Spider-Man" }, "status": "tough"
                } },
                { "dealDamage": {
                  "cards": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void HealthGrantedBeforeRepeatedDamageRaisesItsLethalThreshold()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "grantUntil": {
                  "card": { "titled": "Spider-Man" },
                  "keyword": "health", "amount": 1, "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void ProhibitedMoveLeavesDamageForALaterRepeatedMove()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Madame Hydra" },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            includeAuthored: true);
        World? world = null;
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01180", board.AreaOf(DeckType.SideSchemesArea));
                board.CreateCard(
                    "01181",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                villain = board.TheCardIn(DeckType.VillainArea)!;
                villain.TakeDamage(1);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(1, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void ProhibitedDamageCannotReplenishARepeatedMoveSource()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": {
                  "cards": { "titled": "Madame Hydra" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Madame Hydra" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            includeAuthored: true);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01180", board.AreaOf(DeckType.SideSchemesArea));
                board.CreateCard(
                    "01181",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void EarlierDiscardCanRemoveARepeatedMoveProhibition()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "if": {
                  "test": { "titleInPlay": "Legions of Hydra" },
                  "then": { "discard": { "titled": "Legions of Hydra" } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Madame Hydra" },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            includeAuthored: true);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01180", board.AreaOf(DeckType.SideSchemesArea));
                board.CreateCard(
                    "01181",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void SideSchemeDefeatCanRemoveARepeatedDamageProhibition()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "removeThreat": {
                  "scheme": { "titled": "Legions of Hydra" }, "amount": 1
                } },
                { "dealDamage": {
                  "cards": { "titled": "Madame Hydra" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Madame Hydra" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            includeAuthored: true);
        World? world = null;
        Card? source = null;
        Card? legions = null;
        Card? madame = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                legions = board.CreateCard(
                    "01180", board.AreaOf(DeckType.SideSchemesArea));
                legions.PlaceTokens("k_threat", 1);
                madame = board.CreateCard(
                    "01181",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(1, legions!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, madame!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void MinionDefeatCanRemoveARepeatedDamageProhibition()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "query": "drones" }, "amount": 100 } },
                { "dealDamage": { "cards": { "titled": "Ultron" }, "amount": 1 } },
                { "moveDamage": {
                  "from": { "titled": "Ultron" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            includeAuthored: true);
        World? world = null;
        Card? source = null;
        Card? drone = null;
        Card? ultron = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                ultron = board.CreateCard(
                    "01136", board.AreaOf(DeckType.VillainArea));
                drone = FacedownDrones.EngageTop(
                    board, 0, "test", "Create_Drone", []);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.NotNull(drone);
        Assert.Equal(0, drone!.Damage);
        Assert.Equal(0, ultron!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:villain-defeat.2")]
    [Fact]
    public void ARepeatedFrameUsesTheNewVillainStageAfterDefeat()
    {
        // "Excess damage that is dealt to defeat a villain stage does not
        // carry over to the new stage." The later move therefore reads zero
        // damage from the newly revealed stage, not the defeated card's dial.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 100 } },
                { "moveDamage": {
                  "from": { "titled": "Rhino" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:villain-defeat.2")]
    [Fact]
    public void AFilteredVillainSelectorMutatesTheNewStageBeforeTheNextFrame()
    {
        // The new stage is the current in-play card titled Rhino. It begins
        // without the defeated stage's excess damage, then receives this
        // effect's next point. The following move must see that point or the
        // trace would offer an ability whose first frame defeats the player.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 100 } },
                { "dealDamage": {
                  "cards": { "withTrait": {
                    "cards": { "query": "villain" }, "trait": "BRUTE"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:villain-defeat.4")]
    [Fact]
    public void ATitleSelectorDoesNotFollowADifferentVillainCharacter()
    {
        // A different-title stage is not Rhino. The live title selector finds
        // nothing after Ultron replaces Rhino, so it cannot put damage on
        // Ultron for the following move to carry to the wounded hero.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 100 } },
                { "dealDamage": { "cards": { "titled": "Rhino" }, "amount": 1 } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:villain-defeat.2")]
    [Fact]
    public void ARankedSelectorIsRecomputedForTheNewVillainStage()
    {
        // Rhino I and Shocker tie for the lowest attack, but Rhino II does
        // not. The selector is evaluated after the stage changes, so only
        // Shocker receives the point and the new villain has none to move.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 100 } },
                { "dealDamage": {
                  "cards": { "minBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01103",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:villain-defeat.2")]
    [Fact]
    public void ARankedSelectorCanGainTheNewVillainStage()
    {
        // Rhino I is below Sandman's attack, while Rhino II ties it. The live
        // maximum therefore gains the new stage after the defeat; its damage
        // must be visible to the following move before the action is offered.
        var runner = RepeatedDynamicTargetRunner(
            """{ "query": "villain" }""",
            """{ "maxBy": { "of": { "query": "enemies" }, "by": "attack" } }""");
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01102",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:guard.1")]
    [Fact]
    public void DefeatingGuardCanMakeTheVillainADynamicDamageTarget()
    {
        // "The engaged player cannot attack any villain" only while Guard is
        // present. Defeating Hydra Mercenary makes Rhino attackable before the
        // next effect resolves, so the subsequent move has damage to carry.
        var runner = RepeatedDynamicTargetRunner(
            """{ "titled": "Hydra Mercenary" }""",
            """{ "query": "attackableEnemies" }""");
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:villain-defeat.3.2")]
    [Fact]
    public void CarriedAttachmentTraitsApplyToTheNewVillainStage()
    {
        // Attachments and their constant abilities carry to a same-title
        // stage. Flight therefore still gives Rhino II AERIAL before the
        // filtered damage resolves, leaving a point for the following move.
        var runner = RepeatedDynamicTargetRunner(
            """{ "query": "villain" }""",
            """{ "withTrait": { "cards": { "query": "villain" }, "trait": "AERIAL" } }""",
            includeAuthored: true);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                var villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard(
                    "40151",
                    board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability")]
    [Fact]
    public void DiscardedAttachmentStopsGrantingItsTraitDuringTheTrace()
    {
        // A constant ability remains active only while its card is in play.
        // Discarding Cosmic Flight removes AERIAL before the filtered damage,
        // so the wounded identity is no longer one of that effect's targets.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "discard": { "titled": "Cosmic Flight" } },
                { "dealDamage": {
                  "cards": { "withTrait": {
                    "cards": { "query": "characters" }, "trait": "AERIAL"
                  } },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            includeAuthored: true);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01017",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:lasting-effects.1")]
    [Fact]
    public void DiscardedAttachmentKeepsItsLastingTraitDuringTheTrace()
    {
        // A lasting effect continues for its specified duration whether or not
        // its source remains in play. Discarding Rocket Boots therefore does
        // not remove the AERIAL it already granted until the phase ends.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "discard": { "titled": "Rocket Boots" } },
                { "dealDamage": {
                  "cards": { "withTrait": {
                    "cards": { "query": "characters" }, "trait": "AERIAL"
                  } },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                var boots = board.CreateCard(
                    "01039",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
                board.Effects.Register(new ContinuousEffect(
                    EffectSource.LastingEffect,
                    Traits.Granted + "AERIAL",
                    Card: boots.ObjectId,
                    Affects: board.Seats[0].IdentityCard.ObjectId,
                    Lasts: new Duration(Until: TimingPoints.EndOfPlayerPhase)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:attachment.1")]
    [Fact]
    public void DiscardedAttachmentStopsModifyingARankedField()
    {
        // An attachment may modify its attached character's ATK "as indicated
        // by the values in the associated fields on the attachment card."
        // Discarding Enhanced Ivory Horn therefore drops Rhino to Shocker's
        // ATK, so both are minimum targets and Rhino's point is available for
        // the lethal move.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "discard": { "titled": "Enhanced Ivory Horn" } },
                { "dealDamage": {
                  "cards": { "minBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                var villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard(
                    "01100",
                    board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.CreateCard(
                    "01103",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:traits.1")]
    [Fact]
    public void EarlierTraitGrantChangesALaterDynamicTargetSet()
    {
        // A granted trait is immediately part of what later card abilities
        // query. Granting Rhino AERIAL makes both it and the already-AERIAL
        // Vulture targets before the villain's point is moved to the hero.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "grantUntil": {
                  "card": { "query": "villain" },
                  "trait": "AERIAL", "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "withTrait": {
                    "cards": { "query": "enemies" }, "trait": "AERIAL"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "27163",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:traits.1")]
    [Fact]
    public void EarlierTraitGrantChangesAMinionOnlyTargetSet()
    {
        // Dynamic membership is not limited to sets that can contain the
        // villain. Giving Shocker AERIAL adds it to the later minion-only set,
        // so its damage is present for the move to the wounded hero.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "grantUntil": {
                  "card": { "titled": "Shocker" },
                  "trait": "AERIAL", "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "withTrait": {
                    "cards": { "query": "minions" }, "trait": "AERIAL"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Shocker" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01103",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "27163",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void EarlierNumericGrantChangesALaterRankedTargetSet()
    {
        // Rhino begins below Sandman's attack. The +2 ATK grant makes Rhino
        // the unique maximum before the ranked damage resolves, leaving a
        // point for the following move to carry to the wounded hero.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "grantUntil": {
                  "card": { "query": "villain" },
                  "keyword": "attack", "amount": 2, "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01102",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void RankedSelectorCanDropAnInitiallySelectedNonVillain()
    {
        // Sandman begins as the maximum attack enemy, but Ultron's next stage
        // exceeds him. The live maximum drops Sandman, so no damage is present
        // on him for the following move and the ability remains legal.
        var runner = RepeatedDynamicTargetRunner(
            """{ "query": "villain" }""",
            """{ "maxBy": { "of": { "query": "enemies" }, "by": "attack" } }""",
            """{ "titled": "Sandman" }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
                board.CreateCard(
                    "01102",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void RankedPlayerCharacterSelectorRetainsItsControllerScope()
    {
        // The dynamic rank is over characters the resolving player controls,
        // not over enemies. Hulk is in that set and has the highest attack, so
        // its damage is available for the following move to Spider-Man.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "charactersYouControl" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Hulk" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01050",
                    board.AreaOf(
                        DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:dash-value.3")]
    [Fact]
    public void ATraceLocalModifierCannotChangeADashPowerForRanking()
    {
        // A referenced dash is "treated as having a value of 0" and "cannot
        // be modified." Giving alter-ego Carol +5 ATK therefore leaves Hulk
        // as the maximum-ATK character and makes his damage available for the
        // lethal move that follows.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "grantUntil": {
                  "card": { "titled": "Carol Danvers" },
                  "keyword": "attack", "amount": 5, "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "characters" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Hulk" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01050",
                    board.AreaOf(
                        DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:enters-play")]
    [Rule("rr:toughness.1")]
    [Fact]
    public void AMinionPutIntoPlayJoinsLaterRankedTargetSets()
    {
        // A card enters play when it moves from an out-of-play area into play,
        // and Toughness gives it a tough status at that point. Sandman joins
        // the enemy set before the ranked damage: the first point consumes
        // tough, the second frame damages Sandman, and Rhino has none to move.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "if": {
                  "test": { "not": { "titleInPlay": "Sandman" } },
                  "then": { "putIntoPlay": {
                    "card": { "cardsIn": {
                      "areas": [ "encounterDiscardPile" ], "title": "Sandman"
                    } },
                    "where": "engagedWithYou"
                  } }
                } },
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01102",
                    board.AreaOf(DeckType.EncounterDiscardPile));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:damage.step.1")]
    [Rule("rr:replacement-effect.1")]
    [Fact]
    public void ForcedReplacementCanLeaveARepeatedMoveSourceEmpty()
    {
        // Damage replacement abilities resolve before damage is placed. Once
        // Armored Rhino Suit puts that damage on itself "instead", "the effect
        // is no longer considered imminent" and Rhino has nothing to move.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            includeAuthored: true);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard(
                    AuthoredCards.ArmoredSuit,
                    board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void BoundedThreatRemovalCannotDefeatADistantSideScheme()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "titleInPlay": "Bomb Scare" }, "then": { "removeThreat": { "scheme": { "titled": "Bomb Scare" }, "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var scheme = board.CreateCard(
                    "01109", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 3);
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void InactiveRemovalBranchDoesNotInflateARepeatedMutationBudget()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "titleInPlay": "Bomb Scare" },
              "then": { "removeThreat": {
                "scheme": { "titled": "Bomb Scare" }, "amount": 1
              } },
              "else": { "seq": [
                { "removeThreat": {
                  "scheme": { "titled": "Bomb Scare" }, "amount": 100
                } },
                { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } }
              ] }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var scheme = board.CreateCard(
                    "01109", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 3);
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void RepeatedMutationAnalysisFlowsThroughOnePlayersSequence()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "seq": [
              { "if": {
                "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
                "then": { "draw": { "player": "you", "count": 1 } },
                "else": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } }
              } },
              { "discard": "this" },
              { "if": {
                "test": { "not": { "titleInPlay": "Aunt May" } },
                "then": { "changeForm": { "player": "firstPlayer", "to": "alterEgo" } }
              } }
            ] } } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, source.Area.Type);
    }

    [Rule("rr:choose-option")]
    [Fact]
    public void AChoiceContinuationPreservesEarlierEffectResults()
    {
        // "This way" is scoped to the one ability resolution. Asking a
        // question cannot replace that resolution with a fresh result map.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "seq": [ { "heal": { "card": "you", "amount": 1 } }, { "choose": { "options": [ { "exhaust": "this" }, { "ready": "this" } ] } }, { "if": { "test": { "atLeast": { "value": { "result": "healed" }, "count": 1 } }, "then": { "draw": { "player": "you", "count": 1 } } } } ] }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(1);
            },
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(0));

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void PayOrExhaustContinuesTheContainingSequence()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "seq": [ { "payOrExhaust": { "resources": "YBR", "otherwise": { "exhaust": "this" } } }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(1));

        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:activation.7")]
    [Fact]
    public void SequentialActivationWaitsUseTheOriginatingFaceAndFreshResults()
    {
        // The first attack changes neither the identity's current face nor the
        // authored face that owns this ability. Its damage result must not leak
        // into the second wait's later condition.
        var runner = Runner(
            AuthoredCards.SpiderMan,
            "WhenRevealed",
            """{ "seq": [ { "changeForm": { "player": "you", "to": "alter-ego" } }, { "seq": [ { "enemyAttacks": { "enemies": { "query": "villain" } } }, { "enemyAttacks": { "enemies": { "query": "villain" } } } ] }, { "if": { "test": { "atLeast": { "value": { "result": "activationDamage" }, "count": 1 } }, "then": { "draw": { "player": "you", "count": 1 } } } } ] }""",
            eventName: Steps.CardRevealed);
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var identity = world.Seats[0].IdentityCard;
        int held = world.Seats[0].Hand.Cards.Count;

        runner.WhenRevealed(world, identity, 0);
        Assert.Equal("01001b", identity.FaceId);
        var first = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.Attack);

        runner.ActivationCompleted(world, new EnemyActivation(
            first.Subject, first.Seat, Attacking: true, first.ActivationId,
            Made: true, DamageDealt: 2));
        var second = Assert.Single(
            world.Agenda.Outstanding,
            step => step.What == Steps.Attack && step.ActivationId != first.ActivationId);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);

        runner.ActivationCompleted(world, new EnemyActivation(
            second.Subject, second.Seat, Attacking: true, second.ActivationId,
            Made: true, DamageDealt: 0));

        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void EachPlayerReconstructionUsesTheOriginatingIdentityFace()
    {
        var runner = Runner(
            AuthoredCards.SpiderMan,
            "WhenRevealed",
            """{ "seq": [ { "heal": { "card": "you", "amount": 1 } }, { "changeForm": { "player": "you", "to": "alter-ego" } }, { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } }, { "if": { "test": { "atLeast": { "value": { "result": "healed" }, "count": 1 } }, "then": { "draw": { "player": "you", "count": 1 } } } } ] }""",
            eventName: Steps.CardRevealed);
        var (_, world) = Playing(
            board => board.Seats[0].IdentityCard.TakeDamage(1),
            hero: true,
            abilities: runner);
        var identity = world.Seats[0].IdentityCard;
        int held = world.Seats[0].Hand.Cards.Count;

        runner.WhenRevealed(world, identity, 0);
        var frame = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.ResolveEachPlayer);
        for (int advances = 0;
             world.Agenda.Current?.What != Steps.ResolveEachPlayer && advances < 20;
             advances++)
        {
            world.Agenda.Advance();
        }
        Assert.Equal(Steps.ResolveEachPlayer, world.Agenda.Current?.What);

        runner.ResolveEachPlayer(
            world, identity, frame.Seat, frame.Index, frame.Tier,
            frame.FinalStep, frame.FinalPlayer);

        Assert.Equal("01001b", identity.FaceId);
        Assert.Equal(0, identity.Damage);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:and.1")]
    [Fact]
    public void AFinalOrderedPowerKeepsNestedAncestorWorkPending()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "seq": [ { "and": [ { "draw": { "player": "you", "count": 1 } }, { "attack": { "target": { "query": "villain" }, "effect": { "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } } } } ] }, { "draw": { "player": "you", "count": 1 } } ] } } }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));

        Assert.Equal(1, villain.Damage);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void InvalidOrderedContinuationIsRejectedBeforeRunningAnySibling()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "and": [ { "exhaust": "this" }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var forged = new PhaseStep(
            Steps.ResumeAbility, 1, 2, Subject: source!.ObjectId, Seat: 0,
            Tier: AbilityType.Action, AbilityOrdinal: 0,
            AbilityPath: ["and:0:0,1:"], AbilityFace: source.FaceId,
            AbilityHasContinuation: true);

        Assert.Throws<RulesNotImplementedException>(
            () => runner.ResumeAbility(world, forged));

        Assert.True(source.Ready);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ADirectLastingEffectCannotBeginOutsideItsPeriod()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "WhenRevealed",
            """{ "grantUntil": { "card": "this", "trait": "AERIAL", "until": "EndOfAttack" } }""",
            eventName: Steps.CardRevealed);
        Card? source = null;
        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => runner.WhenRevealed(world, source!, 0));

        Assert.Contains("outside its named period", thrown.Message, StringComparison.Ordinal);
        Assert.Empty(world.Effects.Active());
    }

    [Fact]
    public void PaymentCannotSwitchIntoALastingEffectOutsideItsPeriod()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "grantUntil": { "card": "you", "trait": "AERIAL", "until": "EndOfAttack" } } } }""",
            cost: """{ "discard": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("outside its named period", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void AStableFormBranchIgnoresAnUnreachableLastingConstraint()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "grantUntil": { "card": "this", "trait": "AERIAL", "until": "EndOfAttack" } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void PaymentCannotSwitchIntoALastingEffectWithNoTarget()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "grantUntil": { "card": "attachedTo", "trait": "AERIAL", "until": "EndOfRound" } } } }""",
            cost: """{ "discard": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("no target after payment", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void AFirstActivationResumesTheRestOfASequence()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "WhenRevealed",
            """{ "seq": [ { "enemyAttacks": { "enemies": { "query": "villain" }, "first": "true" } }, { "draw": { "player": "you", "count": 1 } } ] }""",
            eventName: Steps.CardRevealed);
        Card? source = null;

        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;

        runner.WhenRevealed(world, source!, 0);
        var attack = Assert.Single(
            world.Agenda.Outstanding, pending => pending.What == Steps.Attack);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);

        runner.ActivationCompleted(world, new EnemyActivation(
            attack.Subject, attack.Seat, Attacking: true, attack.ActivationId, Made: false));

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AChoiceCannotOfferALastingEffectOutsideItsPeriod()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "choose": { "options": [ { "grantUntil": { "card": "this", "keyword": "attack", "amount": 1, "until": "EndOfAttack" } }, { "draw": { "player": "you", "count": 1 } } ] } }""");
        Card? source = null;

        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        var option = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(1, option.Id);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AResourceCostInsideASequenceIsAdvertised()
    {
        // Tenacity pays one physical resource and discards itself. Discarding
        // the upgrade is automatic, but the resource is a player choice and
        // therefore has to survive the surrounding cost sequence onto the
        // affordance.
        Card? tenacity = null;
        var (game, _) = Playing(
            board =>
            {
                tenacity = InPlay(board, "01093");
                Physical(board, 1);
            },
            hero: true);

        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb
                && option.AnchorId == tenacity!.ObjectId);
        var price = Assert.Single(action.CostOptions);

        Assert.Equal("1", price.Cost);
        Assert.Equal(["R"], price.Rule);
    }

    /// <summary>`01003` Backflip — a Spider-Man card printing a physical.</summary>
    private const string Physicals = "01003";

    /// <summary>`01004` Enhanced Spider-Sense — the same count, a mental.</summary>
    private const string Mentals = "01004";

    /// <summary>Empties the hand and fills it with physical resources.</summary>
    private static void Physical(World world, int count) =>
        Hand(world, Physicals, count);

    private static void Hand(World world, string faceId, int count) =>
        Hand(world, player: 0, faceId, count);

    private static void Hand(World world, int player, string faceId, int count)
    {
        foreach (var card in world.Seats[player].Hand.Cards.ToList())
        {
            World.MoveToTop(card, world.Seats[player].Deck);
        }

        for (int made = 0; made < count; made++)
        {
            world.CreateCard(faceId, world.Seats[player].Hand);
        }
    }

    [Rule("rr:resource.4")]
    [Fact]
    public void ThreeOfTheWrongResourceIsNotThreePhysicals()
    {
        // Enough cards and the wrong kind. `rr:resource.4`: "many abilities
        // require specific resource types, and the specified types in the
        // specified quantities must be generated **in order to pay the cost**"
        // -- so a cost of three physicals is not a cost of three.
        //
        // The board is the same as the paying one except for the letter on the
        // cards, which is what makes this a test of the types rather than of
        // the count.
        Card? horn = null;
        var (game, world) = Playing(
            board =>
            {
                horn = board.CreateCard(
                    AuthoredCards.IvoryHorn, board.AreaOf(DeckType.RevealingArea));

                // Through the reveal: `rr:attach-to` makes "Attach to Rhino" a
                // rule about the card entering play rather than a "When
                // Revealed" ability, so the route in is what attaches it.
                board.Abilities = AuthoredCards.Runner();
                Reveal.Resolve(board, Cards, horn, 0, []);
                Hand(board, Mentals, 3);
            },
            hero: true);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);

        // And paying with them anyway is refused by name rather than half-paid:
        // `rr:initiating-abilities.step.5` aborts "without paying any costs".
        var ability = Assert.Single(AuthoredCards.Runner().Actions(world, 0)
            .Where(pending => pending.Card == horn!.ObjectId)
            .DefaultIfEmpty(new PendingAbility(horn!.ObjectId, AbilityType.Action, 0)));

        int before = world.Seats[0].Hand.Cards.Count;
        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => AuthoredCards.Runner().Act(
                world,
                ability,
                [.. world.Seats[0].Hand.Cards.Select(card => card.ObjectId)],
                []));

        Assert.Contains("requiring 'RRR'", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(before, world.Seats[0].Hand.Cards.Count);
    }

    /// <summary>Puts a support into play under the first player.</summary>
    private static Card InPlay(World world, string faceId) => world.CreateCard(
        faceId, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));

    private static Marvel.Cards.Run.AbilityRunner Runner(
        string card,
        string timing,
        string effect,
        string? cost = null,
        string eventName = "WhenActionTriggered",
        string? player = null,
        long? limit = null,
        bool anyPlayer = false,
        bool includeAuthored = false)
    {
        var local = Marvel.Cards.Dsl.AbilityCatalog.Parse(
            $$"""
            { "cards": [ { "card": "{{card}}", "abilities": [ {
                "trigger": { "event": "{{eventName}}", "timing": "{{timing}}", "subject": "game"{{(player is null ? string.Empty : $", \"player\": \"{player}\"")}} },
                {{(cost is null ? string.Empty : $"\"cost\": {cost},")}}
                {{(limit is null ? string.Empty : $"\"limitPerRound\": {limit.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)},")}}
                {{(anyPlayer ? "\"anyPlayer\": true," : string.Empty)}}
                "effect": {{effect}}
            } ] } ] }
            """);
        if (!includeAuthored)
        {
            return new Marvel.Cards.Run.AbilityRunner(local);
        }

        var book = new Marvel.Cards.Dsl.AbilityBook(
            [.. AuthoredCards.Book.Abilities, .. local.Abilities],
            AuthoredCards.Book.Authored.Concat(local.Authored)
                .ToHashSet(StringComparer.Ordinal),
            AuthoredCards.Book.AttachTo);
        return new Marvel.Cards.Run.AbilityRunner(book);
    }

    private static Marvel.Cards.Run.AbilityRunner RepeatedDynamicTargetRunner(
        string firstTarget, string secondTarget,
        string moveSource = """{ "query": "villain" }""",
        bool includeAuthored = false) => Runner(
        AuthoredCards.AuntMay,
        "Action",
        $$"""
        { "eachPlayer": { "effect": { "if": {
          "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
          "then": { "seq": [
            { "dealDamage": { "cards": {{firstTarget}}, "amount": 100 } },
            { "dealDamage": { "cards": {{secondTarget}}, "amount": 1 } },
            { "moveDamage": {
              "from": {{moveSource}},
              "to": { "titled": "Spider-Man" },
              "amount": 1
            } }
          ] },
          "else": { "attack": {
            "target": { "query": "villain" },
            "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
          } }
        } } } }
        """,
        includeAuthored: includeAuthored);

    /// <summary>
    /// A game past the mulligan, on the first player's turn.
    /// </summary>
    /// <remarks>
    /// The board is prepared <b>before</b> the game begins, because a turn
    /// prompt is built once and lists what was there when it was built. A card
    /// put into play afterwards is on the board and not in the question.
    /// </remarks>
    private static (Game Game, World World) Playing(
        Action<World> prepare,
        bool hero = false,
        string[]? heroes = null,
        ICardAbilities? abilities = null)
    {
        string[] playing = heroes ?? ["spider_man"];
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", playing), Cards),
            [.. playing.Select(name => Setup.Hero(name).Name)],
            12345);

        if (hero)
        {
            world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        }

        prepare(world);

        // The mulligan is asked as a turn option too, so the loop watches the
        // verb rather than the question: declining it keeps the opening hand.
        var game = Game.Begin(world, Cards, abilities ?? AuthoredCards.Runner());
        while (game.Pending is { } asked
            && asked.Affordances.Any(
                option => option.Verb == Game.ResolveMulligans))
        {
            game.Resolve(Decision.Decline);
        }

        return (game, world);
    }
}
