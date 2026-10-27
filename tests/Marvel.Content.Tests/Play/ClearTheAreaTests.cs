using Marvel.Content.Tests.Cards;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ClearTheAreaTests
{
    [Theory]
    [InlineData(false, 1, 1)]
    [InlineData(false, 2, 1)]
    [InlineData(false, 3, 0)]
    [InlineData(true, 1, 1)]
    [InlineData(true, 2, 1)]
    [InlineData(true, 3, 0)]
    [Rule("rr:thwart.2.1")]
    public void DrawDependsOnActuallyRemovingTheLastThreat(bool sideScheme, int threat, int draws)
    {
        // 04049: "If this removes the last threat on that scheme, draw 1 card."
        // rr:thwart.2.1: "An ability labeled as a thwart is considered a single thwart".
        var (game, world, card, scheme, top) = Begin(sideScheme, threat);
        int hand = world.Seats[0].Hand.Cards.Count;
        Play(game, card, scheme);
        Assert.Equal(Math.Max(0, threat - 2), scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(hand - 2 + draws, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(draws == 1, world.Seats[0].Hand.Cards.Contains(top));
        Assert.Equal(sideScheme && threat <= 2 ? DeckType.EncounterDiscardPile :
            sideScheme ? DeckType.SideSchemesArea : DeckType.MainSchemesArea, scheme.Area.Type);
    }

    [Fact]
    [Rule("rr:labeled-ability.6")]
    [Rule("rr:cancel.2")]
    [Rule("rr:cancel.3")]
    public void ConfusedCancelsRemovalAndItsConditionalDraw()
    {
        // "The entire ability (except for its costs) is canceled."
        // "If the effects of an event card are canceled, the card is still
        // considered played, and it is discarded."
        var (game, world, card, scheme, top) = Begin(false, 1, confused: true);
        int hand = world.Seats[0].Hand.Cards.Count;
        var discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0));
        int discarded = discard.Cards.Count;
        Pay(game, card);
        Assert.True(game.IsRootPrompt);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(discarded + 2, discard.Cards.Count);
        Assert.Equal(hand - 2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(1, scheme.Tokens["k_threat"]);
        Assert.DoesNotContain(top, world.Seats[0].Hand.Cards);
        Assert.Equal(DeckType.PlayerDeck, top.Area.Type);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Confused));
    }

    [Fact]
    public void EmptySchemeCannotBeChosenForAFreeDraw()
    {
        var (game, _, card, _, _) = Begin(false, 0);
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == card.ObjectId);
    }

    private static void Play(Game game, Card card, Card scheme)
    {
        Pay(game, card);
        var choice = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == scheme.ObjectId);
        game.Resolve(Decision.Take(choice.Id));
    }

    private static void Pay(Game game, Card card)
    {
        var offer = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == card.ObjectId);
        var cost = offer.CostOptions[0];
        var source = cost.Sources![0];
        var allocation = ResourcePayment.Allocate(cost, [source.Effect]);
        Assert.NotNull(allocation);
        game.Resolve(Decision.Take(offer.Id, [], [source.Effect], new Dictionary<string, long>(), allocation));
    }

    private static (Game Game, World World, Card Event, Card Scheme, Card Top) Begin(
        bool side, int threat, bool confused = false)
    {
        Card? card = null, scheme = null, top = null;
        var (game, world) = Playing(board =>
        {
            card = board.CreateCard("04049", board.Seats[0].Hand);
            scheme = side ? board.CreateCard("01107", board.AreaOf(DeckType.SideSchemesArea))
                : board.AreaOf(DeckType.MainSchemesArea).Cards[0];
            scheme.PlaceTokens("k_threat", threat - scheme.Tokens.GetValueOrDefault("k_threat"));
            top = board.CreateCard("01090", board.Seats[0].Deck);
            if (confused) Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Confused);
        }, hero: true, abilities: Runner());
        return (game, world, card!, scheme!, top!);
    }

    internal static AbilityRunner Runner() => AuthoredCards.Runner();
}
