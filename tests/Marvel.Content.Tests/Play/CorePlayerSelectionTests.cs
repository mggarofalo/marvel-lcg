using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CorePlayerSelectionTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:choose-game-element.3.1")]
    [Fact]
    public void AncestralKnowledgeMovesThreeDifferentTitlesAndLeavesTheRest()
    {
        var world = Board("01040b");
        var card = world.CreateCard("01042", world.Seats[0].Hand);
        var payment = world.CreateCard("01044", world.Seats[0].Hand);
        world.CreateCard("01045", world.Seats[0].Deck);
        var discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
        var first = world.CreateCard("01046", discard);
        var second = world.CreateCard("01047", discard);
        var third = world.CreateCard("01048", discard);
        var left = world.CreateCard("01049", discard);
        var runner = AuthoredCards.Runner();

        runner.Act(
            world, new PendingAbility(card.ObjectId, AbilityType.Action, 0),
            [payment.ObjectId], []);
        var choice = Assert.Single(world.Agenda.Outstanding);
        runner.Chose(
            world, card, 0, choice.Index,
            Decision.Take(card.ObjectId,
                [first.ObjectId, second.ObjectId, third.ObjectId], []),
            choice.Tier);

        Assert.Contains(left, discard.Cards);
        Assert.Contains(first, world.Seats[0].Deck.Cards);
        Assert.Contains(second, world.Seats[0].Deck.Cards);
        Assert.Contains(third, world.Seats[0].Deck.Cards);
    }

    [Rule("rr:traits")]
    [Rule("rr:threat")]
    [Fact]
    public void AerialCrisisInterdictionRequiresTwoDifferentSchemes()
    {
        var world = Board("01010a");
        var aerial = world.CreateCard(
            "01017", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var card = world.CreateCard("01012", world.Seats[0].Hand);
        var payment = world.CreateCard("01044", world.Seats[0].Hand);
        world.CreateCard("01045", world.Seats[0].Deck);
        var main = world.CreateCard("01116a", world.AreaOf(DeckType.MainSchemesArea));
        var side = world.CreateCard("01127", world.AreaOf(DeckType.SideSchemesArea));
        main.PlaceTokens("k_threat", 4);
        side.PlaceTokens("k_threat", 3);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        runner.Act(
            world, new PendingAbility(card.ObjectId, AbilityType.Action, 0),
            [payment.ObjectId], []);
        var choice = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, card, 0, choice.Index, choice.Tier)!;
        Assert.Equal(2, Assert.Single(prompt.Affordances).Targets!.Min);
        runner.Chose(
            world, card, 0, choice.Index,
            Decision.Take(card.ObjectId, [side.ObjectId, main.ObjectId], []),
            choice.Tier);

        Assert.Equal(2, main.Tokens["k_threat"]);
        Assert.Equal(1, side.Tokens["k_threat"]);
        Assert.True(DeckTypes.IsInPlay(aerial.Area.Type));
    }

    [Rule("rr:appendix-ii-setup.step.16")]
    [Rule("rr:search.1")]
    [Rule("rr:search.3")]
    [Fact]
    public void ForesightChoosesABlackPantherUpgradeAfterTheMulligan()
    {
        var world = Board("01040a,01040b");
        world.Seats[0].IdentityCard.TurnTo("01040b");
        var chosen = world.CreateCard("01046", world.Seats[0].Deck);
        var other = world.CreateCard("01047", world.Seats[0].Deck);
        for (int card = 0; card < 6; card++)
        {
            world.CreateCard("01044", world.Seats[0].Deck);
        }
        var runner = AuthoredCards.Runner();
        Assert.Single(runner.PlayerSetupCards(world, 0));
        var game = Game.Begin(world, Cards, runner);

        var setup = game.Resolve(Decision.Decline);

        Assert.Equal(GamePhase.PlayerSetup, game.Phase);
        Assert.Equal(Question.Element, setup.Prompt!.Asking);
        Assert.Equal(
            [chosen.ObjectId, other.ObjectId],
            setup.Prompt.Affordances.Select(option => option.AnchorId).Order());

        game.Resolve(Decision.Take(chosen.ObjectId));

        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.Contains(chosen, world.Seats[0].Hand.Cards);
        Assert.Contains(other, world.Seats[0].Deck.Cards);
    }

    private static World Board(string identity)
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard(identity, seat.Hero);
        world.CreateCard("01113", world.AreaOf(DeckType.VillainArea));
        return world;
    }
}
