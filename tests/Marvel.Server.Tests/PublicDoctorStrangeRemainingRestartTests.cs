using System.Text.Json;
using Marvel.Core.Random;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Marvel.View;
using Xunit;
using static Marvel.Server.Tests.PublicDoctorStrangeRestartFixture;

namespace Marvel.Server.Tests;

public sealed class PublicDoctorStrangeRemainingRestartTests
{
    [Fact]
    public void PublicUnpaidSpellMasteryRejectsAtomicallyAndLeavesTheValidCommitmentAvailable()
    {
        // The session contract rejects an incomplete commitment before changing live or durable state.
        var journey = new PublicStrangeCheckpointJourney(2);
        journey.Reach((game, _) => game.IsRootPrompt && game.Phase == GamePhase.PlayerTurn
            && game.State.Seats[0].IdentityCard.FaceId == "09001a"
            && game.State.Seats[0].InvocationDeck!.PublicTop!.FaceId is "09032" or "09033" or "09034"
            && game.State.Seats[0].IdentityCard.Ready);
        var offer = Assert.Single(journey.Current.Prompt!.Affordances,
            item => item.AnchorId == journey.Factory.Game.State.Seats[0].IdentityCard.ObjectId
                && item.Verb == Game.ActionVerb);
        var valid = Paid(offer);
        Assert.NotEmpty(valid.Spent);
        string baseline = Snapshot(journey);
        var fresh = Restart(journey.Host, journey.Store, journey.Factory, journey.Opened, journey.Current);
        var rejected = journey.Host.Exchange(EngineRequest.ResolveGame("unpaid-review", journey.Opened.GameId,
            journey.Opened.Capability!, Decision.Take(offer.Id, valid.Targets, []), journey.Current.Revision));
        Assert.NotNull(rejected.Error);
        Assert.Equal(baseline, Snapshot(journey));
        var sync = journey.Host.Exchange(EngineRequest.SyncGame("after-unpaid-review", journey.Opened.GameId,
            journey.Opened.Capability!));
        Assert.Null(sync.Error);
        Equivalent(journey.Factory, fresh.Factory, sync, fresh.Response);
        Assert.Equal(baseline, Snapshot(journey));
        var expected = Answer(fresh.Host, journey.Opened, fresh.Response, valid);
        var actual = Answer(journey.Host, journey.Opened, sync, valid);
        for (int index = 0; index < 4 && !journey.Factory.Game.IsRootPrompt; index++)
        {
            var next = PublicStrangeCheckpointJourney.Choose(actual.Prompt!, journey.Factory.Game);
            expected = Answer(fresh.Host, journey.Opened, expected, next);
            actual = Answer(journey.Host, journey.Opened, actual, next);
        }
        Continued(fresh.Store, journey.Store, expected, actual);
        Equivalent(fresh.Factory, journey.Factory, expected, actual);
        Assert.True(journey.Factory.Game.IsRootPrompt);
        Assert.False(journey.Factory.Game.State.Seats[0].IdentityCard.Ready);
    }

