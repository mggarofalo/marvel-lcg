using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class KeywordVictorySiblingTests : KeywordTestBase
{
    [Rule("rr:permanent.5")]
    [Rule("rr:victory-x.1.2")]
    [Fact]
    public void VictorySiblingDoesNotMoveBeforePermanentDepartureRefuses()
    {
        // The complete hosted tree is proved removable before the Victory
        // interrupt moves any sibling. Unsupported Permanent reattachment
        // therefore leaves every card in its original area.
        var printed = new Printed().With("minion", ("HP", "1")).With("attachment", ("Victory", "2")).With("permanentAttachment", ("Permanent", "1"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var victory = world.CreateCard("attachment", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), minion.ObjectId));
        var permanent = world.CreateCard("permanentAttachment", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), minion.ObjectId));
        var events = new List<GameEvent>();
        Agendas.Happening(world);
        Assert.Throws<RulesNotImplementedException>(() => DamagePlacement.Deal(world, printed, minion, minion, 1, "test", "test", events));
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, victory.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, permanent.Area.Type);
        Assert.DoesNotContain(events.OfType<CardsMoved>(), moved => moved.Cards.Any(card => card.Card == victory.ObjectId || card.Card == permanent.ObjectId));
    }

    [Rule("rr:permanent.5")]
    [Rule("rr:victory-x.1.2")]
    [Fact]
    public void PermanentVictoryAttachmentLeavesBeforeItsDefeatedHost()
    {
        // Victory's Forced Interrupt moves this attachment first. Because it
        // is no longer attached when the host leaves, Permanent never needs to
        // resolve its unsupported reattachment instruction.
        var printed = new Printed().With("minion", ("HP", "1")).With("attachment", ("Permanent", "1"), ("Victory", "1"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var attachment = world.CreateCard("attachment", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), minion.ObjectId));
        Agendas.Happening(world);
        DamagePlacement.Deal(world, printed, minion, minion, 1, "test", "test", []);
        Assert.Equal(DeckType.VictoryDisplay, attachment.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
    }

    [Rule("rr:ability.5")]
    [Rule("rr:loses")]
    [Rule("rr:permanent.5")]
    [Rule("rr:victory-x.1.2")]
    [Theory]
    [InlineData(true, DeckType.VictoryDisplay)]
    [InlineData(false, DeckType.EncounterDiscardPile)]
    public void VictoryAttachmentDestinationIsCapturedBeforeConstantsEnd(bool granted, DeckType destination)
    {
        // The Forced Interrupt is determined while H's constant is active.
        // A granted Victory+Permanent attachment therefore leaves first, and
        // an attachment that loses printed Victory remains an ordinary discard
        // even after H's departure ends the grant or loss.
        var printed = new Printed().With("minion", ("HP", "1")).With("attachment", granted ? [("Permanent", "1")] : [("Victory", "1")]);
        var world = Board(printed);
        var host = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var attachment = world.CreateCard("attachment", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), host.ObjectId));
        world.Abilities = new ConstantCharacteristic(host.ObjectId, attachment.ObjectId, granted ? "victory" : Characteristics.LossOf("victory"));
        Agendas.Happening(world);
        DamagePlacement.Deal(world, printed, host, host, 1, "test", "test", []);
        Assert.Equal(destination, attachment.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, host.Area.Type);
    }

    [Rule("rr:ability.5")]
    [Rule("rr:permanent.5")]
    [Fact]
    public void DeparturePreflightProjectsAConstantsEndBeforeMovingAttachments()
    {
        // H temporarily makes A lose Permanent. Once H leaves, Permanent is
        // restored before A loses its host, so unsupported reattachment must
        // refuse the complete defeat without moving either card.
        var printed = new Printed().With("minion", ("HP", "1")).With("attachment", ("Permanent", "1"));
        var world = Board(printed);
        var host = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var attachment = world.CreateCard("attachment", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), host.ObjectId));
        world.Abilities = new ConstantCharacteristic(host.ObjectId, attachment.ObjectId, Characteristics.LossOf("permanent"));
        Agendas.Happening(world);
        Assert.Throws<RulesNotImplementedException>(() => DamagePlacement.Deal(world, printed, host, host, 1, "test", "test", []));
        Assert.Equal(DeckType.EngagedEnemiesArea, host.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, attachment.Area.Type);
    }

    [Rule("rr:ability.5")]
    [Rule("rr:permanent.5")]
    [Rule("rr:victory-x.1.2")]
    [Fact]
    public void VictoryRootDoesNotExemptItsHostedPermanentFromPreflight()
    {
        // V's interrupt lets V itself leave before H, but its child A still
        // loses host V. V currently makes A lose Permanent; projecting V's
        // departure restores Permanent and must refuse before either moves.
        var printed = new Printed().With("minion", ("HP", "1")).With("victoryAttachment", ("Victory", "1")).With("permanentAttachment", ("Permanent", "1"));
        var world = Board(printed);
        var host = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var victory = world.CreateCard("victoryAttachment", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), host.ObjectId));
        var permanent = world.CreateCard("permanentAttachment", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), victory.ObjectId));
        world.Abilities = new ConstantCharacteristic(victory.ObjectId, permanent.ObjectId, Characteristics.LossOf("permanent"));
        Agendas.Happening(world);
        Assert.Throws<RulesNotImplementedException>(() => DamagePlacement.Deal(world, printed, host, host, 1, "test", "test", []));
        Assert.Equal(DeckType.EngagedEnemiesArea, host.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, victory.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, permanent.Area.Type);
    }

    [Rule("rr:ability.5")]
    [Rule("rr:permanent.5")]
    [Fact]
    public void HostedCardKeepsItsOwnConstantsDuringDeparturePreflight()
    {
        // A grants itself Permanent only when H is absent. The projected read
        // must remove H while leaving A in play, or A's own conditional constant
        // disappears and the unsupported reattachment is missed.
        var printed = new Printed().With("minion", ("HP", "1")).With("attachment");
        var world = Board(printed);
        var host = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var attachment = world.CreateCard("attachment", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), host.ObjectId));
        world.Abilities = new ConstantWhenHostAbsent(host.ObjectId, attachment.ObjectId, "permanent");
        Agendas.Happening(world);
        Assert.Throws<RulesNotImplementedException>(() => DamagePlacement.Deal(world, printed, host, host, 1, "test", "test", []));
        Assert.Equal(DeckType.EngagedEnemiesArea, host.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, attachment.Area.Type);
    }

    [Rule("rr:tuck.1")]
    [Rule("rr:victory-x.3")]
    [Fact]
    public void TuckedVictoryCardDiscardsBecauseItIsNotAttached()
    {
        // A tucked card is expressly not attached. Its printed Attachment
        // type and Victory keyword therefore do not receive the attachment's
        // host-defeat interrupt.
        var printed = new Printed().With("minion", ("HP", "1")).With("attachment", ("Victory", "1"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var tucked = world.CreateCard("attachment", world.AreaOf(DeckType.AsideDeck, PlayArea.Of(0), minion.ObjectId));
        Agendas.Happening(world);
        DamagePlacement.Deal(world, printed, minion, minion, 1, "test", "test", []);
        Assert.Equal(DeckType.EncounterDiscardPile, tucked.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
    }

    [Rule("rr:ability.5")]
    [Rule("rr:permanent.5")]
    [Rule("rr:victory-x.1.2")]
    [Fact]
    public void VictoryAttachmentWaitsForTheCompleteDefeatCascadePreflight()
    {
        // H's departure restores zero-counter Uses on U, whose permanent
        // attachment makes that cascade unsupported. V must not take its
        // Victory move until the complete H/V/U/P transaction is proved.
        var printed = new Printed().With("minion", ("HP", "1")).With("victoryAttachment", ("Victory", "1")).With("permanentAttachment", ("Permanent", "1")).With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var host = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var victory = world.CreateCard("victoryAttachment", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), host.ObjectId));
        var uses = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var permanent = world.CreateCard("permanentAttachment", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, uses.ObjectId));
        world.Abilities = new ConstantUsesLoss(host.ObjectId, uses.ObjectId);
        Assert.True(Characteristics.IsLost(world, uses, "uses"));
        Agendas.Happening(world);
        Assert.Throws<RulesNotImplementedException>(() => DamagePlacement.Deal(world, printed, host, host, 1, "test", "test", []));
        Assert.Equal(DeckType.EngagedEnemiesArea, host.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, victory.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, uses.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, permanent.Area.Type);
    }

    [Rule("rr:loses")]
    [Rule("rr:victory-x.2")]
    [Fact]
    public void ADefeatedCardThatLosesVictoryIsDiscarded()
    {
        // Losing Victory removes the replacement destination. The defeated
        // minion therefore goes to the encounter discard pile, not the victory
        // display its printed keyword would otherwise name.
        var printed = new Printed().With("minion", ("HP", "1"), ("Victory", "2"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("victory"), Affects: minion.ObjectId));
        Agendas.Happening(world);
        DamagePlacement.Deal(world, printed, minion, minion, 1, "test", "test", []);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Empty(world.AreaOf(DeckType.VictoryDisplay).Cards);
    }

    [Rule("rr:quickstrike")]
    [Rule("rr:quickstrike.1")]
    [Fact]
    public void AQuickstrikeMinionAttacksTheHeroItEngages()
    {
        // "**Forced Response (Hero)**: after this minion engages a player, it
        // attacks that player." A minion that would otherwise wait for the next
        // villain phase hits at once.
        var printed = new Printed().With("hero", ("HP", "10")).With("minion", ("Quickstrike", "1"), ("ATK", "3"), ("HP", "3"));
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        Reveal.Quickstrike(world, printed, minion, 0, round: 1);
        Undefended(world, printed);
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:quickstrike")]
    [Fact]
    public void AQuickstrikeMinionDoesNothingToAnAlterEgo()
    {
        // "After a minion with the quickstrike keyword engages a player **whose
        // identity is in hero form**." The *(Hero)* on the forced response is
        // the gate.
        var printed = new Printed().With("minion", ("Quickstrike", "1"), ("ATK", "3"), ("HP", "3"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Reveal.Quickstrike(world, printed, minion, 0, round: 1);
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:teamwork")]
    [Fact]
    public void ATeamworkMinionActivatesWhenOneOfItsOwnIsAlreadyThere()
    {
        // "After a minion with teamwork enters play and engages a player, **if
        // there is at least one other minion that shares the specified trait in
        // play**, the minion that just entered play activates against the
        // player it is engaged with."
        var printed = new Printed().With("hero", ("HP", "10")).With("acolyte", ("Teamwork", "ACOLYTE"), ("ATK", "2"), ("HP", "3")).With("friend", ("HP", "3")).Trait("acolyte", "ACOLYTE").Trait("friend", "ACOLYTE");
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        world.CreateCard("friend", engaged);
        var arriving = world.CreateCard("acolyte", engaged);
        KeepEncounterDeckLive(world);
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        Reveal.Teamwork(world, printed, arriving, 0, round: 1);
        Undefended(world, printed);
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
    }
}
