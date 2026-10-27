using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Session;
using Xunit;
using static Marvel.Server.Tests.PublicDoctorStrangeRestartFixture;

namespace Marvel.Server.Tests;

public sealed class PublicDoctorStrangeHostedReturnTests
{
    [Fact]
    public void NonfirstStrangesHostedInvocationReturnsAcrossFreshHostWithExactLifecycle()
    {
        var factory = new PublicDoctorStrangeRestartFixture();
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "hosted",
            new GameSpecification("rhino", ["captain_marvel", "doctor_strange"], [], 7)));
        Assert.Null(opened.Error);
        var encounter = factory.Game.State.Areas.Single(area => area.Type == DeckType.EncounterDeck);
        Assert.Equal("01190", encounter.Cards[^4].FaceId);
        var current = opened;
        for (int index = 0; index < 2; index++)
            current = Answer(host, opened, current, PublicStrangeHostedJourney.Choose(factory.Game, current.Prompt!));
        var originalTop = factory.Game.State.Seats[1].InvocationDeck!.PublicTop!;
        int physical = originalTop.ObjectId;
        int incarnation = originalTop.Incarnation;
        for (int index = 0; index < 24 && !PublicStrangeHostedJourney.HasStored(factory.Game); index++)
            current = Answer(host, opened, current, PublicStrangeHostedJourney.Choose(factory.Game, current.Prompt!));
        var scheme = factory.Game.State.Cards.Single(card => card.FaceId == "09029");
        var stored = Assert.Single(BeneathCards.Of(factory.Game.State, scheme));
        Assert.Equal(physical, stored.ObjectId);
        Assert.Equal(incarnation, stored.Incarnation);
        Assert.Equal(1, stored.Owner);
        Assert.False(stored.FaceUp);
        Assert.Equal(6, scheme.Tokens["k_threat"]);
        PublicStrangeHostedJourney.AssertPrivacy(factory.Game, true, scheme.ObjectId);
        var fresh = Restart(host, store, factory, opened, current);
        Assert.Equal(physical, Assert.Single(BeneathCards.Of(fresh.Factory.Game.State,
            fresh.Factory.Game.State.Cards[scheme.ObjectId])).ObjectId);
        PublicStrangeHostedJourney.AssertPrivacy(fresh.Factory.Game, true, scheme.ObjectId);
        var actual = fresh.Response;
        var raw = new List<GameEvent>();
        for (int index = 0; index < 40 && PublicStrangeHostedJourney.HasStored(factory.Game); index++)
        {
            var answer = PublicStrangeHostedJourney.Choose(factory.Game, current.Prompt!);
            var attempted = host.Exchange(EngineRequest.ResolveGame("answer-" + current.Revision, opened.GameId, opened.Capability!, answer, current.Revision));
            Assert.True(attempted.Error is null, JsonSerializer.Serialize(new { current.Prompt, answer, attempted.Error }));
            current = attempted;
            actual = Answer(fresh.Host, opened, actual, answer);
            Continued(store, fresh.Store, current, actual);
            Equivalent(factory, fresh.Factory, current, actual);
            raw.AddRange(Assert.Single(fresh.Store.Load()).Save.Units[^1].Decisions[^1].Events
                .Select(item => JsonSerializer.Deserialize<GameEvent>(item.GetRawText(), EventJson.Options)!));
        }
        Assert.False(PublicStrangeHostedJourney.HasStored(factory.Game));
        var world = fresh.Factory.Game.State;
        var deck = world.Seats[1].InvocationDeck!;
        var returned = world.Cards[physical];
        Assert.Same(deck.Deck, returned.Area);
        Assert.Equal(incarnation, returned.Incarnation);
        Assert.Equal(1, returned.Owner);
        Assert.Equal(5, deck.Deck.Cards.Count);
        Assert.Empty(deck.Discard.Cards);
        Assert.Equal(5, deck.Deck.Cards.Select(card => card.ObjectId).Distinct().Count());
        Assert.Equal(DeckType.EncounterDiscardPile, world.Cards[scheme.ObjectId].Area.Type);
        AssertLifecycle(raw, physical, scheme.ObjectId, deck.PublicTop!.ObjectId);
        PublicStrangeHostedJourney.AssertPrivacy(fresh.Factory.Game, false, scheme.ObjectId);
        var completed = Restart(fresh.Host, fresh.Store, fresh.Factory, opened, actual);
        PublicStrangeHostedJourney.AssertPrivacy(completed.Factory.Game, false, scheme.ObjectId);
    }

    private static void AssertLifecycle(List<GameEvent> events, int physical, int scheme, int top)
    {
        // 09029: "When Defeated: Shuffle the Invocation card under here into the Invocation deck."
        var movement = Assert.Single(events.OfType<CardsMoved>(), moved => moved.Verb == "Shuffle_Into"
            && moved.Cards.Any(card => card.Card == physical));
        Assert.Equal("BeneathCardsPile", movement.From.Zone);
        Assert.Equal(scheme, movement.From.Host);
        Assert.Equal("AdditionalDeck", movement.To.Zone);
        Assert.Equal(1, movement.To.Owner);
        int move = events.IndexOf(movement);
        int detach = events.FindIndex(item => item is CardDetached detached && detached.Card == physical && detached.Host == scheme);
        int shuffle = events.FindIndex(item => item is AreaReordered reordered && reordered.Area.Zone == "AdditionalDeck");
        int reveal = events.FindIndex(item => item is CardsFlipped flipped && flipped.FaceUp && flipped.Cards.Contains(top));
        int departure = events.FindIndex(item => item is CardsMoved moved && moved.Cards.Any(card => card.Card == scheme)
            && moved.To.Zone == "EncounterDiscardPile");
        Assert.True(move >= 0);
        Assert.True(move < detach);
        Assert.True(detach < shuffle);
        Assert.True(shuffle < reveal);
        Assert.True(reveal < departure);
        Assert.Equal(5, Assert.IsType<AreaReordered>(events[shuffle]).Order.Distinct().Count());
    }
}

