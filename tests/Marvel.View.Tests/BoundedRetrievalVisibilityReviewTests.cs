using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class BoundedRetrievalVisibilityReviewTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void ActualGroupExposesOnlyCandidatesToAnsweringSeatAndRevokesAfterAnswer(int count)
    {
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        }
        world.FirstPlayer = 0;
        var host = world.CreateCard("05007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var other = world.CreateCard("01007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var candidates = new[] { world.CreateCard("01005", world.Seats[1].Hand), world.CreateCard("01004", world.Seats[0].Hand) };
        foreach (var card in candidates) BeneathCards.Place(world, card, host, false, "store", []);
        var unrelated = world.CreateCard("01003", world.Seats[0].Hand);
        BeneathCards.Place(world, unrelated, other, false, "store", []);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"05007","abilities":[{
              "trigger":{"timing":"Action","event":"WhenActionTriggered","subject":"this"},
              "cost":{"exhaust":"this"},
              "effect":{"chooseCardsToHand":{"from":{"query":"beneathThis"},"maximum":3}}
            }]}]}
            """));
        world.Abilities = runner;
        var action = Assert.Single(runner.Actions(world, 1));
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var prompt = Sequence.Work(world, facts, runner, events)!;
        Assert.Equal(1, prompt.Player);
        var entitled = WorldProjection.For(world, prompt, events, new RestrictedVisibilityPolicy(1).Authorize(null, 2));
        var otherSeat = WorldProjection.For(world, prompt, events, new RestrictedVisibilityPolicy(0).Authorize(null, 2));
        Assert.Equal(candidates.Select(card => card.ObjectId), Assert.Single(entitled.Prompt!.Affordances).Targets!.Legal);
        Assert.All(candidates, candidate => Assert.Equal(candidate.FaceId,
            entitled.World.Areas.SelectMany(area => area.Cards).Single(card => card.Id == candidate.ObjectId).Face!.Id));
        Assert.DoesNotContain(entitled.World.Areas.SelectMany(area => area.Cards), card => card.Id == unrelated.ObjectId);
        Assert.Null(otherSeat.Prompt);
        Assert.DoesNotContain(otherSeat.World.Areas.SelectMany(area => area.Cards), card => candidates.Any(candidate => card.Id == candidate.ObjectId));
        Sequence.Answer(world, facts, runner, prompt, new Decision(Assert.Single(prompt.Affordances).Id,
            candidates.Take(count).Select(card => card.ObjectId).ToArray()), events);
        var after = Sequence.Work(world, facts, runner, events);
        var final = WorldProjection.For(world, after, events, new RestrictedVisibilityPolicy(1).Authorize(null, 2));
        Assert.Equal(count, world.Seats[1].Hand.Cards.Count);
        Assert.All(candidates.Skip(count), candidate => Assert.DoesNotContain(final.World.Areas.SelectMany(area => area.Cards), card => card.Id == candidate.ObjectId));
        var finalOther = WorldProjection.For(world, after, events, new RestrictedVisibilityPolicy(0).Authorize(null, 2));
        Assert.DoesNotContain(finalOther.World.Areas.SelectMany(area => area.Cards), card => candidates.Any(candidate => card.Id == candidate.ObjectId));
        Assert.Equal(0, candidates[1].Owner);
    }
}
