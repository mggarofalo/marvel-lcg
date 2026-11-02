using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic variants of authored text; these do not change what any card does.
public sealed class PlayerlessResolutionOwnerTests
{
    [Fact]
    [Rule("rr:first-player.2")]
    public void AnEncounterSetupLookNamingNoPlayerIsAskedOfTheFirstPlayer()
    {
        // rr:first-player.2: "If an encounter card requires a card ability to be
        // resolved, a game function to be performed, or a choice to be made but does
        // not specify which player should act, the first player does so."
        // A 04112a variant that first looks at the top two encounter cards.
        const string setup = "\"timing\": \"Setup\",";
        int scheme = AuthoredCards.Text.IndexOf("\"card\": \"04112a\"", StringComparison.Ordinal);
        int seq = AuthoredCards.Text.IndexOf("\"seq\": [", AuthoredCards.Text.IndexOf(setup, scheme, StringComparison.Ordinal),
            StringComparison.Ordinal) + "\"seq\": [".Length;
        string variant = AuthoredCards.Text[..seq]
            + "{\"inspectEncounterTop\":{\"count\":2,\"result\":\"treacheriesLookedAt\"}},"
            + AuthoredCards.Text[seq..];
        var runner = new AbilityRunner(AbilityLowering.Book(AbilityCatalog.Parse(variant)));
        string[] heroes = ["spider_man", "iron_man"];
        var world = WorldSetup.Deal(CardCatalogData,
            Blueprints.From(Dealer.DealOrder(SetupCatalogData, "zola", heroes), CardCatalogData),
            [.. heroes.Select(hero => SetupCatalogData.Hero(hero).Name)], 42, runner);
        var game = Game.Begin(world, CardCatalogData, runner);

        var look = game.Pending!;
        Assert.Equal("Inspect encounter cards", look.Label);
        Assert.Equal(world.FirstPlayer, look.Player);
        Assert.Equal(world.FirstPlayer, Assert.Single(look.Affordances).AnchorPlayer);
        Assert.Same(look, game.PromptFor(world.FirstPlayer));
    }

    [Fact]
    [Rule("rr:first-player.2")]
    [Rule("rr:each-player.1")]
    public void AChoiceNamingNoPlayerAfterAnEachPlayerBlockIsAskedOfTheFirstPlayer()
    {
        // rr:first-player.2: "If an encounter card requires ... a choice to be made but
        // does not specify which player should act, the first player does so."
        // A 04112a variant that looks at the top two encounter cards after its
        // each-player Ultimate Bio-Servant block, in a two-player game. Each
        // rr:each-player.1 ordering lists the first player first, so the block's last
        // seat is the other player.
        const string shuffle = "\"shuffle\": \"encounterDeck\"";
        int scheme = AuthoredCards.Text.IndexOf("\"card\": \"04112a\"", StringComparison.Ordinal);
        int after = AuthoredCards.Text.IndexOf('}', AuthoredCards.Text.IndexOf(shuffle, scheme, StringComparison.Ordinal)) + 1;
        string variant = AuthoredCards.Text[..after]
            + ",{\"inspectEncounterTop\":{\"count\":2,\"result\":\"treacheriesLookedAt\"}}"
            + AuthoredCards.Text[after..];
        var runner = new AbilityRunner(AbilityLowering.Book(AbilityCatalog.Parse(variant)));
        string[] heroes = ["spider_man", "iron_man"];
        var world = WorldSetup.Deal(CardCatalogData,
            Blueprints.From(Dealer.DealOrder(SetupCatalogData, "zola", heroes), CardCatalogData),
            [.. heroes.Select(hero => SetupCatalogData.Hero(hero).Name)], 42, runner);
        var game = Game.Begin(world, CardCatalogData, runner);
        var asked = new List<int>();

        while (game.Pending!.Label != "Inspect encounter cards")
        {
            asked.Add(game.Pending.Player);
            var offer = game.Pending.Affordances[0];
            game.Resolve(Decision.Take(offer.Id, [.. offer.Targets?.Legal.Take(offer.Targets.Min) ?? []], []));
        }

        Assert.NotEqual(world.FirstPlayer, asked[^1]);
        var look = game.Pending;
        Assert.Equal(world.FirstPlayer, look.Player);
        Assert.Equal(world.FirstPlayer, Assert.Single(look.Affordances).AnchorPlayer);
        Assert.Same(look, game.PromptFor(world.FirstPlayer));
    }

    [Fact]
    public void APlayerCardResolvingForNoPlayerRaisesInsteadOfAskingNobody()
    {
        // rr:first-player.2 assigns only an encounter card's unspecified choice.
        // A player card resolving for no seat has no rule naming who answers.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{"trigger":{"timing":"Setup"},
              "effect":{"chooseCard":{"from":{"cardsIn":{"area":"encounterDeck"}},"effect":{"reveal":"chosen"}}}}]}]}
            """));
        var world = new World(CardCatalogData, 1, 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
        world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        var aunt = world.CreateCard("01006", world.AreaOf(DeckType.VillainArea));
        world.Abilities = runner;
        Assert.True(aunt.Owner < 0);

        var error = Record.Exception(() => runner.ExecuteSetup(world, aunt));

        Assert.Contains("player card resolving for no player",
            Assert.IsType<RulesNotImplementedException>(error).Message);
    }
}
