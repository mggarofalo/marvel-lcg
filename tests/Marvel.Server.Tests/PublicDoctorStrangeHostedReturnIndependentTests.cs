using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Session;
using Marvel.View;
using Xunit;
using static Marvel.Server.Tests.PublicDoctorStrangeRestartFixture;

namespace Marvel.Server.Tests;

public sealed class PublicDoctorStrangeHostedReturnIndependentTests
{
    [Fact]
    public void HostedCheckpointRequiresNaturallyDrawnPaidAstralAndDistinctRestartBranches()
    {
        var factory = new PublicDoctorStrangeRestartFixture();
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        var opened = host.Exchange(EngineRequest.OpenGame("open", "hosted-independent",
            new GameSpecification("rhino", ["captain_marvel", "doctor_strange"], [], 7)));
        Assert.Null(opened.Error);
        var current = opened;
        for (int index = 0; index < 14; index++)
            current = Answer(host, opened, current, PublicStrangeHostedJourney.Choose(factory.Game, current.Prompt!));
        Assert.Equal(14, Assert.Single(store.Load()).Save.Units.Sum(unit => unit.Decisions.Count));
        var scheme = factory.Game.State.Cards.Single(card => card.FaceId == "09029");
        var hosted = Assert.Single(BeneathCards.Of(factory.Game.State, scheme));
        int physical = hosted.ObjectId;
        int incarnation = hosted.Incarnation;
        Assert.Equal(1, hosted.Owner);
        Assert.Equal(6, scheme.Tokens["k_threat"]);
        AssertOnlyPublicInvocationFace(factory.Game);
        var fresh = Restart(host, store, factory, opened, current);
        Assert.NotSame(host, fresh.Host);
        Assert.NotSame(store, fresh.Store);
        Assert.NotSame(factory, fresh.Factory);
        AssertOnlyPublicInvocationFace(fresh.Factory.Game);
        var actual = fresh.Response;
        int astralPlayed = 0;
        int astralPhysical = -1;
        var astralResults = new List<GameEvent>();
        for (int index = 0; index < 40 && PublicStrangeHostedJourney.HasStored(factory.Game); index++)
        {
            var answer = PublicStrangeHostedJourney.Choose(factory.Game, current.Prompt!);
            var offer = current.Prompt!.Affordances.FirstOrDefault(item => item.Id == answer.Affordance);
            bool astral = IsAstralPlay(factory.Game, offer);
            if (astral)
            {
                // 09003: "Choose a scheme → remove 3 threat from that scheme"; printed cost 2.
                Assert.Equal(1, current.Prompt.Player);
                Assert.Contains(factory.Game.State.Seats[1].Hand.Cards, card => card.ObjectId == offer!.AnchorId);
                Assert.Equal([scheme.ObjectId], answer.Targets);
                Assert.NotEmpty(answer.Spent);
                string baseline = StoredSessionJson.Write(Assert.Single(fresh.Store.Load()));
                var rejected = fresh.Host.Exchange(EngineRequest.ResolveGame("unpaid-astral", opened.GameId,
                    opened.Capability!, Decision.Take(answer.Affordance, answer.Targets, []), actual.Revision));
                Assert.NotNull(rejected.Error);
                Assert.Equal(baseline, StoredSessionJson.Write(Assert.Single(fresh.Store.Load())));
                astralPlayed++;
                astralPhysical = offer!.AnchorId;
            }
            current = Answer(host, opened, current, answer);
            actual = Answer(fresh.Host, opened, actual, answer);
            Continued(store, fresh.Store, current, actual);
            Equivalent(factory, fresh.Factory, current, actual);
            Assert.Equal(StoredSessionJson.Write(Assert.Single(store.Load())),
                StoredSessionJson.Write(Assert.Single(fresh.Store.Load())));
            astralResults.AddRange(Assert.Single(fresh.Store.Load()).Save.Units[^1].Decisions[^1].Events
                .Select(item => JsonSerializer.Deserialize<GameEvent>(item.GetRawText(), EventJson.Options)!));
        }
        Assert.Equal(1, astralPlayed);
        var removal = Assert.Single(astralResults.OfType<FieldSet>(), changed =>
            changed.Card == scheme.ObjectId && changed.Field == "k_threat" && changed.Verb == "Remove_Threat");
        Assert.Equal(6, removal.From);
        Assert.Equal(3, removal.To);
        Assert.Equal(EventCauseKind.Thwart, removal.Cause!.Kind);
        Assert.Equal(factory.Game.State.Seats[1].IdentityCard.ObjectId, removal.Cause.Source);
        Assert.Contains(astralResults.OfType<CardsMoved>(), moved => moved.Verb == "Discard"
            && moved.To.Owner == 1 && moved.Cards.Any(card => card.Card == astralPhysical));
        // 09029: "Shuffle the Invocation card under here into the Invocation deck."
        Assert.False(PublicStrangeHostedJourney.HasStored(factory.Game));
        var returned = fresh.Factory.Game.State.Cards[physical];
        Assert.Equal(1, returned.Owner);
        Assert.Equal(incarnation, returned.Incarnation);
        Assert.Same(fresh.Factory.Game.State.Seats[1].InvocationDeck!.Deck, returned.Area);
        AssertOnlyPublicInvocationFace(fresh.Factory.Game);
        var completed = Restart(fresh.Host, fresh.Store, fresh.Factory, opened, actual);
        Assert.NotSame(fresh.Host, completed.Host);
        Assert.Equal(1, completed.Factory.Game.State.Cards[physical].Owner);
        Assert.Equal(incarnation, completed.Factory.Game.State.Cards[physical].Incarnation);
        Assert.Same(completed.Factory.Game.State.Seats[1].InvocationDeck!.Deck,
            completed.Factory.Game.State.Cards[physical].Area);
        AssertOnlyPublicInvocationFace(completed.Factory.Game);
    }

    private static bool IsAstralPlay(Game game, Marvel.Rules.Prompts.Affordance? offer) =>
        game.IsRootPrompt && offer is not null && offer.AnchorId >= 0
        && offer.Verb is "Play" or Game.ActionVerb && game.State.Cards[offer.AnchorId].FaceId == "09003";

    private static void AssertOnlyPublicInvocationFace(Game game)
    {
        var deck = game.State.Seats[1].InvocationDeck!;
        foreach (var scope in new[] { ViewScope.None, new RestrictedVisibilityPolicy(0).Authorize(null, 2),
                     new RestrictedVisibilityPolicy(1).Authorize(null, 2) })
        {
            var view = WorldProjection.For(game.State, game.Pending, [], scope).World;
            var pile = Assert.Single(view.Areas, area => area.Id == deck.Deck.Id);
            var publicCard = Assert.Single(pile.Cards, card => card.Face is not null);
            Assert.Equal(deck.PublicTop!.ObjectId, publicCard.Id);
            Assert.Equal(deck.PublicTop.FaceId, publicCard.Face!.Id);
            Assert.All(pile.Cards.Where(card => card != publicCard), card =>
            {
                Assert.Null(card.Id);
                Assert.Null(card.Face);
            });
        }
    }
}
