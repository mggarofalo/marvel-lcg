using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingSimultaneousSchemeTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [Rule("rr:triggering-condition.2")]
    [Rule("rr:response.3")]
    [Rule("rr:forced.5")]
    [Rule("rr:forced.6")]
    public void BothThresholdsShareAResponseWindowBeforeEitherSchemeResponds(int first, bool mysticalLink)
    {
        // "a single game occurrence" creates one response window; "the first
        // player determines the order" and each forced ability must "resolve
        // as completely as possible before the next". Breakout and Mystical Link
        // each place threat on all schemes in one instruction.
        var (world, groups, source) = WreckingEncounterCardFixture.Create(
            mysticalLink ? "07015" : "07037", 0, players: 2);
        world.FirstPlayer = 1;
        var runner = WreckingSchemeFixture.Runner();
        world.Abilities = runner;
        groups[0].Scheme.PlaceTokens("k_threat", mysticalLink ? 8 : 9);
        groups[1].Scheme.PlaceTokens("k_threat", mysticalLink ? 8 : 9);
        var events = new List<GameEvent>();
        if (mysticalLink) runner.ExecuteReveal(world, source, 0);
        else world.Agenda.Add(new(new ThreatStageOperation(ThreatStageKind.PlaceThreat), 1, 0));
        var choice = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(choice);
        Assert.Equal(1, choice.Player);
        Assert.False(choice.Cancellable);
        Assert.Equal(new[] { groups[0].Scheme.ObjectId, groups[1].Scheme.ObjectId }.Order(),
            choice.Affordances.Select(offer => offer.AnchorId).Order());
        Assert.Equal(10, groups[0].Scheme.Tokens["k_threat"]);
        Assert.Equal(10, groups[1].Scheme.Tokens["k_threat"]);
        Assert.All(world.Seats, seat =>
        {
            Assert.Equal(0, seat.IdentityCard.Damage);
            Assert.False(Statuses.Has(world, seat.IdentityCard, Statuses.Stunned));
        });
        Sequence.Answer(world, world.Facts, runner, choice,
            Decision.Take(choice.Affordances.Single(offer => offer.AnchorId == groups[first].Scheme.ObjectId).Id), events);
        WreckingSchemeFixture.Finish(world, runner, events);
        long otherThreat = mysticalLink ? 2 : 1;
        Assert.Equal(new long[] { 3, 3, otherThreat, otherThreat },
            groups.Select(group => group.Scheme.Tokens["k_threat"]));
        Assert.All(world.Seats, seat =>
        {
            Assert.Equal(2, seat.IdentityCard.Damage);
            Assert.True(Statuses.Has(world, seat.IdentityCard, Statuses.Stunned));
        });
        // Breakout changes the active villain after step one; revealing Mystical
        // Link does not reach that phase boundary.
        Assert.Same(groups[mysticalLink ? 1 : 0], world.EncounterTopology!.Active);
        var resets = events.OfType<FieldSet>().Where(change => change.Field == "k_threat"
            && change.To == 3).Select(change => change.Card).ToArray();
        Assert.Equal(new[] { groups[first].Scheme.ObjectId, groups[1 - first].Scheme.ObjectId }, resets);
    }
}
