using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Test-local Giant Strength interpretation; this does not admit the Ant-Man pack.
public sealed class GiantStrengthPeriodTests
{
    private static AbilityRunner Runner(string boundary, string timing) => new(AbilityCatalog.Parse("""
        {"cards":[
          {"card":"12009","abilities":[{"trigger":{"event":"WhenFormChanged","timing":"Response","subject":"you","form":"hero"},"when":{"hasTrait":{"card":"you","trait":"GIANT"}},"effect":{"grantUntil":{"card":"you","keyword":"attack","amount":1,"until":"EndOfTurn"}}}]},
          {"card":"01007","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},"effect":{"dealDamage":{"cards":"you","amount":99}}}]},
          {"card":"01006","abilities":[
            {"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},"effect":{"changeForm":{"player":"you","to":{"face":"12001c"}}}},
            {"trigger":{"event":"WhenVillainPhaseBegins","timing":"ForcedResponse","subject":"game"},"effect":{"changeForm":{"player":"you","to":{"face":"12001c"}}}}
          ]}
        ]}
        """.Replace("WhenVillainPhaseBegins", boundary, StringComparison.Ordinal)
            .Replace("ForcedResponse", timing, StringComparison.Ordinal)));

    [Theory]
    [InlineData("WhenVillainPhaseBegins")]
    [InlineData("WhenPlayerTurnEnds")]
    [InlineData("WhenPlayerPhaseEnds")]
    [Rule("rr:lasting-effects.6")]
    public void ChangeOutsidePlayerTurnCannotOfferGiantStrength(string boundary)
    {
        // rr:lasting-effects.6: "can only be initiated during that time period."
        // ruling:b777f490f7a0b34d: no turn is taken during the villain phase.
        var (game, world, _, strength) = Board(boundary);
        game.Resolve(Decision.Decline);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
        if (boundary != "WhenPlayerTurnEnds")
        {
            var discard = Assert.Single(game.Pending!.Affordances, item => item.Verb == Game.EndPhaseVerb);
            game.Resolve(Decision.Take(discard.Id, [], []));
            Assert.Equal(GamePhase.VillainPhase, game.Phase);
        }
        Assert.Equal("12001c", world.Seats[0].IdentityCard.FaceId);
        Assert.DoesNotContain(game.Pending!.Affordances, item => item.AnchorId == strength.ObjectId);
        Assert.Equal(3, CardValues.Evaluate(world, world.Seats[0].IdentityCard, "attack", world.Facts, 1).CurrentValue);
    }

