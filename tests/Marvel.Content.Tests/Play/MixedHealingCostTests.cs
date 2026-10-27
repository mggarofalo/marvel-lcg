using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic ordered arrow costs; no content admission.</summary>
public sealed class MixedHealingCostTests : CoreCardTestBase
{
    [Theory]
    [InlineData("dealDamage", false, 1, true, 1)]
    [InlineData("dealDamage", true, 1, false, 1)]
    [InlineData("takeDamage", false, 1, true, 1)]
    [InlineData("takeDamage", true, 1, true, 1)]
    [InlineData("dealDamage", false, 0, true, 0)]
    [Rule("rr:initiating-abilities.step.5")]
    public void HealingUsesTheActualPhysicalPaymentOrder(string kind, bool healFirst,
        int initial, bool offered, int remaining)
    {
        // "Pay the cost(s)". Engine choice: taking costs precede other physical
        // components; dealing costs retain their authored order with healing.
        string hit = $$$$"""{"{{{{kind}}}}":{"cards":"you","amount":2}}""";
        const string heal = """{"heal":{"card":"you","amount":2}}""";
        var (world, runner, source) = Board(healFirst ? [heal, hit] : [hit, heal]);
        world.Seats[0].IdentityCard.TakeDamage(initial);
        Verify(world, runner, source, offered, remaining);
    }

    [Theory]
    [InlineData(true, 0, 0, true, 0)]
    [InlineData(false, 1, 1, true, 1)]
    [InlineData(false, 1, 2, true, 0)]
    [InlineData(false, 2, 1, true, 0)]
    [InlineData(false, 2, 0, false, 0)]
    [Rule("rr:prevent.1")]
    [Rule("rr:tough.2")]
    public void RepeatedDamageSharesToughAndActivePrevention(bool tough, int preventions,
        int uses, bool offered, int remaining)
    {
        // Prevention "reduce[s] the amount of damage the target takes"; Tough
        // prevents "all of that damage" for one instance, then is discarded.
        const string hit = """{"dealDamage":{"cards":"you","amount":2}}""";
        var (world, runner, source) = Board([hit, hit, """{"heal":{"card":"you","amount":2}}"""]);
        var target = world.Seats[0].IdentityCard;
        if (tough) Statuses.Give(world, target, Statuses.Tough);
        for (int i = 0; i < preventions; i++)
            world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                "preventDamage", Amount: 1, Affects: target.ObjectId,
                Lasts: uses == 0 ? null : new Duration(Uses: uses)));
        Verify(world, runner, source, offered, remaining);
    }

    [Fact]
    public void ProjectionReadsRemainingPreventionUsesWithoutConsumingThem()
    {
        const string hit = """{"dealDamage":{"cards":"you","amount":2}}""";
        var (world, runner, source) = Board([hit, hit, """{"heal":{"card":"you","amount":2}}"""]);
        var registration = world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "preventDamage", Amount: 1, Affects: world.Seats[0].IdentityCard.ObjectId,
            Lasts: new Duration(Uses: 2)));
        world.Effects.Use(registration.Effect);
        Assert.Equal(1, registration.Remaining);
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Equal(1, registration.Remaining);
        Verify(world, runner, source, true, 1);
        Assert.DoesNotContain(registration.Effect, world.Effects.Active());
    }

    [Theory]
    [InlineData("dealDamage", true, 0)]
    [InlineData("takeDamage", false, 1)]
    [Rule("rr:cost.12")]
    public void PartialTakingCannotFundHealingButPartialDealingCan(string kind, bool offered, int remaining)
    {
        // Taking is unpaid "unless all of that damage was taken".
        var (world, runner, source) = Board([
            $$$$"""{"{{{{kind}}}}":{"cards":"you","amount":2}}""",
            """{"heal":{"card":"you","amount":2}}"""]);
        var target = world.Seats[0].IdentityCard;
        target.TakeDamage(1);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "preventDamage", Amount: 1, Affects: target.ObjectId));
        Verify(world, runner, source, offered, remaining);
    }

    private static void Verify(World world, AbilityRunner runner, Card source, bool offered, int remaining)
    {
        string before = Snapshot(world);
        for (int query = 0; query < 2; query++)
            Assert.Equal(offered, runner.Actions(world, 0).Any(action => action.Card == source.ObjectId));
        Assert.Equal(before, Snapshot(world));
        var action = new PendingAbility(source.ObjectId, AbilityType.Action, 0, 0);
        if (!offered)
        {
            Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, new PlayerAction(action, [], [])));
            Assert.Equal(before, Snapshot(world));
            return;
        }
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.Equal(remaining, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(2, source.Tokens.GetValueOrDefault("c_test"));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void SelectedGeneratorAndDirectDamageSupplyTheirCombinedHealingCost()
    {
        var (world, runner, source) = Board([
            """{"spend":"Y"}""", """{"dealDamage":{"cards":"you","amount":1}}""",
            """{"heal":{"card":"you","amount":2}}"""]);
        var generator = world.CreateCard("01091", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var hand = world.CreateCard("01088", world.Seats[0].Hand);
        string before = Snapshot(world);
        var offer = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(offer, [hand.ObjectId], [])));
        Assert.Equal(before, Snapshot(world));
        runner.ExecuteAction(world, new PlayerAction(offer, [generator.ObjectId], []));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(2, source.Tokens.GetValueOrDefault("c_test"));
        Assert.Same(world.Seats[0].Hand, hand.Area);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static (World World, AbilityRunner Runner, Card Source) Board(string[] steps)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse($$$$"""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"seq":[{{{{string.Join(",", steps)}}}}]},
              "effect":{"placeCounters":{"card":"this","counter":"test","count":{"result":"healed"}}}
            }]},{"card":"01091","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"this"},
              "cost":{"dealDamage":{"cards":"you","amount":1}},"effect":{"generate":"Y"}
            }]}]}
            """));
        var world = Hero("01001a");
        world.Abilities = runner;
        var source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        return (world, runner, source);
    }

    private static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, world.Agenda.Stage,
        Pending = world.Agenda.Outstanding.Select(step => new { step.What, step.Index, step.Subject, step.Seat }),
    });
}
