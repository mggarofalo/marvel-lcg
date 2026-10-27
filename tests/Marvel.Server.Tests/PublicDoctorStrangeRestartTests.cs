using System.Text.Json;
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

public sealed class PublicDoctorStrangeRestartTests : EngineHostTestBase
{
    // A single bounded twelve-deal harvest serves every public starter journey.
    private static readonly Lazy<(uint Seed, string Top, string Next, bool Master)[]> Candidates = new(() =>
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        return Enumerable.Range(1, 12).Select(value =>
        {
            var game = factory.Create(Specification((uint)value)).Game;
            return ((uint)value, game.State.Seats[0].InvocationDeck!.Deck.Cards[^1].FaceId,
                game.State.Seats[0].InvocationDeck!.Deck.Cards[^2].FaceId,
                game.State.Seats[0].Hand.Cards.Any(card => card.FaceId == "09005"));
        }).ToArray();
    });

    [Fact]
    public void PublicWindsCommitmentAfterFreshHostMatchesUninterruptedDrawAndPhysicalDiscard()
    {
        var candidate = Candidates.Value.First(candidate => candidate.Top == "09036" || candidate.Next == "09036");
        uint seed = candidate.Seed;
        var factory = new PublicDoctorStrangeRestartFixture();
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "strange-winds-restart", Specification(seed)));
        Assert.Null(opened.Error);
        var current = Answer(host, opened, opened, TakeOnly(opened));
        if (candidate.Top != "09036")
            current = Answer(host, opened, current, Decision.Take(Assert.Single(current.Prompt!.Affordances,
                offer => offer.Verb == Game.ActionVerb && offer.AnchorId == factory.Game.State.Seats[0].IdentityCard.ObjectId).Id));
        current = Answer(host, opened, current, Decision.Take(Assert.Single(current.Prompt!.Affordances,
            offer => offer.Verb == Game.ChangeForm).Id));
        var deck = factory.Game.State.Seats[0].InvocationDeck!;
        int top = deck.PublicTop!.ObjectId;
        int discarded = deck.Discard.Cards.Count;
        int hand = Hand(current, 0).Count;
        var fresh = Restart(host, store, factory, opened, current);
        var action = Paid(Assert.Single(current.Prompt!.Affordances,
            offer => offer.AnchorId == factory.Game.State.Seats[0].IdentityCard.ObjectId && offer.Verb == Game.ActionVerb));
        var expected = Answer(host, opened, current, action);
        var actual = Answer(fresh.Host, opened, fresh.Response, action);
        Continued(store, fresh.Store, expected, actual);
        Equivalent(factory, fresh.Factory, expected, actual);
        Assert.Equal(hand + 3, Hand(actual, 0).Count);
        var restoredDeck = fresh.Factory.Game.State.Seats[0].InvocationDeck!;
        Assert.Equal(discarded + 1, restoredDeck.Discard.Cards.Count);
        Assert.Contains(restoredDeck.Discard.Cards, card => card.ObjectId == top);
        Assert.False(fresh.Factory.Game.State.Seats[0].IdentityCard.Ready);
    }

    [Fact]
    [Rule("rr:limit")]
    public void PublicNaturalTalentUseAndNewTopSurviveFreshHostWithoutRestoringThePhaseUse()
    {
        // "Each copy of an ability with such a limit may be used X times per the specified period."
        var factory = new PublicDoctorStrangeRestartFixture();
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "strange-talent-restart", Specification(1)));
        Assert.Null(opened.Error);
        var current = Answer(host, opened, opened, TakeOnly(opened));
        int identity = factory.Game.State.Seats[0].IdentityCard.ObjectId;
        int top = factory.Game.State.Seats[0].InvocationDeck!.PublicTop!.ObjectId;
        current = Answer(host, opened, current, Decision.Take(Assert.Single(current.Prompt!.Affordances,
            offer => offer.AnchorId == identity && offer.Verb == Game.ActionVerb).Id));
        var fresh = Restart(host, store, factory, opened, current);
        Assert.DoesNotContain(fresh.Response.Prompt!.Affordances,
            offer => offer.AnchorId == identity && offer.Verb == Game.ActionVerb);
        Assert.Equal(top, Assert.Single(fresh.Factory.Game.State.Seats[0].InvocationDeck!.Discard.Cards).ObjectId);
        var flip = Decision.Take(Assert.Single(current.Prompt!.Affordances, offer => offer.Verb == Game.ChangeForm).Id);
        var expected = Answer(host, opened, current, flip);
        var actual = Answer(fresh.Host, opened, fresh.Response, flip);
        Continued(store, fresh.Store, expected, actual);
        Equivalent(factory, fresh.Factory, expected, actual);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("ruling:b07d53e12625ab23")]
    public void PublicPaidSpecialChoiceResumesWithExactlyOnceCostsAndExactMastersReturn(bool master)
    {
        var candidate = Candidate(master);
        var factory = new PublicDoctorStrangeRestartFixture();
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "strange-special-restart", Specification(candidate.Seed)));
        Assert.Null(opened.Error);
        var current = Answer(host, opened, opened, TakeOnly(opened));
        if (candidate.Top != "09034")
            current = Answer(host, opened, current, Decision.Take(Assert.Single(current.Prompt!.Affordances,
                offer => offer.Verb == Game.ActionVerb && offer.AnchorId == factory.Game.State.Seats[0].IdentityCard.ObjectId).Id));
        current = Answer(host, opened, current, Decision.Take(Assert.Single(current.Prompt!.Affordances,
            offer => offer.Verb == Game.ChangeForm).Id));
        var world = factory.Game.State;
        int top = world.Seats[0].InvocationDeck!.PublicTop!.ObjectId;
        int next = world.Seats[0].InvocationDeck!.Deck.Cards[^2].ObjectId;
        int source = master ? world.Seats[0].Hand.Cards.First(card => card.FaceId == "09005").ObjectId
            : world.Seats[0].IdentityCard.ObjectId;
        var payment = Paid(Assert.Single(current.Prompt!.Affordances,
            offer => offer.AnchorId == source && (master || offer.Verb == Game.ActionVerb)));
        string beforePayment = StoredSessionJson.Write(Assert.Single(store.Load()));
        var unpaid = host.Exchange(EngineRequest.ResolveGame("unpaid", opened.GameId, opened.Capability!,
            Decision.Take(payment.Affordance, payment.Targets, []), current.Revision));
        Assert.NotNull(unpaid.Error);
        Assert.Equal(beforePayment, StoredSessionJson.Write(Assert.Single(store.Load())));
        current = Answer(host, opened, current, payment);
        world = factory.Game.State;
        Assert.False(factory.Game.IsRootPrompt);
        Assert.Equal(top, world.Seats[0].InvocationDeck!.PublicTop!.ObjectId);
        Assert.Equal(master, world.Seats[0].IdentityCard.Ready);
        var paid = world.Seats[0].Hand.Cards.Select(card => card.ObjectId).ToArray();
        var fresh = Restart(host, store, factory, opened, current);
        var choice = Assert.Single(current.Prompt!.Affordances);
        int[] targets = choice.Targets!.Legal.Take(Math.Max(1, choice.Targets.Min)).ToArray();
        Assert.True(choice.Targets.Allows(targets));
        var answer = Decision.Take(choice.Id, targets, []);
        var expected = Answer(host, opened, current, answer);
        var actual = Answer(fresh.Host, opened, fresh.Response, answer);
        var raw = Assert.Single(fresh.Store.Load()).Save.Units[^1].Decisions[^1].Events
            .Select(happened => JsonSerializer.Deserialize<GameEvent>(happened.GetRawText(), EventJson.Options)!).ToArray();
        AssertLifecycle(raw, top, next, master);
        AssertLifecycle(actual.Events, top, next, master);
        Continued(store, fresh.Store, expected, actual);
        Equivalent(factory, fresh.Factory, expected, actual);
        var restoredWorld = fresh.Factory.Game.State;
        Assert.All(targets, target => Assert.True(Statuses.Has(restoredWorld, restoredWorld.Cards[target], Statuses.Tough)));
        Assert.True(fresh.Factory.Game.IsRootPrompt);
        Assert.Equal(paid, restoredWorld.Seats[0].Hand.Cards.Select(card => card.ObjectId));
        Assert.Equal(master, restoredWorld.Seats[0].IdentityCard.Ready);
        var restoredDeck = restoredWorld.Seats[0].InvocationDeck!;
        Assert.Equal(master, restoredDeck.PublicTop!.ObjectId == top);
        Assert.Equal(5, restoredDeck.Deck.Cards.Concat(restoredDeck.Discard.Cards).Select(card => card.ObjectId).Distinct().Count());
        Assert.Same(master ? restoredDeck.Deck : restoredDeck.Discard, restoredWorld.Cards[top].Area);
        if (master)
        {
            var completed = Restart(fresh.Host, fresh.Store, fresh.Factory, opened, actual);
            var saved = Assert.Single(completed.Store.Load()).Save;
            var replayFactory = new PublicDoctorStrangeRestartFixture();
            var history = SessionReplay.InspectActiveHistory(saved, replayFactory.Compatibility, _ =>
            {
                var replayed = replayFactory.Create(Specification(candidate.Seed));
                return new ReplayOpenedGame(replayed.Game, replayed.SetupEvents);
            });
            var publicHistory = WorldProjection.For(replayFactory.Game.State, null, history[^1].Events, ViewScope.None);
            AssertLifecycle(publicHistory.Events, top, next, master);
            var revealed = Assert.Single(publicHistory.Events.OfType<CardsFlipped>(),
                flip => flip.FaceUp && flip.Cards.Contains(next));
            string face = Assert.Single(revealed.SubjectFaces!).Value;
            Assert.Equal(restoredWorld.Cards[next].FaceId, face);
            Assert.Contains(restoredWorld.Facts.Title(face),
                Assert.Single(EventPresenter.Present([revealed], publicHistory.World)).Summary);
            Assert.DoesNotContain(publicHistory.World.Areas.SelectMany(area => area.Cards), card => card.Id == next);
            Assert.Contains(completed.Response.History!.Entries.SelectMany(entry => entry.Details),
                detail => detail.Contains(restoredWorld.Facts.Title(face), StringComparison.Ordinal)
                    && detail.Contains("face up", StringComparison.Ordinal));
        }
    }

    private static (uint Seed, string Top, string Next, bool Master) Candidate(bool master)
    {
        var matching = Candidates.Value.Where(value => (value.Top == "09034" || value.Next == "09034")
            && (!master || value.Master)).ToArray();
        Assert.True(matching.Length > 0, string.Join("; ", Candidates.Value));
        return matching[0];
    }

    private static GameSpecification Specification(uint seed) => new("rhino", ["doctor_strange"], [], seed);

    private static void AssertLifecycle(IReadOnlyList<GameEvent> events, int top, int next, bool master)
    {
        var ordered = events.ToList();
        int discarded = ordered.FindIndex(happened => happened is CardsMoved moved && moved.Verb == "Discard"
            && moved.Cards.Any(card => card.Card == top));
        Assert.True(discarded >= 0);
        var returns = ordered.OfType<CardsMoved>().Where(moved => moved.Verb == "Return"
            && moved.Cards.Any(card => card.Card == top)).ToArray();
        if (!master)
        {
            Assert.Empty(returns);
            return;
        }
        // "At that point in time, you'd reveal the new top card of the Invocation deck."
        int returned = ordered.IndexOf(Assert.Single(returns));
        Assert.True(returned > discarded);
        var revealed = Assert.Single(ordered.Skip(discarded + 1).Take(returned - discarded - 1)
            .OfType<CardsFlipped>(), flip => flip.FaceUp);
        Assert.Equal([next], revealed.Cards);
    }
}
