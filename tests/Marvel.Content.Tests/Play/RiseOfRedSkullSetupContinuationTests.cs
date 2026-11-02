using Marvel.Rules.Setup;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
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

    [Fact]
    [Rule("rr:first-player.2")]
    public void ASetupSearchNamingNoPlayerAsksTheFirstPlayerAmongSeveralCopies()
    {
        // rr:first-player.2: "If an encounter card requires a card ability to be
        // resolved, a game function to be performed, or a choice to be made but does
        // not specify which player should act, the first player does so."
        // A 04112a variant whose scenario-wide search names Ultimate Bio-Servant, of
        // which the encounter deck holds four, so the search needs an answer.
        int scheme = AuthoredCards.Text.IndexOf("\"card\": \"04112a\"", StringComparison.Ordinal);
        int end = AuthoredCards.Text.IndexOf("\"eachPlayer\"", scheme, StringComparison.Ordinal);
        string variant = AuthoredCards.Text[..scheme] + AuthoredCards.Text[scheme..end]
            .Replace("\"Hydra Prison\"", "\"Ultimate Bio-Servant\"", StringComparison.Ordinal)
            + AuthoredCards.Text[end..];
        var runner = new AbilityRunner(AbilityLowering.Book(AbilityCatalog.Parse(variant)));
        string[] heroes = ["spider_man", "iron_man"];
        var world = WorldSetup.Deal(CardCatalogData,
            Blueprints.From(Dealer.DealOrder(SetupCatalogData, "zola", heroes), CardCatalogData),
            [.. heroes.Select(hero => SetupCatalogData.Hero(hero).Name)], 42, runner);
        var game = Game.Begin(world, CardCatalogData, runner);

        var search = game.Pending!;
        Assert.Equal("04112a: choose a card", search.Label);
        Assert.Equal(world.FirstPlayer, search.Player);
        Assert.Equal(4, search.Affordances.Count(option => option.Label == "04114"));
        Assert.Same(search, game.PromptFor(world.FirstPlayer));
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


