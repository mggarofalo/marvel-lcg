using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class StatusReplacementTests : StatusTestBase
{
    [Theory]
    [InlineData(false, 1, false)]
    [InlineData(true, 1, true)]
    [InlineData(true, 2, false)]
    [Rule("rr:status-cards.1")]
    [Rule("rr:status-cards.1.1")]
    public void ReplacementRespectsOrdinaryAndSteadyStatusLimits(bool steady, int held, bool legal)
    {
        // "Characters with the steady keyword can have one additional ... stunned status card."
        var world = Board(Cards());
        var host = world.TheCardIn(DeckType.VillainArea)!;
        if (steady) GrantStatusRestriction(world, host, "steady");
        var old = Statuses.Give(world, host, Statuses.Tough);
        for (int i = 0; i < held; i++) Statuses.Give(world, host, Statuses.Stunned);
        var events = new List<GameEvent>();
        string before = world.Digest().Canonical();
        Assert.Equal(legal, StatusReplacement.CanReplace(world, old, Statuses.Stunned));
        Assert.Equal(legal, StatusReplacement.Replace(world, old, Statuses.Stunned, "test", events));
        Assert.Equal(held + (legal ? 1 : 0), Statuses.Count(world, host, Statuses.Stunned));
        if (!legal)
        {
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Empty(events);
        }
        else Assert.DoesNotContain(events, item => item.Verb == "Give_Status");
    }

    [Theory]
    [InlineData("stalwart", "stunned")]
    [InlineData("stalwart", "confused")]
    [InlineData("cannot_be_stunned", "stunned")]
    [InlineData("cannot_be_confused", "confused")]
    [Rule("rr:stalwart.1")]
    [Rule("rr:cannot")]
    public void CannotRestrictionsRejectBeforeRemovingOldStatus(string restriction, string replacement)
    {
        // "This character cannot have confused or stunned status cards."
        var world = Board(Cards());
        var host = world.TheCardIn(DeckType.VillainArea)!;
        GrantStatusRestriction(world, host, restriction);
        var old = Statuses.Give(world, host, Statuses.Tough);
        string before = world.Digest().Canonical();
        var events = new List<GameEvent>();
        Assert.False(StatusReplacement.Replace(world, old, replacement, "test", events));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Empty(events);
        Assert.Null(Statuses.Inflict(world, world.Facts, host, replacement));
    }

    [Theory]
    [InlineData(false, 0, true)]
    [InlineData(true, 0, false)]
    [InlineData(true, 1, true)]
    [Rule("rr:vulnerable.1")]
    [Rule("rr:vulnerable.3")]
    public void VulnerableDiscardsOnlyOnBecomingAfflicted(bool steady, int held, bool discarded)
    {
        // "The vulnerable keyword does not take effect until that character has two ... stunned status cards."
        var world = Board(Cards());
        var host = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        GrantStatusRestriction(world, host, "vulnerable");
        if (steady) GrantStatusRestriction(world, host, "steady");
        for (int i = 0; i < held; i++) Statuses.Give(world, host, Statuses.Stunned);
        var old = Statuses.Give(world, host, Statuses.Tough);
        var events = new List<GameEvent>();
        Assert.True(StatusReplacement.Replace(world, old, Statuses.Stunned, "test", events));
        Assert.Equal(discarded ? DeckType.EncounterDiscardPile : DeckType.EngagedEnemiesArea, host.Area.Type);
        Assert.DoesNotContain(events, item => item.Verb == "Defeat");
    }

    [Fact]
    [Rule("rr:cannot")]
    public void CannotBeToughKeepsTheSelectedConfusedCard()
    {
        // "Cannot ... is absolute, and cannot be countermanded by other abilities."
        var world = Board(Cards());
        var host = world.TheCardIn(DeckType.VillainArea)!;
        GrantStatusRestriction(world, host, "cannot_be_tough");
        var old = Statuses.Give(world, host, Statuses.Confused);
        string before = world.Digest().Canonical();
        var events = new List<GameEvent>();
        Assert.False(StatusReplacement.Replace(world, old, Statuses.Tough, "test", events));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Empty(events);
    }

    private static void GrantStatusRestriction(World world, Card card, string field) =>
        world.Effects.Register(new Marvel.Rules.Timing.ContinuousEffect(
            Marvel.Rules.Timing.EffectSource.LastingEffect, field, Amount: 1,
            Card: card.ObjectId, Affects: card.ObjectId));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:status-cards.1")]
    public void SameKindAndDepartedPhysicalSelectionAreRejectedAtomically(bool steady)
    {
        // A status's physical location identifies the selected card, not only its kind.
        var world = Board(Cards());
        var host = world.TheCardIn(DeckType.VillainArea)!;
        if (steady) GrantStatusRestriction(world, host, "steady");
        string originalKind = steady ? Statuses.Stunned : Statuses.Tough;
        var old = Statuses.Give(world, host, originalKind);
        var events = new List<GameEvent>();
        string before = world.Digest().Canonical();
        Assert.False(StatusReplacement.Replace(world, old, originalKind, "test", events));
        Assert.Equal(before, world.Digest().Canonical());
        Discard.Card(world, old, "test", events);
        events.Clear();
        before = world.Digest().Canonical();
        Assert.False(StatusReplacement.Replace(world, old, Statuses.Stunned, "test", events));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Empty(events);
    }
}
