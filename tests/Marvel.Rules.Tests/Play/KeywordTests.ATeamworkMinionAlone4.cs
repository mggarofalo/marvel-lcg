using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class KeywordATeamworkMinionAloneTests : KeywordTestBase
{
    [Rule("rr:teamwork")]
    [Fact]
    public void ATeamworkMinionAloneDoesNothing()
    {
        // "At least one **other** minion." The arriving minion does not count
        // itself, and a minion of a different trait is not one of its own --
        // which is the clause `rr:teamwork.1`'s shorter restatement drops.
        var printed = new Printed().With("hero", ("HP", "10")).With("acolyte", ("Teamwork", "ACOLYTE"), ("ATK", "2"), ("HP", "3")).With("stranger", ("HP", "3")).Trait("acolyte", "ACOLYTE").Trait("stranger", "HYDRA");
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        var alone = world.CreateCard("acolyte", engaged);
        Reveal.Teamwork(world, printed, alone, 0, round: 1);
        Assert.False(world.Agenda.IsBusy);
        world.CreateCard("stranger", engaged);
        Reveal.Teamwork(world, printed, alone, 0, round: 1);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:loses")]
    [Rule("rr:teamwork")]
    [Fact]
    public void AMinionThatLosesTeamworkDoesNotActivate()
    {
        // Teamwork remains printed, but the lost keyword supplies no forced
        // response when a matching minion is already in play.
        var printed = new Printed().With("acolyte", ("Teamwork", "ACOLYTE"), ("ATK", "2"), ("HP", "3")).With("friend", ("HP", "3")).Trait("acolyte", "ACOLYTE").Trait("friend", "ACOLYTE");
        var world = Board(printed);
        var engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        world.CreateCard("friend", engaged);
        var arriving = world.CreateCard("acolyte", engaged);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("teamwork"), Affects: arriving.ObjectId));
        Reveal.Teamwork(world, printed, arriving, 0, round: 1);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:teamwork")]
    [Rule("rr:activation.1")]
    [Fact]
    public void ATeamworkMinionSchemesAgainstAnAlterEgo()
    {
        // The difference from quickstrike, which says outright "a player whose
        // identity is in hero form". Teamwork says the minion **activates**,
        // and `rr:activation.1` reads the form to choose between attacking and
        // scheming -- so an alter-ego is schemed at rather than left alone.
        var printed = new Printed().With("acolyte", ("Teamwork", "ACOLYTE"), ("ATK", "2"), ("SCH", "2"), ("HP", "3")).With("friend", ("HP", "3")).Trait("acolyte", "ACOLYTE").Trait("friend", "ACOLYTE");
        var world = Board(printed);
        var engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        world.CreateCard("friend", engaged);
        var arriving = world.CreateCard("acolyte", engaged);
        Reveal.Teamwork(world, printed, arriving, 0, round: 1);
        var step = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(Steps.Scheme, step.What);
        Assert.Equal(arriving.ObjectId, step.Subject);
    }

    [Rule("rr:teamwork")]
    [Fact]
    public void ATeamworkMinionCountsFriendsInAnotherPlayersArea()
    {
        // "In play", not "engaged with you". A minion in the other player's
        // area is in play, and this is unreachable at one player -- which is
        // the only board the recording has.
        var printed = new Printed().With("hero", ("HP", "10")).With("acolyte", ("Teamwork", "ACOLYTE"), ("ATK", "2"), ("HP", "3")).With("friend", ("HP", "3")).Trait("acolyte", "ACOLYTE").Trait("friend", "ACOLYTE");
        var world = Board(printed, players: 2);
        var arriving = world.CreateCard("acolyte", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("friend", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        Reveal.Teamwork(world, printed, arriving, 0, round: 1);
        Assert.Equal(arriving.ObjectId, Assert.Single(world.Agenda.Outstanding).Subject);
    }

    [Rule("rr:quickstrike")]
    [Fact]
    public void AMinionWithoutQuickstrikeWaitsForTheVillainPhase()
    {
        // The keyword is the whole of it: an ordinary minion engaging a hero
        // does nothing until step 2 of the next villain phase.
        var printed = new Printed().With("hero", ("HP", "10")).With("minion", ("ATK", "3"), ("HP", "3"));
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Reveal.Quickstrike(world, printed, minion, 0, round: 1);
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:uses-x-type")]
    [Fact]
    public void ACardWithUsesEntersPlayWithItsCounters()
    {
        // "When a card with this keyword enters play, place X all-purpose
        // counters from the token pool on the card. The word following the
        // value establishes and identifies the type." Printed as one field
        // holding both -- `"3,web"` -- so the type travels with the count.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var card = world.CreateCard("sideScheme", world.AreaOf(DeckType.RevealingArea));
        Reveal.Resolve(world, printed, card, 0, []);
        Assert.Equal(3, card.Tokens["c_web"]);
    }

    [Rule("rr:loses")]
    [Rule("rr:uses-x-type")]
    [Fact]
    public void ACardThatLosesUsesEntersPlayWithoutItsCounters()
    {
        // The composite value remains printed, but the lost Uses keyword no
        // longer places its counters as the card enters play.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var card = world.CreateCard("sideScheme", world.AreaOf(DeckType.RevealingArea));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("uses"), Affects: card.ObjectId));
        Reveal.Resolve(world, printed, card, 0, []);
        Assert.Equal(0, card.Tokens.GetValueOrDefault("c_web"));
    }

    [Rule("rr:loses")]
    [Rule("rr:uses-x-type.1")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RegainingUsesWithNoCountersImmediatelyDiscardsTheCard(bool expires)
    {
        // Uses is a constant ability: as soon as the loss ends, its live
        // zero-counter condition discards the card. This is true whether a
        // duration expires normally or the registration ends early.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var card = world.CreateCard("sideScheme", world.AreaOf(DeckType.RevealingArea));
        var loss = world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("uses"), Affects: card.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfPlayerPhase)));
        Reveal.Resolve(world, printed, card, 0, []);
        var events = new List<GameEvent>();
        if (expires)
        {
            world.Effects.Expire(TimingPoints.EndOfPlayerPhase, events);
        }
        else
        {
            loss.Dispose();
        }

        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
        if (expires)
        {
            Assert.Contains(events.OfType<CardsMoved>(), moved => moved.Cards.Any(landing => landing.Card == card.ObjectId));
        }
    }

    [Rule("rr:uses-x-type.1")]
    [Rule("rr:permanent.5")]
    [Fact]
    public void RestoringUsesPreflightsEveryDiscardBeforeEndingAnyLoss()
    {
        // Both Uses constants would become active at the same timing point.
        // A Permanent attachment makes the second discard unsupported, so the
        // complete transition refuses before either card or either loss moves.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web")).With("permanentish", ("Permanent", "1"));
        var world = Board(printed);
        var first = world.CreateCard("sideScheme", world.AreaOf(DeckType.RevealingArea));
        var second = world.CreateCard("sideScheme", world.AreaOf(DeckType.RevealingArea));
        var duration = Duration.UntilEndOf(TimingPoints.EndOfPlayerPhase);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("uses"), Affects: first.ObjectId, Lasts: duration));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("uses"), Affects: second.ObjectId, Lasts: duration));
        Reveal.Resolve(world, printed, first, 0, []);
        Reveal.Resolve(world, printed, second, 0, []);
        var attachment = world.CreateCard("permanentish", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, second.ObjectId));
        Assert.Throws<RulesNotImplementedException>(() => world.Effects.Expire(TimingPoints.EndOfPlayerPhase, []));
        Assert.Equal(2, world.Effects.Registered.Count);
        Assert.Equal(DeckType.SideSchemesArea, first.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, second.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, attachment.Area.Type);
    }

    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void RestoringUsesRefusesAHostingCycleBeforeEndingEitherLoss()
    {
        // A cyclic hosted component has no root to discard first. It is
        // refused explicitly rather than pruning every candidate as somebody
        // else's child and leaving active Uses cards at zero counters.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web")).With("permanentish", ("Permanent", "1"));
        var world = Board(printed);
        var first = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var second = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        World.MoveToTop(first, world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, second.ObjectId));
        World.MoveToTop(second, world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, first.ObjectId));
        var duration = Duration.UntilEndOf(TimingPoints.EndOfPlayerPhase);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("uses"), Affects: first.ObjectId, Lasts: duration));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("uses"), Affects: second.ObjectId, Lasts: duration));
        var thrown = Assert.Throws<RulesNotImplementedException>(() => world.Effects.Expire(TimingPoints.EndOfPlayerPhase, []));
        Assert.Contains("hosting cycle", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(2, world.Effects.Registered.Count);
        Assert.Equal(second.ObjectId, first.Area.Host);
        Assert.Equal(first.ObjectId, second.Area.Host);
    }

    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void RestoringAnotherCardsUsesIgnoresAFacedownDroneUnderlyingUsesCard()
    {
        // A facedown encounter card has no active printed attributes. Ending a
        // different card's Uses loss must not expose and apply the player card
        // text hidden beneath a Drone.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web")).With("playerUses", ("Uses", "3,charge"));
        var world = Board(printed);
        world.CreateCard("playerUses", world.Seats[0].Deck);
        var drone = Assert.IsType<Card>(FacedownDrones.EngageTop(world, 0, "test", "Drone", []));
        var card = world.CreateCard("sideScheme", world.AreaOf(DeckType.RevealingArea));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("uses"), Affects: card.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfPlayerPhase)));
        Reveal.Resolve(world, printed, card, 0, []);
        world.Effects.Expire(TimingPoints.EndOfPlayerPhase, []);
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
        Assert.Equal(DeckType.EngagedEnemiesArea, drone.Area.Type);
        Assert.True(FacedownDrones.Is(drone));
    }

    [Rule("rr:ability.5")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void UsesRestoredWhenAConstantSourceLeavesImmediatelyDiscardsTheCard()
    {
        // A constant exists only while its source is in play. Its departure is
        // preflighted as one transition, then the newly active zero-counter
        // Uses constant discards the affected card.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var source = world.CreateCard("temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("filler", world.Seats[0].Deck);
        var card = world.CreateCard("sideScheme", world.AreaOf(DeckType.RevealingArea));
        world.Abilities = new ConstantUsesLoss(source.ObjectId, card.ObjectId);
        Reveal.Resolve(world, printed, card, 0, []);
        Discard.Card(world, source, "test", []);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
    }

    [Rule("rr:ability.5")]
    [Rule("rr:attach-to.1")]
    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void AHostedConstantCannotDiscardItsDepartingHostTwice()
    {
        // The attachment leaves immediately before its host. Ending its
        // constant restores the host's zero-counter Uses, but the host is
        // already part of the same departure and moves exactly once.
        var printed = new Printed().With("sideScheme", ("Uses", "3,web"));
        var world = Board(printed);
        var host = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        var source = world.CreateCard("temp", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, host.ObjectId));
        world.Abilities = new ConstantUsesLoss(source.ObjectId, host.ObjectId);
        var events = new List<GameEvent>();
        Discard.Card(world, host, "test", events);
        Assert.Equal(DeckType.EncounterDiscardPile, host.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Single(events.OfType<CardsMoved>(), moved => moved.Cards.Any(landing => landing.Card == host.ObjectId));
        Assert.Single(events.OfType<CardsMoved>(), moved => moved.Cards.Any(landing => landing.Card == source.ObjectId));
    }
}
