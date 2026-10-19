using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Rules.Prompts;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class TaskmasterAbilityTests
{
    [Theory]
    [InlineData(true, "01005", true)]
    [InlineData(true, "01012", false)]
    [InlineData(false, "01012", true)]
    [InlineData(false, "01005", false)]
    public void MimicryUsesTheDiscardedPrintedTraitAndCurrentForm(bool hero, string face, bool activates)
    {
        // 04105: ATTACK in hero form causes an attack; THWART in alter-ego causes a scheme.
        var (world, villain) = Board();
        if (!hero) Forms.Change(world.Seats[0], world.Facts);
        Statuses.Give(world, villain, "stunned");
        Statuses.Give(world, villain, "confused");
        for (int i = 0; i < 5; i++) world.CreateCard(face, world.Seats[0].Deck);
        world.CreateCard("01014", world.Seats[0].Deck);
        var treachery = world.CreateCard("04105", world.AreaOf(DeckType.RevealingArea));

        AuthoredCards.Runner().ExecuteReveal(world, treachery, 0);
        Sequence.Finish(world, world.Facts, AuthoredCards.Runner(), []);

        Assert.Equal(!(hero && activates), Statuses.Has(world, villain, "stunned"));
        Assert.Equal(!(!hero && activates), Statuses.Has(world, villain, "confused"));
        Assert.Equal(5, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)).Cards.Count);
        Assert.Single(world.Seats[0].Deck.Cards);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FormChangePunishesOnlyThePlayerNowInHeroForm(bool hero)
    {
        // 04093 responds after a player changes to hero form and counts printed boost icons.
        var (world, _) = Board();
        if (!hero) Forms.Change(world.Seats[0], world.Facts);
        var identity = world.Seats[0].IdentityCard;
        world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        var boost = world.CreateCard("04064", world.AreaOf(DeckType.EncounterDeck));
        var runner = AuthoredCards.Runner();
        var occurrence = new Occurrence(1, [Steps.FormChanged], Subject: identity.ObjectId, Player: 0);
        var responses = runner.Waiting(world, occurrence, WindowKind.Response);
        if (hero)
        {
            var response = Assert.Single(responses);
            runner.ResolveWindow(world, occurrence, response, new Decision(response.Card, [], []));
            Sequence.Finish(world, world.Facts, runner, []);
            Assert.Equal(world.Facts.PrintedValue(boost.FaceId, "Boost", 1), identity.Damage);
            Assert.Equal(DeckType.EncounterDiscardPile, boost.Area.Type);
        }
        else
        {
            Assert.Empty(responses);
            Assert.Equal(0, identity.Damage);
            Assert.Equal(DeckType.EncounterDeck, boost.Area.Type);
        }
    }

    [Fact]
    public void CapturedByHydraStoresOnePhysicalCaptiveAndRescuesItOnDefeat()
    {
        // 04107: random set-aside CAPTIVE ally is facedown beneath; the defeater adds it to hand.
        var (world, _) = Board();
        var first = world.CreateCard("04097", world.AreaOf(DeckType.AsideDeck));
        var second = world.CreateCard("04098", world.AreaOf(DeckType.AsideDeck));
        var scheme = world.CreateCard("04107", world.AreaOf(DeckType.SideSchemesArea));
        var runner = AuthoredCards.Runner();
        runner.ExecuteReveal(world, scheme, 0);
        var captive = Assert.Single(BeneathCards.Of(world, scheme));
        Assert.Contains(captive, new[] { first, second });
        Assert.False(captive.FaceUp);
        Defeat.Scheme(world, world.Facts, scheme, "defeat", [], by: 0, recordOn: new Occurrence(2, []));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Contains(captive, world.Seats[0].Hand.Cards);
        Assert.Equal(0, captive.Owner);
        Assert.True(captive.FaceUp);
        Assert.Equal(DeckType.RemovedArea, scheme.Area.Type);
    }

    private static (World World, Card Villain) Board()
    {
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
        world.Abilities = AuthoredCards.Runner();
        return (world, world.CreateCard("04093", world.AreaOf(DeckType.VillainArea)));
    }
}




