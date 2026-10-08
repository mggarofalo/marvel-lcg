using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class DeckReturnPresentationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void PublicReceiptSurvivesConcealmentWithoutReintroducingIdentities(int viewer)
    {
        // Synthetic public occurrence-time receipt; the resulting deck stays concealed.
        var cards = Marvel.Content.CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var state = new World(cards, players: 2);
        state.CreateSeat("Black Panther");
        state.CreateSeat("Spider-Man");
        Card hidden = state.CreateCard("01044", state.Seats[0].Deck);
        var receipt = new CardsShuffledIntoDeck(0, 1, ["Vibranium"])
        { Subjects = new Dictionary<int, string> { [hidden.ObjectId] = "Vibranium" } };
        ViewScope scope = new RestrictedVisibilityPolicy(viewer).Authorize(null, state.Players);
        VisibleResult result = WorldProjection.For(state, null, [receipt], scope);
        CardsShuffledIntoDeck safe = Assert.IsType<CardsShuffledIntoDeck>(Assert.Single(result.Events));
        Assert.Null(safe.Subjects);
        Assert.All(result.World.Areas.SelectMany(area => area.Cards), card => Assert.Null(card.Id));
        EventPresentation shown = EventPresenter.Present(safe, result.World);
        Assert.Equal("Black Panther shuffled 1 card (Vibranium) into their deck.", shown.Summary);
        Assert.Empty(shown.Anchors);
        Assert.Empty(shown.Relationships);
        EventPresentation countOnly = EventPresenter.Present(new CardsShuffledIntoDeck(0, 2, []), result.World);
        Assert.Equal("Black Panther shuffled 2 cards into their deck.", countOnly.Summary);
        // Synthetic event shape: this does not assert that the card offers a zero selection.
        EventPresentation zero = EventPresenter.Present(new CardsShuffledIntoDeck(0, 0, []), result.World);
        Assert.Equal("Black Panther shuffled 0 cards into their deck.", zero.Summary);
    }
}
