using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorNamedIdentityReviewTests : CoreCardTestBase
{
    [Fact]
    public void MjolnirDoesNotModifyJaneFosterThor()
    {
        // ruling:8b3af19c1dfc9452: "The cards in Thor's hero kit that refer to Thor only refer to him."
        var world = ThorActualFixture.Board("06001b");
        var jane = world.CreateCard("13011", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        Assert.Equal(Cards.PrintedValue("13011", "ATK", 1), Modified(world, jane, "attack"));
        Assert.False(Traits.Has(world, jane, "AERIAL", world.Facts));
    }

    [Fact]
    public void LadySifDoesNotReadyJaneFosterWhenOdinsonIsAlreadyReady()
    {
        // The named-identity ruling excludes Jane Foster, including when her title matches the first selector.
        var world = ThorActualFixture.Board("06001b");
        var runner = AuthoredCards.Runner();
        var jane = world.CreateCard("13011", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        jane.Exhaust();
        var sif = world.CreateCard("06002", world.Seats[0].Hand);
        var energy = world.CreateCard("06022", world.Seats[0].Hand);
        var genius = world.CreateCard("06023", world.Seats[0].Hand);
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], sif, [energy.ObjectId, genius.ObjectId], []);
        var prompt = Sequence.Work(world, world.Facts, runner, []);
        if (prompt is not null)
        {
            var response = Assert.Single(prompt.Affordances, a => a.AnchorId == sif.ObjectId);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), []);
            Sequence.Finish(world, world.Facts, runner, []);
        }
        Assert.False(jane.Ready);
    }
}