    [Fact]
    [Rule("rr:status-cards.1")]
    public void PublicVaporsReplacementResumesTheSelectedPhysicalStatusAndRecyclesTheFinalInvocation()
    {
        // "A character cannot have more than one status card of each type at a time."
        var journey = new PublicStrangeCheckpointJourney(2);
        int selectedStatus = -1;
        journey.Reach((game, response) =>
        {
            if (response.Prompt!.Label == "09035: choose a card")
                selectedStatus = response.Prompt.Affordances[0].AnchorId;
            return response.Prompt.Affordances.Any(offer => offer.Description == "Replace with tough");
        });
        var world = journey.Factory.Game.State;
        var old = world.Cards[selectedStatus];
        Assert.Equal(Statuses.Confused, old.FaceId);
        int host = old.Area.Host;
        int incarnation = world.Seats[0].InvocationDeck!.PublicTop!.Incarnation;
        int invocation = world.Seats[0].InvocationDeck!.PublicTop!.ObjectId;
        Assert.Equal("09035", world.Cards[invocation].FaceId);
        Assert.Single(world.Seats[0].InvocationDeck!.Deck.Cards);
        Assert.False(world.Seats[0].IdentityCard.Ready);
        int[] hand = world.Seats[0].Hand.Cards.Select(card => card.ObjectId).ToArray();
        int[] otherStatuses = world.Areas.Where(area => area.Type == DeckType.StatusArea)
            .SelectMany(area => area.Cards).Where(card => card.ObjectId != selectedStatus).Select(card => card.ObjectId).ToArray();
        var fresh = Restart(journey.Host, journey.Store, journey.Factory, journey.Opened, journey.Current);
        Assert.NotSame(journey.Host, fresh.Host);
        Assert.NotSame(journey.Factory.Game, fresh.Factory.Game);
        Assert.Equal(host, fresh.Factory.Game.State.Cards[selectedStatus].Area.Host);
        var answer = Decision.Take(Assert.Single(journey.Current.Prompt!.Affordances,
            offer => offer.Description == "Replace with tough").Id);
        var expected = Answer(journey.Host, journey.Opened, journey.Current, answer);
        var actual = Answer(fresh.Host, journey.Opened, fresh.Response, answer);
        Continued(journey.Store, fresh.Store, expected, actual);
        Equivalent(journey.Factory, fresh.Factory, expected, actual);
        var restored = fresh.Factory.Game.State;
        Assert.Equal(DeckType.RemovedArea, restored.Cards[selectedStatus].Area.Type);
        Assert.True(Statuses.Has(restored, restored.Cards[host], Statuses.Tough));
        Assert.False(Statuses.Has(restored, restored.Cards[host], Statuses.Confused));
        Assert.All(otherStatuses, id => Assert.Equal(DeckType.StatusArea, restored.Cards[id].Area.Type));
        Assert.Contains(actual.Events.OfType<CardAttached>(), attached => attached.Host == host && attached.Verb == "Replace_Status");
        Assert.Equal(hand, restored.Seats[0].Hand.Cards.Select(card => card.ObjectId));
        Assert.False(restored.Seats[0].IdentityCard.Ready);
        Assert.Equal(incarnation, restored.Cards[invocation].Incarnation);
        Assert.Equal(0, restored.Cards[invocation].Owner);
        Assert.Equal(5, restored.Seats[0].InvocationDeck!.Deck.Cards.Count);
        Assert.Empty(restored.Seats[0].InvocationDeck!.Discard.Cards);
        Assert.True(fresh.Factory.Game.IsRootPrompt);
        AssertPrivacy(restored);
    }

