using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SelectedHandDiscardAreaReviewTests
{
    [Theory]
    [InlineData("discardHandCards")]
    [InlineData("discardResourceIcons")]
    public void SyntheticSingularDiscardReadRefusesBeforeCommittingHandSelection(string operation)
    {
        // Repository choice: unprojected changes to a singularly queried area refuse before commitment.
        var runner = Runner("01007", "Action", $$$$"""
            {"seq":[{"{{{{operation}}}}":1},
              {"placeCounters":{"card":{"cardsIn":{"area":"yourDiscardPile","title":"Energy"}},
                "counter":"review","count":1}}]}
            """);
        var (game, world) = Playing(_ => { }, abilities: runner);
        var source = InPlay(world, "01007");
        Hand(world, "01088", 1);
        var hand = Assert.Single(world.Seats[0].Hand.Cards);
        world.CreateCard("01088", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        var step = AbilityContinuationFixtures.Pending(world, source);
        var option = Assert.Single(runner.Choosing(world, step)!.Affordances);
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var agenda = world.Agenda.Outstanding.ToArray();
        var effects = world.Effects.Registered.ToArray();
        var pending = game.Pending;

        for (int attempt = 0; attempt < 2; attempt++)
        {
            var error = Assert.Throws<RulesNotImplementedException>(() =>
                runner.Chose(world, step, new Decision(option.Id, [hand.ObjectId], [])));
            Assert.Contains("singular area query", error.Message);
            Assert.Equal(digest, world.Digest().Canonical());
            Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(words, world.Random.Generator.WordsConsumed);
            Assert.Equal(agenda, world.Agenda.Outstanding);
            Assert.Equal(effects, world.Effects.Registered);
            Assert.Same(pending, game.Pending);
            Assert.Equal(DeckType.HandsArea, hand.Area.Type);
            Assert.True(source.Ready);
        }
    }
}
