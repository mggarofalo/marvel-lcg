using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Canonical 07035 target exclusion in an arranged multi-villain game.</summary>
public sealed class DistractingTauntsTargetTests
{
    [Fact]
    [Rule("rr:response")]
    public void HeroAttackingAMinionCannotPayToDiscardTaunts()
    {
        // Responses resolve "after the specified triggering condition occurs".
        // Printed 07035 specifies "After your hero attacks Piledriver".
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07035", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Reveal.Resolve(world, world.Facts, source, 0, []);
        var payment = world.CreateCard("01090", world.Seats[0].Hand);
        var minion = world.CreateCard("01102", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        BasicPowers.BasicAttack(world, world.Facts, 0, minion, []);
        for (int step = 0; ; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) break;
            Assert.True(step < 12, prompt.Label);
            Assert.DoesNotContain(prompt.Affordances, offer => offer.AnchorId == source.ObjectId);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        }
        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.Equal(2, minion.Damage);
        Assert.Equal(groups[2].Villain.ObjectId, source.Area.Host);
        Assert.Equal(DeckType.HandsArea, payment.Area.Type);
    }
}
