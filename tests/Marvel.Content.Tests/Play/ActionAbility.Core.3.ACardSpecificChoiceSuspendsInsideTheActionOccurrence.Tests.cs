using Marvel.Cards.Dsl;
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
public sealed class ActionAbilityCoreACardSpecificChoiceSuspendsInsideTheActionOccurrenceTests
{
    [Rule("rr:choose-game-element.3.1")]
    [Rule("rr:scheme-card-type.1")]
    [Rule("rr:player-turn.5")]
    [Fact]
    public void ACardSpecificChoiceSuspendsInsideTheActionOccurrence()
    {
        Card? practice = null;
        Card? discard = null;
        Card? side = null;
        var(game, world) = Playing(board =>
        {
            var scheme = board.TheCardIn(DeckType.MainSchemesArea)!;
            scheme.PlaceTokens("k_threat", 2);
            side = board.CreateCard("01107", board.AreaOf(DeckType.SideSchemesArea));
            side.PlaceTokens("k_threat", 1);
            practice = board.CreateCard("01023", board.Seats[0].Hand);
            discard = board.CreateCard("01087", board.Seats[0].Hand);
        });
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == practice!.ObjectId);
        var suspended = game.Resolve(Decision.Take(action.Id));
        Assert.Equal(Question.Element, suspended.Prompt!.Asking);
        Assert.Contains(suspended.Prompt.Affordances, option => option.Id == world.TheCardIn(DeckType.MainSchemesArea)!.ObjectId);
        Assert.Contains(suspended.Prompt.Affordances, option => option.Id == side!.ObjectId);
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
        var(game, _) = Playing(board =>
        {
            InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(5);
        }, hero: true);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
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
        var(game, _) = Playing(board =>
        {
            InPlay(board, AuthoredCards.AuntMay).Exhaust();
            board.Seats[0].IdentityCard.TakeDamage(5);
        });
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
    }

    [Rule("rr:player-turn.5")]
    [Rule("rr:action.1")]
    [Rule("rr:player-turn.3")]
    [Rule("rr:player-turn.4")]
    [Rule("rr:player-turn.6")]
    [Fact]
    public void AnotherPlayersActionIsOfferedDirectlyAndResolvedByThem()
    {
        // "Ask another player to trigger any Action ability that player could
        // trigger on their own turn." The engine treats taking this direct
        // affordance as the other player accepting or offering. It is still
        // their action: their alter-ego form makes it legal, their Aunt May
        // exhausts, and their identity is healed.
        Card? may = null;
        var(game, world) = Playing(board =>
        {
            may = board.CreateCard(AuthoredCards.AuntMay, board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            board.Seats[1].IdentityCard.TakeDamage(5);
        }, heroes: ["spider_man", "she_hulk"]);
        Prompt activeMenu = Assert.IsType<Prompt>(game.PromptFor(0));
        Prompt otherMenu = Assert.IsType<Prompt>(game.PromptFor(1));
        // Options 3 and 4 say that the player taking their turn uses “their”
        // identity or an ally “they control.” Only option 6 crosses seats, and
        // it names an Action specifically. The second seat cannot inherit card
        // play, form change, end turn, or either kind of basic power.
        Assert.DoesNotContain(activeMenu.Affordances, option => option.AnchorPlayer == 1);
        Assert.All(otherMenu.Affordances, option =>
        {
            Assert.Equal(Game.ActionVerb, option.Verb);
            Assert.Equal(1, option.AnchorPlayer);
        });
        Assert.False(otherMenu.Cancellable);
        var action = Assert.Single(otherMenu.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == may!.ObjectId);
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
        var(game, world) = Playing(board =>
        {
            board.Seats[1].IdentityCard.TurnTo("01010a");
            board.Seats[1].IdentityCard.TakeDamage(2);
            energy = board.CreateCard("01087", board.Seats[1].Hand);
        }, heroes: ["spider_man", "captain_marvel"]);
        int captain = world.Seats[1].IdentityCard.ObjectId;
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == captain);
        var price = Assert.Single(action.CostOptions);
        Assert.Equal(1, action.AnchorPlayer);
        Assert.Contains(price.Generators, source => source.Effect == energy!.ObjectId);
        Assert.DoesNotContain(price.Generators, source => world.Seats[0].Hand.Cards.Any(card => card.ObjectId == source.Effect));
        game.Resolve(Decision.Take(action.Id, [], [energy!.ObjectId]));
        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == captain);
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
        var runner = Runner(alliance, "Action", """{ "draw": { "player": "you", "count": 1 } }""");
        var(game, world) = Playing(board =>
        {
            Hand(board, player: 0, Physicals, count: 0);
            Hand(board, player: 1, Mentals, count: 0);
            card = board.CreateCard(alliance, board.Seats[0].Hand);
            mine = board.CreateCard(Physicals, board.Seats[0].Hand);
            theirs = board.CreateCard("01088", board.Seats[1].Hand); // two energy
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == card!.ObjectId);
        var price = Assert.Single(action.CostOptions);
        Assert.Contains(price.Generators, source => source.Effect == mine!.ObjectId);
        Assert.Contains(price.Generators, source => source.Effect == theirs!.ObjectId);
        int playerZeroHand = world.Seats[0].Hand.Cards.Count;
        int playerOneHand = world.Seats[1].Hand.Cards.Count;
        game.Resolve(Decision.Take(action.Id, [], [mine!.ObjectId, theirs!.ObjectId]));
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
        var runner = Runner(alliance, "Action", """{ "draw": { "player": "you", "count": 1 } }""", includeAuthored: true);
        var(game, world) = Playing(board =>
        {
            Hand(board, player: 0, Physicals, count: 0);
            Hand(board, player: 1, Mentals, count: 0);
            card = board.CreateCard(alliance, board.Seats[0].Hand);
            doubleEnergy = board.CreateCard("01088", board.Seats[0].Hand);
        }, heroes: ["captain_marvel", "spider_man"], abilities: runner);
        int scientist = world.Seats[1].IdentityCard.ObjectId;
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == card!.ObjectId);
        var price = Assert.Single(action.CostOptions);
        Assert.Contains(price.Generators, source => source.Effect == scientist);
        game.Resolve(Decision.Take(action.Id, [], [doubleEnergy!.ObjectId, scientist]));
        Assert.Equal(DeckType.DiscardPile, card!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, doubleEnergy.Area.Type);
        Assert.DoesNotContain(runner.ResourceAbilities(world, 1), source => source.Effect == scientist);
    }

    [Fact]
    public void AnEventWithPrintedAndArrowCostsIsRejectedBeforePayment()
    {
        const string alliance = "25036";
        Card? card = null;
        var runner = Runner(alliance, "Action", """{ "draw": { "player": "you", "count": 1 } }""", cost: """{ "spend": "Y" }""");
        var(game, world) = Playing(board =>
        {
            Hand(board, Physicals, 4);
            card = board.CreateCard(alliance, board.Seats[0].Hand);
        }, abilities: runner);
        var eventCard = Assert.IsType<Card>(card);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == eventCard.ObjectId);
        int[] payment = [..world.Seats[0].Hand.Cards.Where(candidate => candidate.ObjectId != eventCard.ObjectId).Select(candidate => candidate.ObjectId)];
        Assert.Throws<RulesNotImplementedException>(() => runner.Act(world, new PendingAbility(eventCard.ObjectId, AbilityType.Action, 0), payment, []));
        Assert.Same(world.Seats[0].Hand, eventCard.Area);
        Assert.All(payment, id => Assert.Same(world.Seats[0].Hand, world.Cards[id].Area));
    }
}
