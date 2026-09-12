using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class KeywordDiscardingAHostedConstantTests : KeywordTestBase
{
    [Rule("rr:ability.5")]
    [Rule("rr:attach-to.1")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void DiscardingAHostedConstantCanCascadeToItsAcyclicHost()
    {
        // S is already in the departure plan when ending its constant restores
        // its host H. Walking H's hosted tree reaches S again by deduplication,
        // not by an ancestor cycle; both cards still move exactly once.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var host = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var source = world.CreateCard("temp", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, host.ObjectId));
        world.Abilities = new ConstantUsesLoss(source.ObjectId, host.ObjectId);
        var events = new List<GameEvent>();
        Discard.Card(world, source, "test", events);
        Assert.Equal(DeckType.EncounterDiscardPile, host.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Single(events.OfType<CardsMoved>(), moved => moved.Cards.Any(landing => landing.Card == host.ObjectId));
        Assert.Single(events.OfType<CardsMoved>(), moved => moved.Cards.Any(landing => landing.Card == source.ObjectId));
    }

    [Rule("rr:ability.9")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void ADependentConstantEndingRestoresUsesAndDiscardsTheCard()
    {
        // S grants B a trait, and B conditionally makes U lose Uses while it
        // has that trait. S authors no Uses loss itself, but its departure
        // still disables B's condition and restores U at zero counters.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var source = world.CreateCard("temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("filler", world.Seats[0].Deck);
        var bridge = world.CreateCard("bridge", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var uses = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        world.Abilities = new DependentConstantUsesLoss(source.ObjectId, bridge.ObjectId, uses.ObjectId);
        Assert.True(Characteristics.IsLost(world, uses, "uses"));
        Discard.Card(world, source, "test", []);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, uses.Area.Type);
    }

    [Rule("rr:ability.5")]
    [Rule("rr:permanent.5")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void AConstantDeparturePreflightsActiveCrossRootPermanent()
    {
        // S restores U1 and U2 together. U2 grants Permanent to A on U1 while
        // both roots are still in play immediately before the simultaneous
        // departure, so the unsupported host loss is refused atomically.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var source = world.CreateCard("temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("filler", world.Seats[0].Deck);
        var first = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var second = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var attachment = world.CreateCard("attachment", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, first.ObjectId));
        world.Abilities = new UsesLossWithDependentPermanent(source.ObjectId, first.ObjectId, second.ObjectId, attachment.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => Discard.Card(world, source, "test", []));
        Assert.Equal(DeckType.SupportsArea, source.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, first.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, second.Area.Type);
        Assert.Equal(first.ObjectId, attachment.Area.Host);
    }

    [Rule("rr:ability.9")]
    [Rule("rr:permanent.5")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void ADepartingCardCannotActivateANewConditionalConstantMidCommit()
    {
        // S restores U1 and U2. U2's Permanent grant is dormant until U1 is
        // absent, but U2 is itself already departing and therefore cannot
        // activate a new constant between the two preflighted moves.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var source = world.CreateCard("temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("filler", world.Seats[0].Deck);
        var first = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var second = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var attachment = world.CreateCard("attachment", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, second.ObjectId));
        world.Abilities = new UsesLossWithDormantPermanent(source.ObjectId, first.ObjectId, second.ObjectId, attachment.ObjectId);
        Discard.Card(world, source, "test", []);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, first.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, second.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
    }

    [Rule("rr:ability.9")]
    [Rule("rr:permanent.5")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void AUsesCascadeDoesNotActivateAPostCascadePermanentEarly()
    {
        // S restores U1 and U2. Surviving B grants Permanent to A only after
        // U1 is absent, which is after the simultaneous Uses roots qualified;
        // the sequential event writes cannot activate it between their moves.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var source = world.CreateCard("temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("filler", world.Seats[0].Deck);
        var surviving = world.CreateCard("bridge", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var first = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var second = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var attachment = world.CreateCard("attachment", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, second.ObjectId));
        world.Abilities = new UsesLossWithDormantPermanent(source.ObjectId, first.ObjectId, second.ObjectId, surviving.ObjectId, attachment.ObjectId);
        Discard.Card(world, source, "test", []);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.SupportsArea, surviving.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, first.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, second.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
    }

    [Rule("rr:ability.9")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void ASourceDepartureCanActivateASurvivingReplacementUsesLoss()
    {
        // S's loss ending appears to restore U during preflight, but surviving
        // B supplies the same loss as soon as S is actually absent. U still
        // lacks Uses at the commit boundary and therefore is not discarded.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web")).With("permanentish", ("Permanent", "1"));
        var world = Board(printed);
        var source = world.CreateCard("temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("filler", world.Seats[0].Deck);
        var surviving = world.CreateCard("bridge", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var uses = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        world.Abilities = new UsesLossWithSurvivingReplacement(source.ObjectId, surviving.ObjectId, uses.ObjectId);
        Discard.Card(world, source, "test", []);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.SupportsArea, surviving.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, uses.Area.Type);
        Assert.True(Characteristics.IsLost(world, uses, "uses"));
    }

    [Rule("rr:ability.9")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void ARejectedUsesRootKeepsItsDependentRootFromDeparting()
    {
        // S appears to restore U2, and suppressing U2 during preflight appears
        // to restore U3. Once S is absent, U2's own replacement loss keeps U2
        // in play, so its loss on U3 also remains and neither root departs.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web")).With("permanentish", ("Permanent", "1"));
        var world = Board(printed);
        var source = world.CreateCard("temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("filler", world.Seats[0].Deck);
        var second = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var third = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var permanent = world.CreateCard("permanentish", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, second.ObjectId));
        world.Abilities = new ReplacementUsesLossCascade(source.ObjectId, second.ObjectId, third.ObjectId);
        Discard.Card(world, source, "test", []);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, second.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, third.Area.Type);
        Assert.Equal(second.ObjectId, permanent.Area.Host);
        Assert.True(Characteristics.IsLost(world, second, "uses"));
        Assert.True(Characteristics.IsLost(world, third, "uses"));
    }

    [Rule("rr:ability.9")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void AQualifiedUsesDepartureStaysLatchedDuringProjection()
    {
        // S makes U lose Uses, while U grants B the trait that disables B's
        // replacement loss. With S projected absent U qualifies; projecting U
        // absent next removes the trait, but that consequence cannot revoke a
        // departure whose zero-counter condition already qualified.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var source = world.CreateCard("temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("filler", world.Seats[0].Deck);
        var bridge = world.CreateCard("bridge", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var uses = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        world.Abilities = new LatchedUsesDeparture(source.ObjectId, uses.ObjectId, bridge.ObjectId);
        Discard.Card(world, source, "test", []);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, uses.Area.Type);
        Assert.Equal(DeckType.SupportsArea, bridge.Area.Type);
    }

    [Rule("rr:ability.9")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void ProjectedAbsenceCanDiscoverAUsesRestoration()
    {
        // B makes U lose Uses only while S is in play. S authors no constant
        // itself, so suppressing emitted source effects cannot discover U;
        // projecting S absent must include every predeparture lost Uses card.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var source = world.CreateCard("temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("filler", world.Seats[0].Deck);
        var bridge = world.CreateCard("bridge", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var uses = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        world.Abilities = new PresenceDependentUsesLoss(source.ObjectId, bridge.ObjectId, uses.ObjectId);
        Discard.Card(world, source, "test", []);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.SupportsArea, bridge.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, uses.Area.Type);
    }

    [Rule("rr:ability.5")]
    [Rule("rr:permanent.5")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void ProjectedAttachmentLegalityKeepsSelfConstantsActive()
    {
        // S restores H's zero-counter Uses. A is hosted by H and its own
        // constant makes A Permanent while it remains in play, so preflight
        // must refuse before projecting A away disables that constant.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var source = world.CreateCard("temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var host = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var attachment = world.CreateCard("attachment", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, host.ObjectId));
        world.Abilities = new UsesLossWithSelfPermanentAttachment(source.ObjectId, host.ObjectId, attachment.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => Discard.Card(world, source, "test", []));
        Assert.Equal(DeckType.SupportsArea, source.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, host.Area.Type);
        Assert.Equal(host.ObjectId, attachment.Area.Host);
    }

    [Rule("rr:ability.5")]
    [Rule("rr:permanent.5")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void AConstantDeparturePreflightsTheCompleteUsesCascade()
    {
        // S restores U1 and U2; discarding U2 would restore U3, whose
        // Permanent attachment cannot yet be resolved. The complete cascade
        // is refused before S, U1, or U2 moves or emits an event.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web")).With("permanentish", ("Permanent", "1"));
        var world = Board(printed);
        var source = world.CreateCard("temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var first = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var second = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var third = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var permanent = world.CreateCard("permanentish", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, third.ObjectId));
        world.Abilities = new ConstantUsesLoss((source.ObjectId, first.ObjectId), (source.ObjectId, second.ObjectId), (second.ObjectId, third.ObjectId));
        var events = new List<GameEvent>();
        Assert.Throws<RulesNotImplementedException>(() => Discard.Card(world, source, "test", events));
        Assert.Equal(DeckType.SupportsArea, source.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, first.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, second.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, third.Area.Type);
        Assert.Equal(third.ObjectId, permanent.Area.Host);
        Assert.Empty(events);
    }
}
