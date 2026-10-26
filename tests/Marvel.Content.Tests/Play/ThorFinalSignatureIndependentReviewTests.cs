using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Nonpublic boards exercise the complete authored book, without Thor product admission.
public sealed class ThorFinalSignatureIndependentReviewTests
{
    [Fact]
    [Rule("rr:cost.5.1")]
    [Rule("rr:initiating-abilities.step.5")]
    public void LightningRejectsWrongTypedAllocationAtomicallyAndAllowsCorrectedRetry()
    {
        // The player "chooses how to divide those resources between those costs."
        // If costs cannot be paid, "abort this process without paying any costs."
        // Printed 06006 separately requires energy before its cost arrow.
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("06006", world.Seats[1].Hand);
        var energy = world.CreateCard("01088", world.Seats[1].Hand);
        var mental = world.CreateCard("01089", world.Seats[1].Hand);
        var before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        var effects = world.Effects.Registered.ToArray();
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == source.ObjectId);
        var cost = Assert.Single(runner.Describe(world, action).CostOptions);
        var values = new Dictionary<string, long> { ["X"] = 1 };
        int[] paying = [energy.ObjectId, mental.ObjectId];
        ResourceAllocation[] invalid =
        [
            new(energy.ObjectId, Cost: 0, PaidAs: "Y"),
            new(mental.ObjectId, Cost: 1, PaidAs: "B")
        ];
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(action, paying, [], values, invalid)));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(effects, world.Effects.Registered);
        ResourceAllocation[] corrected =
        [
            new(mental.ObjectId, Cost: 0, PaidAs: "B"),
            new(energy.ObjectId, Cost: 1, PaidAs: "Y")
        ];
        runner.ExecuteAction(world, new PlayerAction(action, paying, [], values, corrected));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.All(new[] { source, energy, mental }, card =>
        {
            Assert.Equal(DeckType.DiscardPile, card.Area.Type);
            Assert.Equal(PlayArea.Of(1), card.Area.PlayArea);
        });
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void GetOverHereUsesActingSeatsAerialAndEngagesChosenMinionWithThatSeat(
        int aerialSeat, bool transfers)
    {
        // Printed 06014: "If you have the Aerial trait, engage that enemy."
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("06014", world.Seats[1].Hand);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: Traits.Granted + "AERIAL", Affects: world.Seats[aerialSeat].IdentityCard.ObjectId));
        var target = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var decoy = world.CreateCard("01101", target.Area);
        int incarnation = target.Incarnation;
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Contains(prompt.Affordances, a => a.AnchorId == target.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(target.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(1, target.Damage);
        Assert.Equal(PlayArea.Of(transfers ? 1 : 0), target.Area.PlayArea);
        Assert.Equal(incarnation, target.Incarnation);
        Assert.Equal(0, decoy.Damage);
        Assert.Equal(PlayArea.Of(0), decoy.Area.PlayArea);
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.Empty(world.Seats[1].Hand.Cards);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(PlayArea.Of(1), source.Area.PlayArea);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
