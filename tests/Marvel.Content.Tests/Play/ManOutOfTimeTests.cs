using Marvel.Content.Tests.Cards;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ManOutOfTimeTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(0, 2)]
    [InlineData(1, 3)]
    [InlineData(0, 4)]
    [InlineData(1, 5)]
    [Rule("rr:discard.1")]
    public void DiscardsChosenHalfRoundedDownInOneSelection(int player, int handSize)
    {
        // "If a player card is discarded, it is placed faceup on top of the owning player's discard pile."
        // Man Out of Time supplies the chosen half-hand count, rounded down.
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(_ => { }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var seat = world.Seats[player];
        foreach (var card in seat.Hand.Cards.ToArray()) World.MoveToTop(card, seat.Deck);
        var hand = Enumerable.Range(0, handSize).Select(_ => world.CreateCard("01088", seat.Hand)).ToArray();
        var foreign = world.CreateCard("01088", world.Seats[1 - player].Hand);
        var source = world.CreateCard("03026", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(player)));
        runner.ExecuteReveal(world, source, player);
        runner.Chose(world, AbilityContinuationFixtures.Pending(world, source), Decision.Take(1));
        runner.Chose(world, AbilityContinuationFixtures.Pending(world, source), Decision.Take(1));
        int required = handSize / 2;
        var chosen = hand.AsEnumerable().Reverse().Take(required).ToArray();
        if (required > 0)
        {
            var step = AbilityContinuationFixtures.Pending(world, source);
            var prompt = runner.Choosing(world, step)!;
            Assert.Equal(player, prompt.Player);
            var offer = Assert.Single(prompt.Affordances);
            Assert.NotEqual(DeckType.EncounterDiscardPile, source.Area.Type);
            string before = world.Digest().Fingerprint();
            Assert.Throws<ArgumentException>(() => runner.Chose(world, step,
                new Decision(source.ObjectId, [foreign.ObjectId], [])));
            Assert.Equal(before, world.Digest().Fingerprint());
            Assert.Throws<ArgumentException>(() => runner.Chose(world, step,
                new Decision(source.ObjectId, [], [])));
            Assert.Throws<ArgumentException>(() => runner.Chose(world, step,
                new Decision(source.ObjectId, [hand[0].ObjectId, hand[0].ObjectId], [])));
            Assert.Equal(before, world.Digest().Fingerprint());
            runner.Chose(world, step, new Decision(source.ObjectId, chosen.Select(card => card.ObjectId).ToArray(), []));
        }
        Assert.Equal(handSize - required, seat.Hand.Cards.Count);
        Assert.All(chosen, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.All(hand.Except(chosen), card => Assert.Equal(DeckType.HandsArea, card.Area.Type));
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Equal(DeckType.HandsArea, foreign.Area.Type);
    }
    [Theory]
    [InlineData(true, true, false, true)]
    [InlineData(true, false, false, false)]
    [InlineData(false, false, false, true)]
    [InlineData(false, false, true, false)]
    [Rule("rr:form-change-form.4")]
    public void RemovingTheObligationRequiresAReadySteveRogers(bool hero, bool flip, bool exhausted, bool removable)
    {
        // Abilities interacting with an alter-ego do not interact with an identity in hero form.
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(board =>
        {
            var seat = board.Seats[0];
            World.MoveToTop(seat.IdentityCard, board.AreaOf(DeckType.RemovedArea));
            seat.IdentityCard = board.CreateCard("03001a,03001b", seat.Hero);
            if (!hero) seat.IdentityCard.TurnTo("03001b");
            if (exhausted) seat.IdentityCard.Exhaust();
        }, abilities: runner);
        var source = world.CreateCard("03026", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)));
        runner.ExecuteReveal(world, source, 0);
        runner.Chose(world, AbilityContinuationFixtures.Pending(world, source), Decision.Take(flip ? 0 : 1));
        var removal = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var step = AbilityContinuationFixtures.Pending(world, source);
        Assert.Equal(removable, removal.Affordances.Any(offer => offer.Id == 0));
        if (removable)
        {
            runner.Chose(world, step, Decision.Take(0));
            Assert.False(world.Seats[0].IdentityCard.Ready);
            Assert.Equal("03001b", world.Seats[0].IdentityCard.FaceId);
            Assert.Equal(DeckType.RemovedArea, source.Area.Type);
        }
        else
        {
            string before = world.Digest().Fingerprint();
            Assert.Throws<ArgumentException>(() => runner.Chose(world, step, Decision.Take(0)));
            Assert.Equal(before, world.Digest().Fingerprint());
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(9)]
    public void SyntheticCountIsCappedAndRetainsPhysicalDiscardReceipt(int count)
    {
        // Repository choice: this synthetic action isolates the reusable counted-discard instruction.
        var runner = Runner("01007", "Action", $$$$"""
            {"seq":[{"discardHandCards":{{{{count}}}}},
              {"placeCounters":{"card":"this","counter":"icons",
                "count":{"add":[1,{"printedResourceIconsDiscarded":"game"}]}}}]}
            """);
        var (_, world) = Playing(_ => { }, abilities: runner);
        Hand(world, "01088", 2);
        var source = InPlay(world, "01007");
        var hand = world.Seats[0].Hand.Cards.ToArray();
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        int selected = Math.Min(count, hand.Length);
        if (selected > 0)
        {
            var step = AbilityContinuationFixtures.Pending(world, source);
            var offer = Assert.Single(runner.Choosing(world, step)!.Affordances);
            Assert.False(offer.Targets!.Allows([]));
            runner.Chose(world, step, new Decision(source.ObjectId,
                hand.Take(selected).Select(card => card.ObjectId).ToArray(), []));
        }
        Assert.Equal(1 + selected * 2, source.Tokens["c_icons"]);
        Assert.Equal(hand.Length - selected, world.Seats[0].Hand.Cards.Count);
        Assert.All(hand.Take(selected), card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
    }

}
