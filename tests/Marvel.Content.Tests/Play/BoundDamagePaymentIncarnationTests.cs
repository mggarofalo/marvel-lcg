using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BoundDamagePaymentIncarnationTests
{
    [Theory]
    [InlineData("takeDamage")]
    [InlineData("dealDamage")]
    [Rule("rr:initiating-abilities.3")]
    [Rule("rr:leaves-play.1")]
    public void PreparedDamageCostsCannotRebindToAReturnedSource(string operation)
    {
        // Initiation stops when departure "prevents a required cost from being paid".
        // This synthetic preparation/commit boundary models an earlier suspended payment.
        var (program, runner) = PaymentRunner(AuthoredCards.AuntMay,
            """{"draw":{"player":"you","count":1}}""",
            cost: $$$"""{"{{{operation}}}":{"cards":"this","amount":1}}""");
        var (world, source) = OrderedPaymentBoard(runner);
        var ability = Assert.Single(program.On(source.FaceId));
        var payment = AbilityCostPayment.Prepare(world, source, 0, ability.Cost,
            program, runner, AbilityPaymentSelection.Capture([], []));
        var area = source.Area;
        Discard.Card(world, source, CardPlay.Verb, []);
        World.MoveToTop(source, area);
        var before = world.Digest().Canonical();
        var events = new List<GameEvent>();

        var error = Assert.Throws<RulesNotImplementedException>(
            () => PreparedCostPayment.Commit(world, payment, runner, events));

        Assert.Contains("accepted copy", error.Message, StringComparison.Ordinal);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Empty(events);
    }
}