    [Fact]
    [Rule("rr:lasting-effects.5")]
    public void PlayerTurnChangeOffersStrengthAndExpiresAtTurnEnd()
    {
        // rr:lasting-effects.5: "expires as soon as the timing point specified by its duration is reached."
        var (game, world, source, strength) = Board();
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source.ObjectId && item.Verb == Game.ActionVerb);
        game.Resolve(Decision.Take(action.Id));
        var response = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == strength.ObjectId);
        game.Resolve(Decision.Take(response.Id));
        Assert.Equal(4, CardValues.Evaluate(world, world.Seats[0].IdentityCard, "attack", world.Facts, 1).CurrentValue);
        game.Resolve(Decision.Decline);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.Equal(3, CardValues.Evaluate(world, world.Seats[0].IdentityCard, "attack", world.Facts, 1).CurrentValue);
    }

    [Fact]
    [Rule("rr:lasting-effects.6")]
    public void EndTurnInterruptStillOffersStrengthBeforeTheTurnEnds()
    {
        // rr:lasting-effects.6: "can only be initiated during that time period."
        var (game, world, _, strength) = Board("WhenPlayerTurnEnds", "ForcedInterrupt");
        game.Resolve(Decision.Decline);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        var response = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == strength.ObjectId);
        game.Resolve(Decision.Take(response.Id));
        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.Equal(3, CardValues.Evaluate(world, world.Seats[0].IdentityCard,
            "attack", world.Facts, 1).CurrentValue);
    }

    [Fact]
    [Rule("rr:lasting-effects.6")]
    public void TheNextPlayersTurnOpensItsOwnLastingPeriod()
    {
        // rr:lasting-effects.6: "can only be initiated during that time period."
        var (game, world, _, _) = Board(players: 2);
        game.Resolve(Decision.Decline);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.Equal(1, game.Active);
        var source = world.Cards.Single(c => c.FaceId == "01006" && c.Owner == 1);
        var strength = world.Cards.Single(c => c.FaceId == "12009" && c.Owner == 1);
        var action = Assert.Single(game.Pending!.Affordances,
            a => a.AnchorId == source.ObjectId && a.Verb == Game.ActionVerb);
        game.Resolve(Decision.Take(action.Id));
        var response = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == strength.ObjectId);
        game.Resolve(Decision.Take(response.Id));
        Assert.Equal(4, CardValues.Evaluate(world, world.Seats[1].IdentityCard,
            "attack", world.Facts, 2).CurrentValue);
        game.Resolve(Decision.Decline);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.Equal(3, CardValues.Evaluate(world, world.Seats[1].IdentityCard,
            "attack", world.Facts, 2).CurrentValue);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [Rule("rr:player-elimination.4")]
    public void EliminatingTheLastTurnSeatClosesItsPeriod(int players)
    {
        // rr:player-elimination.4: "if all players are eliminated, the game ends and the players lose."
        var (game, world, _, _) = Board(players: players, lethal: true);
        if (players == 2) game.Resolve(Decision.Decline);
        var action = Assert.Single(game.Pending!.Affordances,
            a => a.Verb == Game.ActionVerb && world.Cards[a.AnchorId].FaceId == "01007");
        game.Resolve(Decision.Take(action.Id));
        Assert.False(world.Agenda.PlayerTurnOpen);
        Assert.Equal(players == 1 ? GamePhase.Over : GamePhase.EndPhase, game.Phase);
        if (players == 1) Assert.Null(game.Pending);
        else Assert.Equal(0, game.Active);
    }

    [Fact]
    [Rule("rr:lasting-effects.5")]
    [Rule("rr:player-turn.6")]
    public void EliminatedActivePlayersTurnExpiresSurvivorsStrength()
    {
        // rr:lasting-effects.5: a lasting effect expires when its timing point is reached.
        var (game, world, _, _) = Board(players: 2);
        var otherSource = world.Cards.Single(c => c.FaceId == "01006" && c.Owner == 1);
        var strength = world.Cards.Single(c => c.FaceId == "12009" && c.Owner == 1);
        var lethal = world.CreateCard("01007", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var offTurn = Assert.Single(game.PromptFor(1)!.Affordances, a => a.AnchorId == otherSource.ObjectId);
        game.Resolve(Decision.Take(offTurn.Id));
        var response = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == strength.ObjectId);
        game.Resolve(Decision.Take(response.Id));
        Assert.Equal(4, CardValues.Evaluate(world, world.Seats[1].IdentityCard, "attack", world.Facts, 2).CurrentValue);
        var action = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == lethal.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(1, game.Active);
        Assert.Equal(3, CardValues.Evaluate(world, world.Seats[1].IdentityCard, "attack", world.Facts, 2).CurrentValue);
    }

    private static (Game Game, World World, Card Source, Card Strength) Board(
        string boundary = "WhenVillainPhaseBegins", string timing = "ForcedResponse", int players = 1, bool lethal = false)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = Runner(boundary, timing);
        var world = new World(cards, players, 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 12; i++) world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        for (int i = 0; i < 12; i++) world.CreateCard("01087", seat.Deck);
        world.CreateCard("01087", seat.Hand);
        var source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var strength = world.CreateCard("12009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        if (players == 2)
        {
            var other = world.CreateSeat("p1");
            other.IdentityCard = world.CreateCard("12001a,12001b,12001c", other.Hero);
            for (int i = 0; i < 12; i++) world.CreateCard("01087", other.Deck);
            world.CreateCard("01087", other.Hand);
            world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            world.CreateCard("12009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        }
        if (lethal) world.CreateCard("01007", world.AreaOf(DeckType.AlliesArea,
            PlayArea.Of(players - 1), cardOwner: players - 1));
        world.Abilities = runner;
        var game = Game.Begin(world, cards, runner);
        for (int i = 0; i < players; i++)
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
                item => item.Verb == Game.ResolveMulligans).Id));
        return (game, world, source, strength);
    }
}