    [Fact]
    public void PublicFinalWindsFreshHostPreservesExactRecycleOrderRngAndHasNoPlayerDeckPenalty()
    {
        // mc08 Invocation deck: "There is no penalty for doing this."
        var journey = new PublicStrangeCheckpointJourney(9);
        journey.Reach((game, _) => game.IsRootPrompt && game.Phase == GamePhase.PlayerTurn
            && game.State.Seats[0].InvocationDeck!.Deck.Cards.Count == 1
            && game.State.Seats[0].InvocationDeck!.PublicTop!.FaceId == "09036"
            && game.State.Seats[0].IdentityCard.Ready);
        var world = journey.Factory.Game.State;
        var deck = world.Seats[0].InvocationDeck!;
        var top = deck.PublicTop!;
        int originalIncarnation = top.Incarnation;
        var shuffled = deck.Discard.Cards.Select(card => card.ObjectId).Append(top.ObjectId).ToList();
        long randomBefore = world.Random.Generator.WordsConsumed;
        var random = new EngineRandom(0);
        random.Generator.SetState(world.Random.Generator.GetState());
        random.Shuffle(shuffled);
        Assert.True(world.Seats[0].Deck.Cards.Count > 3);
        int[] playerDeck = world.Seats[0].Deck.Cards.Select(card => card.ObjectId).ToArray();
        int[] hand = world.Seats[0].Hand.Cards.Select(card => card.ObjectId).ToArray();
        int[] dealt = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Select(card => card.ObjectId).ToArray();
        string baseline = Snapshot(journey);
        var fresh = Restart(journey.Host, journey.Store, journey.Factory, journey.Opened, journey.Current);
        Assert.NotSame(journey.Host, fresh.Host);
        Assert.NotSame(journey.Factory.Game, fresh.Factory.Game);
        for (int index = 0; index < 3; index++)
        {
            var sync = journey.Host.Exchange(EngineRequest.SyncGame("query-" + index, journey.Opened.GameId, journey.Opened.Capability!));
            Assert.Null(sync.Error);
            AssertPrivacy(world);
            Assert.Equal(baseline, Snapshot(journey));
        }
        var answer = PublicStrangeCheckpointJourney.Choose(journey.Current.Prompt!, journey.Factory.Game);
        var expected = Answer(journey.Host, journey.Opened, journey.Current, answer);
        var actual = Answer(fresh.Host, journey.Opened, fresh.Response, answer);
        Continued(journey.Store, fresh.Store, expected, actual);
        Equivalent(journey.Factory, fresh.Factory, expected, actual);
        var restored = fresh.Factory.Game.State;
        var recycled = restored.Seats[0].InvocationDeck!;
        Assert.Equal(shuffled, recycled.Deck.Cards.Select(card => card.ObjectId));
        Assert.Empty(recycled.Discard.Cards);
        Assert.Single(recycled.Deck.Cards, card => card.FaceUp);
        Assert.Equal(shuffled[^1], recycled.PublicTop!.ObjectId);
        Assert.Equal(originalIncarnation, restored.Cards[top.ObjectId].Incarnation);
        Assert.All(recycled.Deck.Cards, card => Assert.Equal(0, card.Owner));
        Assert.Equal(JsonSerializer.Serialize(random.Generator.GetState()), JsonSerializer.Serialize(restored.Random.Generator.GetState()));
        Assert.Equal(randomBefore + random.Generator.WordsConsumed, restored.Random.Generator.WordsConsumed);
        Assert.Equal(playerDeck.SkipLast(3), restored.Seats[0].Deck.Cards.Select(card => card.ObjectId));
        Assert.Equal(hand.Concat(playerDeck.TakeLast(3).Reverse()).Order(), restored.Seats[0].Hand.Cards.Select(card => card.ObjectId).Order());
        Assert.Equal(dealt, restored.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Select(card => card.ObjectId));
        var raw = Assert.Single(fresh.Store.Load()).Save.Units[^1].Decisions[^1].Events
            .Select(item => JsonSerializer.Deserialize<GameEvent>(item.GetRawText(), EventJson.Options)!).ToArray();
        Assert.Equal(shuffled, Assert.Single(raw.OfType<AreaReordered>()).Order);
        AssertPrivacy(restored);
    }

    private static string Snapshot(PublicStrangeCheckpointJourney journey)
    {
        var game = journey.Factory.Game;
        return JsonSerializer.Serialize(new { Digest = game.State.Digest().Canonical(),
            Rng = game.State.Random.Generator.GetState(), game.State.Random.Generator.WordsConsumed,
            Agenda = game.State.Agenda.Outstanding.Select(step => step.ToString()).ToArray(),
            Prompt = PromptRecord.From(game.Pending!), Save = StoredSessionJson.Write(Assert.Single(journey.Store.Load())) });
    }

    private static void AssertPrivacy(World world)
    {
        var projected = WorldProjection.For(world, null, [], ViewScope.None).World;
        var invocation = Assert.Single(projected.InvocationDecks);
        var pile = Assert.Single(projected.Areas, area => area.Id == invocation.Deck);
        Assert.Single(pile.Cards, card => card.Id is not null);
        Assert.Equal(world.Seats[0].InvocationDeck!.PublicTop!.ObjectId, Assert.Single(pile.Cards, card => card.Id is not null).Id);
        Assert.All(pile.Cards.Where(card => card.Id is null), card => Assert.Null(card.Face));
        Assert.All(projected.Areas.Where(area => area.Zone == "HandsArea").SelectMany(area => area.Cards), card => Assert.Null(card.Face));
    }
}

