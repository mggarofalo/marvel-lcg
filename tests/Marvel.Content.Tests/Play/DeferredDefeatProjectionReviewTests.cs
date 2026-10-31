using System.Collections.Immutable;
using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DeferredDefeatProjectionReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OpaqueDefeatProjectionCannotAuthorizeAReceiptRemovalFromALiveChosenScheme(bool chooseDeparted)
    {
        // Synthetic projection contract, not a claimed admitted action shape.
        // Engine choice: unknown defeat-window work cannot justify later reads
        // of a different live scheme. A departed retained recipient needs none.
        var world = ThorActualFixture.Board("06001a");
        var source = world.CreateCard("09003", world.Seats[0].Hand);
        var patrol = world.CreateCard("04154", world.AreaOf(DeckType.SideSchemesArea));
        patrol.PlaceTokens("k_threat", 2);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 8);
        var chosen = chooseDeparted ? patrol : main;
        var query = new AbilityQueryContext(world, source, new Occurrence(0, []), 0,
            source.Incarnation, new AbilityCardReference(chosen, chosen.Area.Id, chosen.Incarnation),
            null, null, []);
        var expressions = new AbilityExpressionContext(query,
            ImmutableDictionary<string, long>.Empty.Add("boostIconsLookedAt", 2), [], "", 0, false, null);
        var scope = new AbilityAdmission.AbilityAdmissionScope(new AbilityAdmissionContext(
            AuthoredCards.Program, world.ResourceAbilities, expressions, new(), "thwart"), []);
        string before = Observe(world);
        var prefix = new AbilityEffect.RemoveThreat(new AbilityCardSelection.Titled("Hydra Patrol"),
            new AbilityNumber.Constant(3), false, null);
        var projected = AbilityPowerTrace.PowerStateAfterKnown(prefix, scope, false, false,
            AbilityPowerStateFactory.Initial(scope));
        var state = Assert.IsType<RuleProjection<AbilityPowerState>.Known>(projected).Value;
        Assert.True(state.DefeatWindowDeferred);
        Assert.Contains(patrol.ObjectId, state.Discarded);
        Assert.DoesNotContain(main.ObjectId, state.Discarded);
        var tail = new AbilityEffect.RemoveThreat(new AbilityCardSelection.Bound(AbilityCardBinding.Chosen),
            new AbilityNumber.Result("boostIconsLookedAt"), false, null);
        if (chooseDeparted)
            AbilityProjectedDefeatWindows.ValidateContinuation(tail, state, scope);
        else
        {
            var failure = Assert.Throws<RulesNotImplementedException>(() =>
                AbilityProjectedDefeatWindows.ValidateContinuation(tail, state, scope));
            Assert.Contains("unprojected defeat window", failure.Message);
        }
        Assert.Equal(before, Observe(world));
    }

    private static string Observe(World world) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, world.Agenda.Current,
        Outstanding = world.Agenda.Outstanding.ToArray(),
    });
}
