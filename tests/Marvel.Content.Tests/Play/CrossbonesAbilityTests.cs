using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CrossbonesAbilityTests
{
    [Theory]
    [InlineData("04058")]
    [InlineData("04059")]
    [InlineData("04060")]
    public void OnlyALiveWeaponAttachmentGrantsPiercing(string face)
    {
        // Printed Crossbones: "While Crossbones has a Weapon attachment, his attacks gain piercing."
        var (world, villain) = Board(face);
        Assert.False(Keywords.Has(world, villain, "piercing", world.Facts));
        world.CreateCard("04065", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, villain.ObjectId));
        Assert.False(Keywords.Has(world, villain, "piercing", world.Facts));
        var gun = world.CreateCard("04064", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, villain.ObjectId));
        Assert.True(Keywords.Has(world, villain, "piercing", world.Facts));
        Discard.Card(world, gun, "test", []);
        Assert.False(Keywords.Has(world, villain, "piercing", world.Facts));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:attach-to.3.1")]
    public void StageTwoFindsMachineGunInEitherPileAndInitializesItsAmmo(bool discarded)
    {
        // An ability causing attachment to a specific game element replaces its attach-to instruction.
        var (world, villain) = Board("04059", players: 2);
        var gun = world.CreateCard("04064", world.AreaOf(discarded ? DeckType.EncounterDiscardPile : DeckType.EncounterDeck));
        world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        AuthoredCards.Runner().ExecuteReveal(world, villain, 0);
        Assert.Equal(villain.ObjectId, gun.Area.Host);
        Assert.Equal(DeckType.UpgradesArea, gun.Area.Type);
        Assert.Equal(4, gun.Tokens["c_ammo"]);
        Assert.True(Keywords.Has(world, villain, "piercing", world.Facts));
    }

    [Theory]
    [InlineData(false, 4)]
    [InlineData(true, 1)]
    [Rule("rr:status-cards.1")]
    public void HardAsNailsHealsOnlyWhenToughCannotBeGiven(bool alreadyTough, int damage)
    {
        // "A character cannot have more than one status card of each type at a time."
        // 04068 explicitly heals 3 instead when the villain cannot receive tough.
        var (world, villain) = Board("04058");
        villain.TakeDamage(4);
        if (alreadyTough) Statuses.Give(world, villain, "tough");
        var card = world.CreateCard("04068", world.AreaOf(DeckType.RevealingArea));
        AuthoredCards.Runner().ExecuteReveal(world, card, 0);
        Assert.True(Statuses.Has(world, villain, "tough"));
        Assert.Equal(damage, villain.Damage);
    }

    [Fact]
    public void CorneredStaffCountsPrintedBoostsButNotBoostStars()
    {
        // 04071: "Discard 1 [per-player] cards ... Place 1 additional threat here for each boost icon."
        var (world, _) = Board("04058", players: 2);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        world.CreateCard("01104", deck);
        world.CreateCard("04064", deck); // Three boost icons.
        world.CreateCard("04068", deck); // A boost star, no boost icons.
        var card = world.CreateCard("04071", world.AreaOf(DeckType.SideSchemesArea));
        Reveal.Resolve(world, world.Facts, card, 0, []);
        AuthoredCards.Runner().ExecuteReveal(world, card, 0);
        Sequence.Finish(world, world.Facts, AuthoredCards.Runner(), []);
        Assert.Equal(6, card.Tokens["k_threat"]);
        Assert.Equal(2, world.AreaOf(DeckType.EncounterDiscardPile).Cards.Count);
    }

    private static (World World, Card Villain) Board(string face, int players = 1)
    {
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, players);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat("p" + player);
            seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        }
        world.Abilities = AuthoredCards.Runner();
        return (world, world.CreateCard(face, world.AreaOf(DeckType.VillainArea)));
    }
}


