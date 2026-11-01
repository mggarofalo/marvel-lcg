using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicDoctorStrangeSetupTests
{
    private static readonly string[] OtherHeroes = ["spider_man", "captain_marvel", "hawkeye"];
    [Theory]
    [InlineData("rhino")]
    [InlineData("rhino_expert")]
    [InlineData("absorbing_man")]
    [InlineData("absorbing_man_expert")]
    public void ExactStarterAndOwnedInvocationDeckReachPlayAtOneThroughFourSeats(string scenario)
    {
        // Pinned mc08 divider: 15 signature + 17 Protection + 8 Basic, with
        // obligation, five nemesis cards and five Invocations outside those forty.
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var catalog = factory.DiscoverDecks();
        var hero = catalog.Heroes.Single(hero => hero.Key == "doctor_strange");
        Assert.Equal(18, catalog.Heroes.Count);
        Assert.Equal(40, hero.Starter.Count);
        Assert.Equal(15, hero.Signature.Count);
        Assert.True(factory.AssessDeck(hero.Key, hero.Starter).IsValid);
        foreach (int count in Enumerable.Range(1, 4))
        {
            string[] heroes = [.. OtherHeroes.Take(count - 1), hero.Key];
            var specification = new GameSpecification(scenario, heroes, null, 693);
            var opened = new EngineHost(factory).Exchange(EngineRequest.OpenGame("open", "strange-setup", specification));
            Assert.Null(opened.Error);
            Assert.NotNull(opened.Capability);
            Assert.Equal(count - 1, Assert.Single(opened.World!.InvocationDecks).Owner);
            var game = factory.Create(specification).Game;
            Assert.Equal(GamePhase.Mulligan, game.Phase);
            var seat = game.State.Seats[count - 1];
            Assert.Equal("09001b", seat.IdentityCard.FaceId);
            Assert.Equal(hero.Starter.Order(StringComparer.Ordinal), seat.Deck.Cards.Concat(seat.Hand.Cards)
                .Select(card => card.FaceId).Order(StringComparer.Ordinal));
            Assert.Single(game.State.Cards, card => card.FaceId == "09027");
            Assert.Equal(5, game.State.Cards.Count(card => card.FaceId is "09028" or "09029" or "09030" or "09031"));
            var invocation = Assert.IsType<InvocationDeck>(seat.InvocationDeck);
            Assert.Equal(5, invocation.Deck.Cards.Count);
            Assert.Empty(invocation.Discard.Cards);
            Assert.DoesNotContain(game.State.Seats.SelectMany(player => player.Deck.Cards.Concat(player.Hand.Cards)),
                card => card.FaceId is "09032" or "09033" or "09034" or "09035" or "09036");
            for (int index = 0; index < count; index++)
            {
                Assert.Null(invocation.PublicTop);
                game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
            }
            Assert.Equal(GamePhase.PlayerTurn, game.Phase);
            Assert.Single(invocation.Deck.Cards, card => card.FaceUp);
            Assert.Same(invocation.Deck.Cards[^1], invocation.PublicTop);
        }
    }
}
