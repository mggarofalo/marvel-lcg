using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Production Moxie on a synthetic research board; this does not admit the pack.
public sealed class AntManFormResponseReviewTests
{
    [Fact]
    [Rule("rr:lasting-effects.5")]
    [Rule("rr:villain-phase.step.6")]
    public void MoxiePersistsThroughVillainPhaseAndExpiresOnActualRoundTransition()
    {
        // "Any effects that last ... until the end of the round end" at step 6a.
        var (game, world, source, moxie, _) = AntManFormResponseFixture.Board("12016", controller: 1);
        AntManFormResponseFixture.Change(game, source);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == moxie.ObjectId).Id));
        Assert.Equal(4, AntManFormResponseFixture.Value(world, 1, "attack"));
        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Decline);
        bool sawVillain = false;
        for (int count = 0; game.Round == 1 && count < 80; count++)
        {
            var prompt = Assert.IsType<Prompt>(game.Pending);
            if (game.Phase == GamePhase.VillainPhase)
            {
                sawVillain = true;
                Assert.Equal(4, AntManFormResponseFixture.Value(world, 1, "attack"));
                Assert.Equal(2, AntManFormResponseFixture.Value(world, 1, "thwart"));
                Assert.Equal(4, AntManFormResponseFixture.Value(world, 1, "defense"));
            }
            var offer = prompt.Affordances[0];
            game.Resolve(prompt.Cancellable ? Decision.Decline : new Decision(offer.Id,
                offer.Targets is { } targets ? [.. targets.Legal.Take(targets.Min)] : []));
        }
        Assert.True(sawVillain);
        Assert.Equal(2, game.Round);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.Equal(3, AntManFormResponseFixture.Value(world, 1, "attack"));
        Assert.Equal(1, AntManFormResponseFixture.Value(world, 1, "thwart"));
        Assert.Equal(3, AntManFormResponseFixture.Value(world, 1, "defense"));
    }
    [Fact]
    [Rule("rr:resource.5")]
    public void LawRemovesThreatFromTheChosenSideSchemeOnly()
    {
        // Printed 12031 removes threat from a scheme selected by its controller.
        var (game, world, source, law, main) = AntManFormResponseFixture.Board("12031", controller: 1);
        var side = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens("k_threat", 6);
        var payment = world.CreateCard("01089", world.Seats[1].Hand);
        AntManFormResponseFixture.Change(game, source);
        var response = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == law.ObjectId);
        game.Resolve(new Decision(response.Id, [], [payment.ObjectId],
            Allocations: [new ResourceAllocation(payment.ObjectId, 0, "B")]));
        Assert.Contains(game.Pending!.Affordances, a => a.AnchorId == main.ObjectId);
        var chosen = Assert.Single(game.Pending.Affordances, a => a.AnchorId == side.ObjectId);
        game.Resolve(Decision.Take(chosen.Id));
        Assert.Equal(6, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(2, side.Tokens.GetValueOrDefault("k_threat"));
    }

}
