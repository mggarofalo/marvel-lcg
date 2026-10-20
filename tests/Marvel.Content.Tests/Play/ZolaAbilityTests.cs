using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ZolaAbilityTests
{
    [Fact]
    [Rule("rr:when-defeated-abilities.2.1")]
    public void DefeatingHydraPrisonReleasesBothOwnersAlliesAndRemovesThePrison()
    {
        // The When Defeated ability resolves before the defeated card leaves play.
        // 04122 returns each captive to its owner's hand and removes this scheme.
        var world = Board();
        var scheme = world.CreateCard("04122", world.AreaOf(DeckType.SideSchemesArea));
        var first = world.CreateCard("01002", world.Seats[0].Hand);
        var second = world.CreateCard("04032", world.Seats[1].Hand);
        BeneathCards.Place(world, first, scheme, false, "setup", []);
        BeneathCards.Place(world, second, scheme, false, "setup", []);

        Defeat.Scheme(world, world.Facts, scheme, "defeat", [], by: 1,
            recordOn: new Occurrence(1, []));
        Sequence.Finish(world, world.Facts, AuthoredCards.Runner(), []);

        Assert.Same(world.Seats[0].Hand, first.Area);
        Assert.Same(world.Seats[1].Hand, second.Area);
        Assert.True(first.FaceUp);
        Assert.True(second.FaceUp);
        Assert.Equal(DeckType.RemovedArea, scheme.Area.Type);
    }

    [Fact]
    public void UltimateBioServantCountsEncounterAttachmentsButNotToughOrPlayerUpgrades()
    {
        // 04114: "+1 ATK for each attachment on it"; ruling:81bce25b60d865d5
        // distinguishes the Attachment card type from attached player upgrades.
        var world = Board();
        var minion = world.CreateCard("04114", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        long attack = StateFields.Modified(world, minion, "attack", world.Facts, 2);
        Statuses.Give(world, minion, "tough");
        world.CreateCard("01009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), minion.ObjectId, 0));
        Assert.Equal(attack, StateFields.Modified(world, minion, "attack", world.Facts, 2));
        var attachment = world.CreateCard("04117", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), minion.ObjectId));
        Assert.Equal(attack + 1, StateFields.Modified(world, minion, "attack", world.Facts, 2));
        Assert.True(Keywords.Has(world, minion, "guard", world.Facts));
        Discard.Card(world, attachment, "discard", []);
        Assert.Equal(attack, StateFields.Modified(world, minion, "attack", world.Facts, 2));
        Assert.False(Keywords.Has(world, minion, "guard", world.Facts));
    }

    private static World Board()
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, 2) { Abilities = AuthoredCards.Runner() };
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat("p" + player);
            seat.IdentityCard = world.CreateCard(player == 0 ? "01001a" : "04031a", seat.Hero);
        }
        return world;
    }
}
