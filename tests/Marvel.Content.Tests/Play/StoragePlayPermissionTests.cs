using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class StoragePlayPermissionTests
{
    [Theory]
    [InlineData("when")]
    [InlineData("form")]
    [InlineData("nested")]
    public void UnsupportedConditionalPermissionFailsAtCompilation(string variant)
    {
        const string permission = """{"allowPlayBeneath":{"kind":"Event","trait":"ARROW"}}""";
        string effect = variant == "nested" ? "{\"seq\":[" + permission + "]}" : permission;
        string form = variant == "form" ? ",\"form\":\"hero\"" : string.Empty;
        string condition = variant == "when" ? ",\"when\":{\"exists\":\"yourHero\"}" : string.Empty;
        var book = AbilityCatalog.Parse("{\"cards\":[{\"card\":\"01006\",\"abilities\":[{\"trigger\":{\"timing\":\"Constant\",\"subject\":\"this\""
            + form + "},\"effect\":" + effect + condition + "}]}]}");

        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Book(book));

        Assert.Contains("direct unconditional Constant", failure.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("matching", true)]
    [InlineData("wrong-trait", false)]
    [InlineData("wrong-owner", false)]
    [InlineData("other-host", false)]
    [InlineData("other-controller", false)]
    [InlineData("departed-host", false)]
    [InlineData("ordinary-attachment", false)]
    public void OnlyTheControlledLiveHostGrantsItsPrintedStoragePermission(string state, bool permitted)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"timing":"Constant","subject":"this"},
              "effect":{"allowPlayBeneath":{"kind":"Event","trait":"ARROW"}}
            }]}]}
            """));
        var (_, world) = Playing(_ => { }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var host = InPlay(world, state == "other-host" ? "01092" : AuthoredCards.AuntMay);
        var card = world.CreateCard(state == "wrong-trait" ? "04035" : "04005",
            world.Seats[state == "wrong-owner" ? 1 : 0].Hand);
        BeneathCards.Place(world, card, host, true, "store", []);
        if (state == "departed-host") World.MoveToTop(host, world.AreaOf(DeckType.RemovedArea));
        if (state == "ordinary-attachment")
            World.MoveToTop(card, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), host.ObjectId, cardOwner: 0));
        if (state == "other-controller") World.MoveToTop(host, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 0));
        var before = world.Digest().Canonical();

        Assert.Equal(permitted, runner.CanPlayFromStorage(world, card, 0));

        Assert.Equal(before, world.Digest().Canonical());
        Assert.DoesNotContain(card, world.Seats[0].Hand.Cards);
    }
}
