using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic ordered-cost controls; no additional content admission.</summary>
public sealed class IndependentMixedHealingReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:ability.9")]
    public void LaterDamageRespectsProtectionEnabledByEarlierCostDamage(int healing, bool offered)
    {
        // If costs cannot be paid, "abort this process without paying any costs".
        // This synthetic ally cannot take more damage once the first hit lands.
        // Conditional constants are active "anytime the specific condition is met".
        var (world, runner, source) = ProtectionBoard(healing);
        string before = Snapshot(world);
        Assert.Equal(offered, runner.Actions(world, 0).Any(action => action.Card == source.ObjectId));
        Assert.Equal(before, Snapshot(world));
        if (!offered) return;
        var offer = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(offer, [], []));
        Assert.Equal(0, source.Damage);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void SelectedPaymentCannotCommitDamageWhenLaterHealingIsUnpayable()
    {
        // If costs cannot be paid, "abort this process without paying any costs".
        var (world, runner, source) = ProtectionBoard(3);
        string before = Snapshot(world);
        var forged = new PendingAbility(source.ObjectId, AbilityType.Action, 0, 0);
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, new PlayerAction(forged, [], [])));
        Assert.Equal(before, Snapshot(world));
    }

    [Fact]
    [Rule("rr:tough.2")]
    public void ToughDamageConsumesRegisteredPreventionBeforeTheNextCostInstance()
    {
        // Tough says "prevent all of that damage". Engine policy also spends
        // registered prevention on that instance, before the next cost's hit.
        var (world, runner, source) = Board();
        var target = world.Seats[0].IdentityCard;
        Statuses.Give(world, target, Statuses.Tough);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "preventDamage",
            Amount: 1, Affects: target.ObjectId, Lasts: new Duration(Uses: 1)));
        string before = Snapshot(world);
        var offer = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Equal(before, Snapshot(world));
        runner.ExecuteAction(world, new PlayerAction(offer, [], []));
        Assert.Equal(0, target.Damage);
        Assert.False(Statuses.Has(world, target, Statuses.Tough));
        Assert.Empty(DamagePrevention.Active(world, target));
        Assert.Equal(2, source.Tokens.GetValueOrDefault("c_test"));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void APreviouslyOfferedPaymentIsRejectedWhenNewPreventionMakesHealingIncomplete()
    {
        // Inability to pay must "abort this process without paying any costs".
        var (world, runner, source) = Board();
        var offer = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        var target = world.Seats[0].IdentityCard;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "preventDamage",
            Amount: 2, Affects: target.ObjectId));
        string before = Snapshot(world);
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, new PlayerAction(offer, [], [])));
        Assert.Equal(before, Snapshot(world));
    }

    private static (World World, AbilityRunner Runner, Card Source) ProtectionBoard(int healing)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01066","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"seq":[{"dealDamage":{"cards":"this","amount":2}},
                {"dealDamage":{"cards":"this","amount":2}},
                {"heal":{"card":"this","amount":HEALING}}]},
              "effect":{"draw":{"player":"you","count":1}}
            },{
              "trigger":{"timing":"Constant","subject":"this"},
              "effect":{"preventDamageWhile":{"card":"this","condition":{
                "atLeast":{"value":{"damageOn":"this"},"count":1}}}}
            }]}]}
            """.Replace("HEALING", healing.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)));
        var world = Hero("01001a");
        world.Abilities = runner;
        var source = world.CreateCard("01066", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01088", world.Seats[0].Deck);
        world.CreateCard("01088", world.Seats[0].Deck);
        return (world, runner, source);
    }

    private static (World World, AbilityRunner Runner, Card Source) Board()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"seq":[{"dealDamage":{"cards":"you","amount":2}},
                {"dealDamage":{"cards":"you","amount":2}},
                {"heal":{"card":"you","amount":2}}]},
              "effect":{"placeCounters":{"card":"this","counter":"test","count":{"result":"healed"}}}
            }]}]}
            """));
        var world = Hero("01001a");
        world.Abilities = runner;
        return (world, runner, world.CreateCard("01006",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0)));
    }

    private static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, world.Agenda.Stage,
        Pending = world.Agenda.Outstanding.Select(step => new { step.What, step.Index, step.Subject, step.Seat }),
        Prevention = DamagePrevention.CaptureUses(world, world.Seats[0].IdentityCard)
            .Select(entry => new { entry.Effect, entry.Remaining }),
    });
}

