using Marvel.Rules.Setup;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RiseOfRedSkullSetupContinuationTests
{
    [Theory]
    [InlineData("hawkeye", "04004", 706u)]
    [InlineData("spider_woman", "04032", 716u)]
    [InlineData("spider_man", "01002", 42u)]
    [InlineData("iron_man", "01030", 42u)]
    [Rule("rr:ability.4")]
    [Rule("rr:when-revealed-abilities.1")]
    public void HydraPrisonSearchCompletesBeforeTheFirstTurn(string hero, string captive, uint seed)
    {
        // rr:ability.4: "resolve the ability one sentence at a time".
        // rr:when-revealed-abilities.1 resolves encounter When Revealed text during scenario setup.
        // 04122: each player searches for a hero-specific ally and places it facedown beneath this scheme.
        // The single threat placement uses the total printed costs of the cards actually found.
        var runner = AuthoredCards.Runner();
        var world = WorldSetup.Deal(CardCatalogData,
            Blueprints.From(Dealer.DealOrder(SetupCatalogData, "zola", [hero]), CardCatalogData),
            [SetupCatalogData.Hero(hero).Name], seed, runner);
        var game = Game.Begin(world, CardCatalogData, runner);
        var policy = new CoreGamePolicy(CardCatalogData);
        for (int answers = 0; game.Phase != GamePhase.PlayerTurn; answers++)
        {
            Assert.True(answers < 30);
            game.Resolve(policy.Answer(game));
        }
        var prison = Assert.Single(world.AreaOf(DeckType.SideSchemesArea).Cards, card => card.FaceId == "04122");
        var stored = BeneathCards.Of(world, prison);
        var ally = Assert.Single(stored);
        Assert.Equal(captive, ally.FaceId);
        Assert.False(ally.FaceUp);
        Assert.Equal(1 + CardCatalogData.PrintedValue(captive, "Cost", 1), prison.Tokens.GetValueOrDefault("k_threat"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PiercingStrikeResumesItsActionAfterChoosingATarget(bool tough)
    {
        // 04044: deal 3 damage; this attack gains piercing. Constant text precedes the Action in the canonical book.
        Card? strike = null, villain = null;
        var (game, world) = Playing(board =>
        {
            strike = board.CreateCard("04044", board.Seats[0].Hand);
            board.CreateCard("01088", board.Seats[0].Hand);
            villain = board.TheCardIn(DeckType.VillainArea)!;
            if (tough) Statuses.Give(board, villain, Statuses.Tough);
        }, hero: true);
        var play = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == strike!.ObjectId);
        var cost = play.CostOptions[0];
        var payment = cost.Sources!.First(source => source.Generates == "YY");
        var allocation = ResourcePayment.Allocate(cost, [payment.Effect]);
        Assert.NotNull(allocation);
        game.Resolve(Decision.Take(play.Id, [], [payment.Effect], new Dictionary<string, long>(), allocation));
        var choice = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == villain!.ObjectId);
        game.Resolve(Decision.Take(choice.Id));
        Assert.Equal(3, villain!.Damage);
        Assert.False(Statuses.Has(world, villain, Statuses.Tough));
        Assert.Equal(DeckType.DiscardPile, strike!.Area.Type);
    }
}


