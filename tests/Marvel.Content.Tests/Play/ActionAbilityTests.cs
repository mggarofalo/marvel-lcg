using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
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
/// interrupt or a response — which is why <c>AbilityTypes.PriorityOf</c> has
/// always refused to give it a tier.
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
    public void AnotherPlayersCardIsNotYoursToTrigger()
    {
        // "A card in play **they control**". `rr:player-turn.6` is how you
        // reach somebody else's -- by asking them -- and that is not written.
        var (game, _) = Playing(
            board =>
            {
                board.CreateCard(
                    AuthoredCards.AuntMay,
                    board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
                board.Seats[0].IdentityCard.TakeDamage(5);
            },
            heroes: ["spider_man", "she_hulk"]);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
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
        Assert.Equal(3, price.Generators.Count(source => source.Generates == "R"));

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

    /// <summary>`01003` Backflip — a Spider-Man card printing a physical.</summary>
    private const string Physicals = "01003";

    /// <summary>`01004` Enhanced Spider-Sense — the same count, a mental.</summary>
    private const string Mentals = "01004";

    /// <summary>Empties the hand and fills it with physical resources.</summary>
    private static void Physical(World world, int count) =>
        Hand(world, Physicals, count);

    private static void Hand(World world, string faceId, int count)
    {
        foreach (var card in world.Seats[0].Hand.Cards.ToList())
        {
            World.MoveToTop(card, world.Seats[0].Deck);
        }

        for (int made = 0; made < count; made++)
        {
            world.CreateCard(faceId, world.Seats[0].Hand);
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

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => AuthoredCards.Runner().Act(
                world, ability, [.. world.Seats[0].Hand.Cards.Select(card => card.ObjectId)]));

        Assert.Contains("requiring 'RRR'", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(3, world.Seats[0].Hand.Cards.Count);
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
