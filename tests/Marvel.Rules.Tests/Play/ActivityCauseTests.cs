using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class ActivityCauseTests : BasicPowerTestBase
{
    [Fact]
    public void BasicThwartThreatReceiptRetainsItsActorAndExactOccurrence()
    {
        // Engine contract choice: causal evidence refers to the actual agenda occurrence.
        var printed = new Printed().With("hero", ("THW", "2"));
        var world = Board(printed);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 5);
        BasicThwartPowers.BasicThwart(world, printed, 0, scheme, []);
        var events = Agendas.Finish(world, printed);
        var threat = Assert.Single(events.OfType<FieldSet>(), entry => entry.Field == "k_threat");
        Assert.NotNull(threat.Cause);
        Assert.Equal(EventCauseKind.Thwart, threat.Cause.Kind);
        Assert.Equal(world.Seats[0].IdentityCard.ObjectId, threat.Cause.Source);
        Assert.Equal(scheme.ObjectId, threat.Cause.Target);
        Assert.True(threat.Cause.Occurrence >= 0);
    }
}
