using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AttachmentEventIntegrityTests : ChoosingCardsTestBase
{
    [Theory]
    [InlineData("01105", "tough", true)]
    [InlineData("01112", "confused", false)]
    [InlineData("01096", "stunned", false)]
    [Rule("rr:status-cards.1")]
    public void AuthoredStatusEffectsAnnounceOneCreationAndAttachment(
        string face, string status, bool villain)
    {
        // "A character cannot have more than one status card of each type at a time."
        var world = Deal();
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var source = world.CreateCard(face, world.AreaOf(DeckType.RevealingArea));
        var host = villain ? world.TheCardIn(DeckType.VillainArea)! : world.Seats[0].IdentityCard;

        var events = runner.WhenRevealed(world, source, 0);

        var applied = Assert.Single(Statuses.On(world, host, status));
        Assert.Equal(applied.ObjectId, Assert.Single(Assert.Single(events.OfType<CardsCreated>()).Cards).Id);
        Assert.Equal(new CardAttached(applied.ObjectId, host.ObjectId)
            { Trigger = "WhenCardRevealed", Verb = "Give_Status" }, Assert.Single(events.OfType<CardAttached>()));

        var repeated = runner.WhenRevealed(world, source, 0);
        Assert.Empty(repeated.OfType<CardsCreated>());
        Assert.Empty(repeated.OfType<CardAttached>());
        Assert.Equal(applied.ObjectId, Assert.Single(Statuses.On(world, host, status)).ObjectId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:attach-to.1")]
    public void ReturningHellcatDetachesOnlyHerActualAttachments(bool inspired)
    {
        // "If the game element an attachment is attached to leaves play, the attachment is discarded."
        var world = Deal("she_hulk");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var hellcat = world.CreateCard("01020", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var upgrade = inspired ? world.CreateCard("01074",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), hellcat.ObjectId, 0)) : null;
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == hellcat.ObjectId);

        var events = runner.Act(world, action, [], []);

        Assert.Equal(DeckType.HandsArea, hellcat.Area.Type);
        Assert.Single(events.OfType<CardsMoved>(), item => item.Cards.Any(card => card.Card == hellcat.ObjectId));
        if (upgrade is null) Assert.Empty(events.OfType<CardDetached>());
        else
        {
            var detached = Assert.Single(events.OfType<CardDetached>());
            Assert.Equal(upgrade.ObjectId, detached.Card);
            Assert.Equal(hellcat.ObjectId, detached.Host);
            Assert.Equal(DeckType.DiscardPile, upgrade.Area.Type);
            Assert.Single(events.OfType<CardsMoved>(), item => item.Cards.Any(card => card.Card == upgrade.ObjectId));
        }
    }

    [Fact]
    public void HighwayRobberyReturnsItsRealAttachedCardsOnce()
    {
        var world = Deal();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var scheme = world.CreateCard("01166", world.AreaOf(DeckType.SideSchemesArea));
        var placed = runner.WhenRevealed(world, scheme, 0);
        var attachment = Assert.Single(placed.OfType<CardAttached>());
        var card = world.Cards[attachment.Card];
        Assert.Equal(scheme.ObjectId, card.Area.Host);
        var occurrence = new Occurrence(1, [Steps.CardDefeated], Subject: scheme.ObjectId, Player: 0);
        occurrence.Also(new Defeated(scheme.ObjectId, 0, "Thwart") { Kind = CardKind.EncounterSideScheme });
        var ability = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt));

        var events = runner.Resolve(world, occurrence, ability, [], []);

        Assert.Equal(DeckType.HandsArea, card.Area.Type);
        Assert.True(card.FaceUp);
        var detached = Assert.Single(events.OfType<CardDetached>());
        Assert.Equal((card.ObjectId, scheme.ObjectId), (detached.Card, detached.Host));
    }

    [Fact]
    [Rule("rr:attach-to")]
    public void RevealedChargeAnnouncesItsRhinoHost()
    {
        // "Attach to" means the card is attached to the specified game element as it enters play.
        var world = Deal();
        world.Abilities = AuthoredCards.Runner();
        var charge = world.CreateCard(AuthoredCards.Charge, world.AreaOf(DeckType.RevealingArea));
        var events = new List<GameEvent>();

        Marvel.Rules.Play.Reveal.Resolve(world, Cards, charge, 0, events);

        var rhino = world.TheCardIn(DeckType.VillainArea)!;
        Assert.Equal(rhino.ObjectId, charge.Area.Host);
        var attached = Assert.Single(events.OfType<CardAttached>());
        Assert.Equal((charge.ObjectId, rhino.ObjectId), (attached.Card, attached.Host));
    }

    [Fact]
    [Rule("rr:toughness.1")]
    public void ArmoredGuardsToughnessAnnouncesItsStatusHost()
    {
        // "After this character enters play, give it a tough status card."
        var world = Deal();
        var guard = world.CreateCard("01120", world.AreaOf(DeckType.RevealingArea));
        var events = new List<GameEvent>();

        Marvel.Rules.Play.Reveal.Resolve(world, Cards, guard, 0, events);

        var status = Assert.Single(Statuses.On(world, guard, Statuses.Tough));
        Assert.Single(events.OfType<CardsCreated>());
        var attached = Assert.Single(events.OfType<CardAttached>());
        Assert.Equal((status.ObjectId, guard.ObjectId), (attached.Card, attached.Host));
    }

    [Fact]
    [Rule("rr:villain-defeat.3.2")]
    public void RhinoStageChangeNamesBothEndsOfEachInheritedAttachment()
    {
        // "Attachments, upgrades, status cards, counters, and non-damage tokens on a villain carry over to the new stage."
        var world = Deal();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var rhino = world.TheCardIn(DeckType.VillainArea)!;
        rhino.TakeDamage(Damage.Health(world, Cards, rhino) - 1);
        var charge = world.CreateCard(AuthoredCards.Charge,
            world.AreaOf(DeckType.UpgradesArea, rhino.Area.PlayArea, rhino.ObjectId));
        var stunned = Statuses.Give(world, rhino, Statuses.Stunned);
        var events = new List<GameEvent>();

        BasicPowerInitiation.BasicAttack(world, Cards, 0, rhino, events);
        events.AddRange(Agendas.Finish(world, Cards, runner));

        var next = world.TheCardIn(DeckType.VillainArea)!;
        Assert.NotEqual(rhino.ObjectId, next.ObjectId);
        foreach (var card in new[] { charge, stunned })
        {
            Assert.Equal(next.ObjectId, card.Area.Host);
            Assert.Equal(rhino.ObjectId, Assert.Single(events.OfType<CardDetached>(), item => item.Card == card.ObjectId).Host);
            Assert.Equal(next.ObjectId, Assert.Single(events.OfType<CardAttached>(), item => item.Card == card.ObjectId).Host);
        }
    }
}
