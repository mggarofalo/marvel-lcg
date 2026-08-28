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
    [Fact]
    public void AnActionIsOfferedOnTheTurnAndDoesWhatItSays()
    {
        // "Alter-Ego Action: Exhaust Aunt May → heal 4 damage from Peter
        // Parker." The whole path: offered among the turn options, taken, cost
        // paid, effect resolved, and the turn goes on.
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
        // sequence, and the one live Action occurrence owns both the damage
        // and defeat conditions.
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

    [Rule("rr:player-elimination.5")]
    [Rule("rr:player-elimination.step.5")]
    [Fact]
    public void APlayerEliminatedByAnActionCostFinishesItAndTheirTurn()
    {
        // Focused Rage's Hero Action deals one damage to its player as a cost.
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
        Assert.Equal(DeckType.HandsArea, kick!.Area.Type);

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
                .Select(card => card.Card));
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

    /// <summary>
    /// A game past the mulligan, on the first player's turn.
    /// </summary>
    /// <remarks>
    /// The board is prepared <b>before</b> the game begins, because a turn
    /// prompt is built once and lists what was there when it was built. A card
    /// put into play afterwards is on the board and not in the question.
    /// </remarks>
    private static (Game Game, World World) Playing(
        Action<World> prepare, bool hero = false, string[]? heroes = null)
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
        var game = Game.Begin(world, Cards, AuthoredCards.Runner());
        while (game.Pending is { } asked
            && asked.Affordances.Any(
                option => option.Verb == Game.ResolveMulligans))
        {
            game.Resolve(Decision.Decline);
        }

        return (game, world);
    }
}
