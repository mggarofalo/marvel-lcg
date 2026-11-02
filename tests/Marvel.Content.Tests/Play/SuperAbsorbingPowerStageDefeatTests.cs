using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SuperAbsorbingPowerStageDefeatTests
{
    [Fact]
    [Rule("rr:ability.1")]
    [Rule("rr:villain-defeat")]
    public void DefeatingTheFinalStageWinsWhileThePowerHasNoAbsorbingManToGrant()
    {
        // 04092: "Absorbing Man gains the Ice, Metal, Stone, and Wood traits."
        // rr:ability.1: "Card abilities only interact with cards that are in
        // play." While the defeated stage leaves, no Absorbing Man is in play,
        // so the side scheme grants nothing until a stage of that title is.
        // rr:villain-defeat: "If the final stage of the villain deck is
        // defeated, the players win the game."
        var f = Board();
        World.MoveToTop(f.Villain, f.World.AreaOf(DeckType.RemovedArea));
        var villain = f.World.CreateCard("04077", f.World.AreaOf(DeckType.VillainArea));
        var power = f.Card("04092", DeckType.SideSchemesArea);
        Statuses.Give(f.World, villain, Statuses.Stunned);
        Assert.Empty(f.World.AreaOf(DeckType.VillainDeck).Cards);
        Assert.Contains("STONE", Traits.Of(f.World, villain, f.World.Facts));
        long health = DamagePlacement.Health(f.World, f.World.Facts, villain);
        var hero = f.World.Seats[0].IdentityCard;

        DamagePlacement.Resolve(f.World, f.World.Facts,
            new DamageRequest(hero, villain, health, "test", "Damage"), []);

        Assert.Equal(Outcome.PlayersWin, f.World.Result);
        Assert.Equal(DeckType.RemovedArea, villain.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, power.Area.Type);
    }

    [Fact]
    [Rule("rr:ability.8.2")]
    [Rule("rr:villain-defeat")]
    public void TheNextStageGainsThePowerTraitsWhenItEntersPlay()
    {
        // rr:ability.8.2: "A constant ability becomes active as soon as its card
        // enters play and remains active while the card is in play." The grant
        // names Absorbing Man by title, so the next stage receives it.
        var f = Board();
        f.World.CreateCard("04077", f.World.AreaOf(DeckType.VillainDeck));
        f.Card("04092", DeckType.SideSchemesArea);
        Statuses.Give(f.World, f.Villain, Statuses.Stunned);
        long health = DamagePlacement.Health(f.World, f.World.Facts, f.Villain);
        var hero = f.World.Seats[0].IdentityCard;

        DamagePlacement.Resolve(f.World, f.World.Facts,
            new DamageRequest(hero, f.Villain, health, "test", "Damage"), []);

        var next = Assert.Single(f.World.AreaOf(DeckType.VillainArea).Cards);
        Assert.Equal("04077", next.FaceId);
        Assert.Equal(Outcome.Unfinished, f.World.Result);
        Assert.Equal<string>(["BRUTE", "ICE", "METAL", "STONE", "WOOD"],
            Traits.Of(f.World, next, f.World.Facts));
    }

    private static AbsorbingManFixture Board()
    {
        var f = AbsorbingManFixture.Create();
        var deck = f.World.AreaOf(DeckType.VillainDeck);
        while (deck.Cards.Count > 0)
            World.MoveToTop(deck.Cards[^1], f.World.AreaOf(DeckType.RemovedArea));
        return f;
    }
}
