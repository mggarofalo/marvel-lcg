using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorBatchBReviewTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:limit")]
    public void DecliningAndAlterEgoEngagementDoNotSpendThorResponse()
    {
        // "Each copy ... may be used X times per the specified period."
        // 06001a: "After you engage a minion, draw 2 cards."
        var world = ThorActualFixture.Board("06001b");
        Assert.Equal(0, Engage(world, take: true));
        Assert.Empty(world.Seats[0].Hand.Cards);
        world.Seats[0].IdentityCard.TurnTo("06001a");
        Assert.Equal(1, Engage(world, take: false));
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.Equal(1, Engage(world, take: true));
        Assert.Equal(2, world.Seats[0].Hand.Cards.Count);
        world.Seats[0].IdentityCard.TurnTo("06001b");
        world.Seats[0].IdentityCard.TurnTo("06001a");
        Assert.Equal(0, Engage(world, take: true));
        Assert.Equal(2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    [Rule("rr:search.1")]
    [Rule("rr:search.2")]
    public void TraitSearchExcludesOtherAreasAndRejectedChoicePreservesContinuation()
    {
        // "the player chooses among those options"; searched cards do not leave
        // "the searched area." 06004 explicitly names "your deck and discard pile".
        var probed = SearchBoard();
        var control = SearchBoard();
        var runner = AuthoredCards.Runner();
        var beforeQuery = probed.Digest().Canonical();
        var beforeRandom = probed.Random.Generator.GetState();
        var beforeAgenda = probed.Agenda.Outstanding.ToArray();
        for (int n = 0; n < 3; n++)
        {
            var action = Assert.Single(runner.Actions(probed, 0), a =>
                probed.Cards[a.Card].FaceId == "06004");
            runner.Describe(probed, action);
        }
        Assert.Equal(beforeQuery, probed.Digest().Canonical());
        Assert.Equal(beforeRandom.Words, probed.Random.Generator.GetState().Words);
        Assert.Equal(beforeRandom.Index, probed.Random.Generator.GetState().Index);
        Assert.Equal(beforeAgenda, probed.Agenda.Outstanding);
        foreach (var world in new[] { probed, control })
        {
            var card = world.Seats[0].Hand.Cards.Single(c => c.FaceId == "06004");
            var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
            runner.ExecuteAction(world, new PlayerAction(action,
                [world.Seats[0].Hand.Cards.Single(c => c.FaceId == "01003").ObjectId], []));
        }
        var prompt = Assert.IsType<Prompt>(Sequence.Work(probed, probed.Facts, runner, []));
        var expected = probed.Seats[0].Deck.Cards.Single(c => c.FaceId == "06009");
        var discard = probed.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0)
            .Cards.Single(c => c.FaceId == "06007");
        Assert.Equal(new[] { expected.ObjectId, discard.ObjectId }.Order(),
            prompt.Affordances.Select(a => a.AnchorId).Order());
        Assert.True(prompt.ExposesConcealedCandidates);
        Assert.False(expected.FaceUp);
        Assert.Equal(0, prompt.Player);
        var digest = probed.Digest().Canonical();
        var random = probed.Random.Generator.GetState();
        var pending = probed.Agenda.Outstanding.ToArray();
        var foreign = probed.Seats[1].Deck.Cards.Single(c => c.FaceId == "06009");
        Assert.Throws<ArgumentException>(() => Sequence.Answer(probed, probed.Facts,
            runner, prompt, Decision.Take(foreign.ObjectId), []));
        Assert.Equal(digest, probed.Digest().Canonical());
        Assert.Equal(random.Words, probed.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, probed.Random.Generator.GetState().Index);
        Assert.Equal(pending, probed.Agenda.Outstanding);
        foreach (var world in new[] { probed, control })
        {
            var choice = Sequence.Work(world, world.Facts, runner, [])!;
            Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(expected.ObjectId), []);
            Sequence.Finish(world, world.Facts, runner, []);
            Assert.Empty(world.Agenda.Outstanding);
        }
        Assert.Equal(control.Digest().Canonical(), probed.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, probed.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, probed.Random.Generator.GetState().Index);
    }

    private static World SearchBoard()
    {
        var world = ThorActualFixture.Board("06001b", 2);
        world.CreateCard("06009", world.Seats[0].Deck);
        world.CreateCard("06007", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("06009", world.Seats[1].Deck);
        world.CreateCard("06007", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
        world.CreateCard("06010", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("06002", world.Seats[0].Hand);
        world.CreateCard("06004", world.Seats[0].Hand);
        world.CreateCard("01003", world.Seats[0].Hand);
        return world;
    }

    private static int Engage(World world, bool take)
    {
        var runner = AuthoredCards.Runner();
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDiscardPile));
        EncounterEntry.Schedule(world, EncounterEntryRequest.For(minion, 0,
            new CardEntryPlacement()), new EffectCompletion(1, 1));
        int offered = 0;
        for (int step = 0; step < 30; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) { Assert.Empty(world.Agenda.Outstanding); return offered; }
            var response = prompt.Affordances.SingleOrDefault(a =>
                a.AnchorId == world.Seats[0].IdentityCard.ObjectId && a.Timing == AbilityType.Response);
            if (response is not null) offered++;
            Sequence.Answer(world, world.Facts, runner, prompt,
                take && response is not null ? Decision.Take(response.Id) : Decision.Decline, []);
        }
        Assert.Fail("Engagement did not finish.");
        return offered;
    }
}
