using System.Text.Json;
using System.Text.Json.Nodes;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RecipientCommitmentTests : ChoosingCardsTestBase
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(2, 1)]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:initiating-abilities.step.7")]
    public void ExplicitMansionRecipientPaysBeforeDrawingWithoutASecondPrompt(int players, int recipient)
    {
        // "Pay the cost(s)." precedes "the ability ... resolves."
        var (world, runner, source) = Board(players, "01091");
        var ability = new PendingAbility(source.ObjectId, AbilityType.Action, 0);
        var offer = runner.Describe(world, ability);
        Assert.True(offer.DirectRecipientCommit);
        Assert.Equal(players, offer.Targets!.Legal.Count);
        Assert.Contains("Exhaust Avengers Mansion", offer.Targets.Details![world.Seats[recipient].IdentityCard.ObjectId]);
        int[] before = [.. world.Seats.Select(seat => seat.Hand.Cards.Count)];
        var events = runner.Act(world, ability, [], [world.Seats[recipient].IdentityCard.ObjectId]);
        Assert.False(source.Ready);
        Assert.Equal("Exhaust", events[0].Verb);
        Assert.Contains(events.Skip(1), entry => entry.Verb == "Draw");
        Assert.Empty(world.Agenda.Outstanding);
        for (int seat = 0; seat < players; seat++)
            Assert.Equal(before[seat] + (seat == recipient ? 1 : 0), world.Seats[seat].Hand.Cards.Count);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    public void ExplicitHelicarrierRecipientChangesOnlyTheirNextCardCost(int players, int recipient)
    {
        var (world, runner, source) = Board(players, "01092");
        Card[] cards = [.. world.Seats.Select(seat => world.CreateCard("01083", seat.Hand))];
        runner.Act(world, new PendingAbility(source.ObjectId, AbilityType.Action, 0), [],
            [world.Seats[recipient].IdentityCard.ObjectId]);
        Assert.False(source.Ready);
        Assert.Empty(world.Agenda.Outstanding);
        for (int seat = 0; seat < players; seat++)
            Assert.Equal(seat == recipient ? 2 : 3, CardPayment.CostOf(world, Cards, world.Seats[seat], cards[seat]).Amount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidOrRepeatedRecipientRejectsBeforeExhaustingOrDrawing(bool repeated)
    {
        var (world, runner, source) = Board(2, "01091");
        int identity = world.Seats[0].IdentityCard.ObjectId;
        var digest = world.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() => runner.Act(world,
            new PendingAbility(source.ObjectId, AbilityType.Action, 0), [],
            repeated ? [identity, identity] : [source.ObjectId]));
        Assert.True(source.Ready);
        Assert.Equal(digest, world.Digest().Canonical());
    }

    [Fact]
    public void EmptyRecipientRetainsTheSeparateChoiceForDurableAnswers()
    {
        var (world, runner, source) = Board(2, "01091");
        runner.Act(world, new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], []);
        Assert.False(source.Ready);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(2, runner.Choosing(world, source, 0, waiting.Index, waiting.Tier)!.Affordances.Count);
    }

    [Fact]
    public void DirectRecipientKeepsTheCostAndEffectEventOrderOfTheSeparateChoice()
    {
        var (direct, runner, source) = Board(2, "01091");
        var expected = runner.Act(direct, new PendingAbility(source.ObjectId, AbilityType.Action, 0), [],
            [direct.Seats[1].IdentityCard.ObjectId]);
        var (deferred, other, sameSource) = Board(2, "01091");
        var cost = other.Act(deferred, new PendingAbility(sameSource.ObjectId, AbilityType.Action, 0), [], []);
        var waiting = Assert.Single(deferred.Agenda.Outstanding);
        var effect = other.Chose(deferred, sameSource, 0, waiting.Index,
            Decision.Take(deferred.Seats[1].IdentityCard.ObjectId), waiting.Tier);
        // Instruction ids are response-local: the direct route includes the cost
        // in its response; the separate effect begins a new response at offset zero.
        Assert.Equal(expected.Select(SemanticEvent), cost.Concat(effect).Select(SemanticEvent));
        Assert.Equal(direct.Digest().Canonical(), deferred.Digest().Canonical());
    }

    [Fact]
    public void IdentityActionsDoNotAcquireTheSupportCommitmentShortcut()
    {
        World world = Deal("captain_marvel");
        var runner = AuthoredCards.Runner();
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == world.Seats[0].IdentityCard.ObjectId);
        Assert.False(runner.Describe(world, action).DirectRecipientCommit);
    }

    private static string SemanticEvent(GameEvent entry)
    {
        JsonObject value = JsonSerializer.SerializeToNode(entry)!.AsObject();
        value.Remove("ActivityBatch");
        value.Remove("Batch");
        return value.ToJsonString();
    }

    private static (World World, AbilityRunner Runner, Card Source) Board(int players, string face)
    {
        World world = players == 1 ? Deal("spider_man") : Deal("spider_man", "captain_marvel");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Card source = world.CreateCard(face, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        return (world, runner, source);
    }
}
