using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class CardPlayAnUpgradeAttachedToAnotherPlayersCardTests : CardPlayTestBase
{
    [Rule("rr:ownership-and-control.2.1")]
    [Rule("rr:ownership-and-control.7.2")]
    [Rule("rr:upgrade.3.1")]
    [Fact]
    public void AnUpgradeAttachedToAnotherPlayersCardIsTheirsUntilItLeavesPlay()
    {
        // An upgrade on another player's card is controlled by that player,
        // but leaving play still sends it to its owner's equivalent out-of-play
        // area. The two play areas make control visible; the discard piles make
        // ownership visible.
        var printed = Cards().With("shared", ("Cost", "0"), ("RES", "R"));
        var world = Table(printed);
        var owner = world.Seats[0];
        var controller = world.Seats[1];
        var upgrade = world.CreateCard("shared", owner.Hand);
        var abilities = new Targets(controller.IdentityCard.ObjectId);
        CardPlay.Play(world, printed, abilities, owner, upgrade, [], [], [controller.IdentityCard.ObjectId]);
        Assert.Equal(0, upgrade.Owner);
        Assert.Equal(1, upgrade.Area.PlayArea.Player);
        Assert.Equal(controller.IdentityCard.ObjectId, upgrade.Area.Host);
        Discard.Card(world, upgrade, "test", []);
        Assert.Same(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(owner.Index)), upgrade.Area);
    }

    [Rule("rr:ownership-and-control.2")]
    [Fact]
    public void APlayerUpgradeAttachedToAnEncounterCardStaysUnderItsOwnersControl()
    {
        // Encounter cards belong to the scenario, but the rule only transfers
        // an attached upgrade to "a player other than the upgrade's owner."
        // An upgrade such as Webbed Up remains in its owner's play area while
        // its host sits in the villain's.
        var printed = Cards().With("web", ("Cost", "0"), ("RES", "R"));
        var world = Board(printed);
        var villain = world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        var upgrade = InHand(world, "web");
        var abilities = new Targets(villain.ObjectId);
        CardPlay.Play(world, printed, abilities, world.Seats[0], upgrade, [], [], [villain.ObjectId]);
        Assert.Equal(0, upgrade.Area.PlayArea.Player);
        Assert.Equal(villain.ObjectId, upgrade.Area.Host);
    }

    [Rule("rr:max-maximum")]
    [Rule("rr:max-maximum.3")]
    [Fact]
    public void MaxPerPlayerRemovesTheCardFromOffersAndRejectsForgedPlay()
    {
        var printed = Cards().With("limited", ("Cost", "0"), ("MaxPerUnit", "1"));
        var world = Board(printed);
        var inPlay = world.CreateCard("limited", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var copy = InHand(world, "limited");
        Assert.NotNull(inPlay);
        Assert.Null(CardPayment.Price(world, printed, world.Seats[0], copy));
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, printed, new Silent(), world.Seats[0], copy, [], []));
    }

    [Rule("rr:max-maximum.3")]
    [Fact]
    public void MaxPerPlayerUsesTheChosenUpgradeController()
    {
        var printed = Cards().With("limited", ("Cost", "0"), ("MaxPerUnit", "1"));
        var world = Table(printed);
        var copy = InHand(world, "limited");
        var first = world.Seats[0].IdentityCard;
        var second = world.Seats[1].IdentityCard;
        world.CreateCard("limited", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), first.ObjectId, cardOwner: 0));
        var abilities = new Targets(first.ObjectId, second.ObjectId);
        world.Abilities = abilities;
        Assert.NotNull(CardPayment.Price(world, printed, world.Seats[0], copy));
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, printed, abilities, world.Seats[0], copy, [], [], [first.ObjectId]));
        CardPlay.Play(world, printed, abilities, world.Seats[0], copy, [], [], [second.ObjectId]);
        Assert.Equal(1, copy.Area.PlayArea.Player);
    }

    [Rule("rr:max-maximum.3.1")]
    [Fact]
    public void ControlCannotTransferIntoAnAlreadyReachedPerPlayerMaximum()
    {
        var printed = Cards().With("limited", ("Cost", "0"), ("MaxPerUnit", "1"));
        var world = Table(printed);
        var owned = world.CreateCard("limited", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var other = world.CreateCard("limited", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        Assert.Throws<RulesNotImplementedException>(() => CardControlTransfer.TakeControl(world, printed, other, player: 0));
        Assert.Equal(0, owned.Area.PlayArea.Player);
        Assert.Equal(1, other.Area.PlayArea.Player);
        Assert.Equal(1, other.Owner);
    }

    [Rule("rr:max-maximum")]
    [Rule("rr:max-maximum.4")]
    [Fact]
    public void MaxPerGameElementIsCheckedForEachAttachmentTarget()
    {
        // “Max 1 per ally” is a limit on each host, not on the controller's
        // play area. A second copy cannot attach to the first ally, while the
        // same title remains legal on a different ally.
        var printed = Cards().With("limited", ("Cost", "0"), ("MaxPerUnit", "1"), ("MaxPerUnitKind", "ally"));
        var world = Board(printed);
        var first = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var second = world.CreateCard("bruiser", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("limited", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), first.ObjectId, cardOwner: 0));
        var copy = InHand(world, "limited");
        var abilities = new Targets(first.ObjectId, second.ObjectId);
        world.Abilities = abilities;
        Assert.NotNull(CardPayment.Price(world, printed, world.Seats[0], copy));
        Assert.Equal([second.ObjectId], CardPlayLegality.LegalAttachmentTargets(world, printed, world.Seats[0], copy, abilities));
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, printed, abilities, world.Seats[0], copy, [], [], [first.ObjectId]));
        CardPlay.Play(world, printed, abilities, world.Seats[0], copy, [], [], [second.ObjectId]);
        Assert.Equal(second.ObjectId, copy.Area.Host);
    }
}
