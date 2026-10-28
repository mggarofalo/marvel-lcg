using static Marvel.Content.Tests.Play.AntManNemesisCardFixture;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual printed faces on a nonpublic research board; this does not admit the pack.
public sealed class AntManNemesisCardTests
{
    [Fact]
    public void YellowjacketReadsTheCurrentEngagedIdentityAcrossFacesAndSeats()
    {
        var runner = Runner();
        var world = new World(Facts, 2, 101);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat("p" + player);
            seat.IdentityCard = world.CreateCard(player == 0 ? "12001a,12001b,12001c" : "06001a,06001b", seat.Hero);
        }
        world.Abilities = runner;
        var enemy = world.CreateCard(Yellowjacket, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Seats[0].IdentityCard.TurnTo("12001c");
        world.Seats[1].IdentityCard.TurnTo("06001a");
        AssertProfile(world, enemy, giant: true, tiny: false, attack: 2, retaliate: 1);
        world.Seats[0].IdentityCard.TurnTo("12001a");
        AssertProfile(world, enemy, giant: false, tiny: true, attack: 3, retaliate: 0);
        world.Seats[0].IdentityCard.TurnTo("12001b");
        AssertProfile(world, enemy, giant: false, tiny: false, attack: 2, retaliate: 0);
        world.Seats[0].IdentityCard.TurnTo("12001c");
        World.MoveToTop(enemy, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        AssertProfile(world, enemy, giant: false, tiny: false, attack: 2, retaliate: 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:attach-to")]
    [Rule("rr:uses-x-type")]
    public void SizeIncreaseSelectsPreferredHostAndAppliesPrintedValuesOnce(bool present)
    {
        // Uses: "When a card with this keyword enters play, place X all-purpose counters" on it.
        var runner = Runner();
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var enemy = present ? world.CreateCard(Yellowjacket,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))) : villain;
        long attack = StateFields.Modified(world, enemy, "attack", world.Facts, world.Players);
        long scheme = StateFields.Modified(world, enemy, "scheme", world.Facts, world.Players);
        var size = world.CreateCard(SizeIncrease, world.AreaOf(DeckType.RevealingArea));
        Reveal.Resolve(world, Facts, size, 0, []);
        Assert.Equal(enemy.ObjectId, size.Area.Host);
        Assert.Equal(3, size.Tokens.GetValueOrDefault("c_size"));
        Assert.Equal(attack + 2, StateFields.Modified(world, enemy, "attack", world.Facts, world.Players));
        Assert.Equal(scheme + 2, StateFields.Modified(world, enemy, "scheme", world.Facts, world.Players));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:activation")]
    [Rule("rr:stun-stunned.7")]
    [Rule("rr:confuse-confused.7")]
    public void SizeCounterChangesOnlyAfterAnActualAttackOrScheme(bool scheme, bool cancelled)
    {
        // rr:activation: "Whenever an enemy attacks or schemes, it is considered to have activated."
        // rr:stun-stunned.7 and rr:confuse-confused.7: replacement means it did not attack or scheme.
        var runner = Runner();
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var size = world.CreateCard(SizeIncrease, world.AreaOf(DeckType.RevealingArea));
        Reveal.Resolve(world, Facts, size, 0, []);
        Assert.Equal(villain.ObjectId, size.Area.Host);
        Assert.True(world.AreaOf(DeckType.EncounterDeck).Cards.Count > 3);
        Assert.True(world.Seats[0].Deck.Cards.Count > 3);
        var mainScheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long threatBefore = mainScheme.Tokens.GetValueOrDefault("k_threat");
        if (cancelled) Statuses.Give(world, villain, scheme ? "confused" : "stunned");
        world.Agenda.Abandon();
        world.Agenda.Add(new PhaseStep(scheme ? new EnemySchemeOperation() : new EnemyAttackOperation(),
            1, 0, Subject: villain.ObjectId, Seat: 0));
        var events = Finish(world, runner);
        Assert.False(world.IsOver);
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
        if (!scheme) Assert.Equal(cancelled ? 0 : 1, events.OfType<AttackCompleted>().Count());
        else if (cancelled) Assert.Equal(threatBefore, mainScheme.Tokens.GetValueOrDefault("k_threat"));
        else Assert.True(mainScheme.Tokens.GetValueOrDefault("k_threat") > threatBefore);
        Assert.Equal(cancelled ? 3 : 2, size.Tokens.GetValueOrDefault("c_size"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:activation")]
    [Rule("rr:uses-x-type")]
    public void SplitResponseTracksYellowjacketAndDiscardsAfterThreeAttacks(bool scheme)
    {
        // rr:activation: "Whenever an enemy attacks or schemes, it is considered to have activated."
        // rr:uses-x-type: "When the last all-purpose counter is removed from a card with uses, discard that card."
        var runner = Runner();
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var enemy = world.CreateCard(Yellowjacket,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var size = world.CreateCard(SizeIncrease, world.AreaOf(DeckType.RevealingArea));
        Reveal.Resolve(world, Facts, size, 0, []);
        Assert.Equal(enemy.ObjectId, size.Area.Host);
        world.Agenda.Abandon();
        Statuses.Give(world, world.Seats[0].IdentityCard, "tough");
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 0, Subject: villain.ObjectId, Seat: 0));
        var other = Finish(world, runner);
        Assert.Single(other.OfType<AttackCompleted>());
        Assert.Equal(3, size.Tokens.GetValueOrDefault("c_size"));
        int count = scheme ? 1 : 3;
        for (int activation = 1; activation <= count; activation++)
        {
            if (!scheme) Statuses.Give(world, world.Seats[0].IdentityCard, "tough");
            var operation = scheme ? (AgendaOperation)new EnemySchemeOperation() : new EnemyAttackOperation();
            world.Agenda.Add(new PhaseStep(operation, 1, 0, Subject: enemy.ObjectId, Seat: 0));
            var events = Finish(world, runner);
            Assert.False(world.IsOver);
            Assert.Null(world.Activation);
            Assert.Empty(world.Agenda.Outstanding);
            if (!scheme) Assert.Single(events.OfType<AttackCompleted>());
            Assert.Equal(3 - activation, size.Tokens.GetValueOrDefault("c_size"));
            if (activation < 3) Assert.Equal(enemy.ObjectId, size.Area.Host);
        }
        if (!scheme) Assert.Equal(DeckType.EncounterDiscardPile, size.Area.Type);
    }

    [Theory]
    [InlineData("12001a", true)]
    [InlineData("12001c", false)]
    [Rule("rr:form-change-form.1")]
    public void VoluntaryGameFormChangeUpdatesYellowjacket(string destination, bool tiny)
    {
        // rr:form-change-form.1: a player can change form during their turn.
        var runner = Runner();
        var world = new World(Facts, 1, 776);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        seat.IdentityCard.TurnTo("12001b");
        var enemy = world.CreateCard(Yellowjacket,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var game = Game.Begin(world, Facts, runner);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
        AssertProfile(world, enemy, giant: false, tiny: false, attack: 2, retaliate: 0);
        int choice = game.Pending!.Affordances.Single(a => a.Verb == Game.ChangeForm && a.Label == destination).Id;
        var result = game.Resolve(Decision.Take(choice));
        Assert.Single(result.Events.OfType<CardFormChanged>());
        Assert.Equal(destination, seat.IdentityCard.FaceId);
        AssertProfile(world, enemy, giant: !tiny, tiny: tiny, attack: tiny ? 3 : 2, retaliate: tiny ? 0 : 1);
    }

    [Fact]
    [Rule("rr:activation.8.2")]
    public void NestedActivationResolvesEachAttachedEnemyResponseBeforeTheNextAttack()
    {
        // rr:activation.8.2: "All abilities triggered by the initial activation resolve before subsequent activations initiate."
        var runner = Runner(nested: true);
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var first = world.CreateCard(SizeIncrease, world.AreaOf(DeckType.RevealingArea));
        Reveal.Resolve(world, Facts, first, 0, []);
        var enemy = world.CreateCard(Yellowjacket, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var second = world.CreateCard(SizeIncrease, world.AreaOf(DeckType.RevealingArea));
        Reveal.Resolve(world, Facts, second, 0, []);
        Assert.Equal(villain.ObjectId, first.Area.Host);
        Assert.Equal(enemy.ObjectId, second.Area.Host);
        Statuses.Give(world, world.Seats[0].IdentityCard, "tough");
        world.Agenda.Abandon();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 0, Subject: villain.ObjectId, Seat: 0));
        var events = Finish(world, runner);
        Assert.False(world.IsOver);
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(new[] { villain.ObjectId, enemy.ObjectId }, events.OfType<AttackCompleted>().Select(e => e.Enemy));
        Assert.Equal(2, first.Tokens.GetValueOrDefault("c_size"));
        Assert.Equal(2, second.Tokens.GetValueOrDefault("c_size"));
        int firstRemoval = events.FindIndex(e => e is FieldSet f && f.Card == first.ObjectId && f.Field == "c_size" && f.To == 2);
        int nestedStart = events.FindIndex(e => e is AttackBoundary a && a.Source == enemy.ObjectId && a.Started);
        Assert.True(firstRemoval >= 0 && firstRemoval < nestedStart);
    }

    private static void AssertProfile(World world, Card enemy, bool giant, bool tiny, long attack, long retaliate)
    {
        Assert.Equal(giant, Traits.Has(world, enemy, "GIANT", world.Facts));
        Assert.Equal(tiny, Traits.Has(world, enemy, "TINY", world.Facts));
        Assert.Equal(attack, StateFields.Modified(world, enemy, "attack", world.Facts, world.Players));
        Assert.Equal(retaliate, StateFields.Modified(world, enemy, "retaliate", world.Facts, world.Players));
    }

}
