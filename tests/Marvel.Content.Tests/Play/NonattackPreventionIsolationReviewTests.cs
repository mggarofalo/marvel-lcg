using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class NonattackPreventionIsolationReviewTests
{
    [Fact]
    [Rule("rr:prevent.1")]
    public void AnActuallySuspendedNestedDamageWindowRetainsItsOwnPrevention()
    {
        // "reduce the amount of damage the target takes" applies separately to
        // each assignment, including a nested assignment with its own target prompt.
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        var row = book["cards"]!.AsArray().Single(r => r!["card"]!.GetValue<string>() == "01008")!;
        row["abilities"] = JsonNode.Parse("""
            [{"trigger":{"event":"WhenDamageWouldBeDealt","damage":"taken","timing":"Interrupt","target":"you"},
              "cost":{"discard":"this"},"effect":{"seq":[
                {"preventDamage":{"card":"trigger.target","amount":3}},
                {"dealDamage":{"cards":"trigger.target","amount":2}}
              ]}}]
            """);
        var f = new BlackWidowPreventionFixture(runner: new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString())));
        var source = f.Upgrade("01008");
        var barrier = f.Upgrade("05017");
        barrier.PlaceTokens("c_reflection", 1);
        var result = DamagePlacement.Resolve(f.World, f.World.Facts,
            new(f.Villain, f.Hero, 5, "outer", "Damage"), f.Events);
        f.Use(f.Find(source), source);
        var nested = f.Find(barrier);
        Assert.Equal(2, f.World.Agenda.Occurrence!.DirectDamage!.Remaining);
        Assert.False(result.IsComplete);
        Assert.Equal(0, f.Hero.Damage);
        f.Use(nested, barrier);
        var target = f.Work();
        Assert.Equal(Question.Element, target.Asking);
        Assert.False(result.IsComplete);
        f.Answer(target, Decision.Take(Assert.Single(target.Affordances).Id));
        f.Finish(source, barrier);
        Assert.True(result.IsComplete);
        Assert.Equal(5, result.Dealt);
        Assert.Equal(2, result.Taken);
        Assert.Equal(3, f.Hero.Damage);
        Assert.Equal(1, f.Villain.Damage);
        Assert.Equal(DeckType.DiscardPile, barrier.Area.Type);
        Assert.False(f.World.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:prevent.1.2")]
    public void FullyPreventingOneNonattackInstancePreservesTheOtherActualBarrierForTheNext()
    {
        // "If an effect prevents all damage ... that character is not considered
        // to have taken damage." FAQ05017 rules out further copies at zero remaining.
        var f = new BlackWidowPreventionFixture();
        var first = f.Upgrade("05017");
        var second = f.Upgrade("05017");
        first.PlaceTokens("c_reflection", 1);
        second.PlaceTokens("c_reflection", 1);
        foreach (var source in new[] { first, second })
        {
            var result = DamagePlacement.Resolve(f.World, f.World.Facts,
                new(f.Villain, f.Hero, 1, "separate instance", "Damage"), f.Events);
            f.Use(f.Find(source), source);
            var target = f.Work();
            f.Answer(target, Decision.Take(Assert.Single(target.Affordances).Id));
            f.Finish(first, second);
            Assert.True(result.IsComplete);
            Assert.Equal(1, result.Dealt);
            Assert.Equal(0, result.Taken);
        }
        Assert.Equal(0, f.Hero.Damage);
        Assert.Equal(2, f.Villain.Damage);
        Assert.All(new[] { first, second }, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.False(f.World.Agenda.IsBusy);
    }
}
