using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ExhaustionReceiptIndependentTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:exhausted.1")]
    public void EffectReceiptExcludesTheExhaustedCostAndResetsOnTheNextResolution()
    {
        // "An exhausted card cannot be exhausted again until it is ready."
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"seq":[
              {"exhaust":{"withTrait":{"cards":{"query":"supports"},"trait":"PERSONA"}}},
              {"heal":{"card":{"query":"villain"},"amount":{"result":"exhausted"}}},
              {"placeCounters":{"card":"you","counter":"invocations","count":1}}
            ]}
            """, cost: """{"exhaust":"this"}""");
        var world = Hero("01001a", 2);
        world.Abilities = runner;
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        villain.TakeDamage(5);
        var source = InPlay(world, AuthoredCards.AuntMay);
        var other = world.CreateCard("05008", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var second = world.CreateCard("05007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var identity = world.Seats[0].IdentityCard;
        for (int invocation = 0; invocation < 2; invocation++)
        {
            source.Refresh();
            var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
            runner.ExecuteAction(world, new PlayerAction(action, [], []));
            Assert.Equal(3, villain.Damage);
            Assert.False(source.Ready);
            Assert.False(other.Ready);
            Assert.False(second.Ready);
            Assert.Equal(invocation + 1, identity.Tokens["c_invocations"]);
        }
    }
}
