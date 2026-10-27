using Marvel.Protocol;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicDoctorStrangeAdmissionIndependentTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void FourSeatCustomDeckKeepsEveryInvocationWithItsChosenOwner(int owner)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var catalog = factory.DiscoverDecks();
        var strange = catalog.Heroes.Single(hero => hero.Key == "doctor_strange");
        string[] custom = [.. strange.Starter.SkipLast(1), "01077"];
        Assert.True(factory.AssessDeck(strange.Key, custom).IsValid);
        var heroes = new List<string> { "spider_man", "captain_marvel", "hawkeye" };
        heroes.Insert(owner, strange.Key);
        var decks = heroes.Select(key => new PlayerDeckRecipe(key == strange.Key
            ? custom : catalog.Heroes.Single(hero => hero.Key == key).Starter)).ToArray();
        var specification = new GameSpecification("rhino_expert", heroes, [], 69, decks);
        var opened = new EngineHost(factory).Exchange(EngineRequest.OpenGame("open", "owned-custom", specification));
        Assert.Null(opened.Error);
        var projected = Assert.Single(opened.World!.InvocationDecks);
        Assert.Equal(owner, projected.Owner);
        Assert.Equal(owner, opened.World.Areas.Single(area => area.Id == projected.Deck).Owner);
        Assert.Equal(owner, opened.World.Areas.Single(area => area.Id == projected.Discard).Owner);

        var game = factory.Create(specification).Game;
        for (int seat = 0; seat < heroes.Count; seat++)
        {
            var player = game.State.Seats[seat];
            Assert.Equal(decks[seat].Cards.Order(StringComparer.Ordinal),
                player.Hand.Cards.Concat(player.Deck.Cards).Select(card => card.FaceId).Order(StringComparer.Ordinal));
            if (seat != owner) Assert.Null(player.InvocationDeck);
        }
        var invocation = game.State.Seats[owner].InvocationDeck!;
        Assert.Equal(["09032", "09033", "09034", "09035", "09036"],
            invocation.Deck.Cards.Select(card => card.FaceId).Order(StringComparer.Ordinal));
        Assert.All(invocation.Deck.Cards, card => Assert.Equal(owner, card.Owner));
        Assert.Empty(invocation.Discard.Cards);
    }

    [Theory]
    [InlineData("09032")]
    [InlineData("09033")]
    [InlineData("09034")]
    [InlineData("09035")]
    [InlineData("09036")]
    public void FortyCardInvocationSubstitutionCannotPublishASession(string face)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var hero = factory.DiscoverDecks().Heroes.Single(item => item.Key == "doctor_strange");
        // The admitted forty-card starter is the control; only its final Basic card changes.
        Assert.True(factory.AssessDeck(hero.Key, hero.Starter).IsValid);
        string[] injected = [.. hero.Starter.SkipLast(1), face];
        Assert.Equal(40, injected.Length);
        var assessment = factory.AssessDeck(hero.Key, injected);
        Assert.False(assessment.CanExtend);
        Assert.Contains(assessment.Diagnostics, item => item.Code == "unsupported-classification" && item.Face == face);
        var quantities = factory.DeckQuantities(hero.Key, [.. hero.Signature, face]);
        Assert.Equal([0], quantities[face]);
        var specification = new GameSpecification("rhino", [hero.Key], [], 3, [new(injected)]);
        Assert.Throws<ArgumentException>(() => factory.Create(specification));
        var store = new MemorySessionStore();
        var opened = new EngineHost(factory, store: store).Exchange(
            EngineRequest.OpenGame("injected", "injected-invocation", specification));
        Assert.NotNull(opened.Error);
        Assert.Null(opened.Capability);
        Assert.Null(opened.World);
        Assert.Empty(store.Load());
    }
}
