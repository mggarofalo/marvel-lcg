using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;
using Marvel.Tests;

namespace Marvel.Content.Tests.Play;

public sealed class RedSkullEncounterTests
{
    [Fact]
    public void SleeperRevealedByAnotherPlayerEngagesFirstPlayer()
    {
        // 04130: "When Revealed: The Sleeper engages the first player."
        var f = RedSkullFixture.Create(players: 2);
        var sleeper = f.Reveal("04130", 1);
        f.Finish();
        Assert.Equal(PlayArea.Of(f.World.FirstPlayer), sleeper.Area.PlayArea);
        Assert.Equal(DeckType.EngagedEnemiesArea, sleeper.Area.Type);
        Assert.True(Statuses.Has(f.World, sleeper, Statuses.Tough));
    }

    [Fact]
    public void SleeperAwakenedStaysUntilSleeperDefeatThenBothLeaveTheGame()
    {
        // 04140: "This scheme cannot leave play while The Sleeper is in play."
        var f = RedSkullFixture.Create();
        var scheme = f.Reveal("04140");
        f.Finish();
        var sleeper = Assert.Single(f.World.Cards, card => card.FaceId == "04130");
        Assert.Equal(DeckType.EngagedEnemiesArea, sleeper.Area.Type);
        Agendas.Happening(f.World);
        Defeat.Scheme(f.World, f.World.Facts, scheme, "test", []);
        f.Finish();
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        foreach (var status in Statuses.On(f.World, sleeper, Statuses.Tough).ToArray()) World.MoveToTop(status, f.World.AreaOf(DeckType.RemovedArea));
        DamagePlacement.Resolve(f.World, f.World.Facts, new DamageRequest(f.World.Seats[0].IdentityCard, sleeper, 20, "test", "Damage"), []);
        f.Finish();
        Assert.Equal(DeckType.RemovedArea, sleeper.Area.Type);
        Assert.Equal(DeckType.RemovedArea, scheme.Area.Type);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void CensorThePastAllowsZeroOrDuplicateTitleCards(int selected)
    {
        // 04142: "Each player chooses up to 3 cards in their discard pile".
        var f = RedSkullFixture.Create();
        var discard = f.World.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
        var cards = new[] { f.World.CreateCard("01088", discard), f.World.CreateCard("01088", discard) };
        var scheme = f.Reveal("04142");
        f.Finish();
        Agendas.Happening(f.World);
        Defeat.Scheme(f.World, f.World.Facts, scheme, "test", []);
        int choices = 0;
        f.Finish(prompt =>
        {
            choices++;
            var option = Assert.Single(prompt.Affordances);
            Assert.Equal(0, option.Targets!.Min);
            Assert.Empty(option.Targets.ExclusiveSets!);
            return new Decision(option.Id, cards.Take(selected).Select(card => card.ObjectId).ToArray());
        });
        Assert.Equal(1, choices);
        Assert.Equal(selected, cards.Count(card => card.Area.Type == DeckType.PlayerDeck));
        Assert.Equal(2 - selected, cards.Count(card => card.Area.Type == DeckType.DiscardPile));
    }

    [Fact]
    public void SpreadingLiesAddsTwoThreatToEveryScheme()
    {
        var f = RedSkullFixture.Create();
        var house = Assert.Single(f.World.AreaOf(DeckType.SideSchemesArea).Cards);
        long mainBefore = f.Main.Tokens.GetValueOrDefault("k_threat");
        long houseBefore = house.Tokens.GetValueOrDefault("k_threat");
        f.Reveal("04137");
        f.Finish();
        Assert.Equal(mainBefore + 2, f.Main.Tokens["k_threat"]);
        Assert.Equal(houseBefore + 2, house.Tokens["k_threat"]);
    }

    [Fact]
    public void BitterRivalExhaustsAReadyCharacterForEachSideScheme()
    {
        var f = RedSkullFixture.Create();
        var identity = f.World.Seats[0].IdentityCard;
        f.Reveal("04136");
        f.Finish(prompt => Decision.Take(Assert.Single(prompt.Affordances).Id));
        Assert.False(identity.Ready);
    }
}
