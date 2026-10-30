using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual cards on prepared boards; this does not admit the Ant-Man pack.
public sealed class TeamBuildingExerciseActualTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HeroActionOffersOnlyMatchingHandCardAndExhaustsOnce(bool hero)
    {
        Card? source = null, matching = null, other = null;
        var (game, world) = Playing(board => {
            Hand(board, "01003", 0);
            source = InPlay(board, "12024");
            matching = board.CreateCard("03019", board.Seats[0].Hand);
            other = board.CreateCard("01002", board.Seats[0].Hand);
        }, hero: hero, abilities: AuthoredCards.Runner());
        var actions = game.Pending!.Affordances.Where(a => a.AnchorId == source!.ObjectId).ToArray();
        Assert.Equal(hero, actions.Length == 1);
        if (!hero) { Assert.True(source!.Ready); return; }
        Assert.Equal("Play a matching card", actions[0].DisplayLabel);
        Assert.Equal("Play a matching card with Team-Building Exercise", actions[0].CommitLabel);
        Assert.Equal("Exhaust Team-Building Exercise", actions[0].CostDescription);
        Assert.Equal("Play a card from your hand that shares a trait with your hero, reducing its cost by 1", actions[0].Description);
        var ordinary = Assert.Single(game.Pending.Affordances, a => a.AnchorId == matching!.ObjectId);
        Assert.Equal("1", Assert.Single(ordinary.CostOptions).Cost);
        game.Resolve(Decision.Take(actions[0].Id));
        Assert.False(source!.Ready);
        Assert.Equal("Choose a matching card to play", game.Pending!.DisplayQuestion);
        Assert.Contains("Team-Building Exercise was exhausted", game.Pending.Description);
        Assert.Contains("shown payment is the current cost", game.Pending.Description);
        Assert.False(game.Pending.Cancellable);
        var offer = Assert.Single(game.Pending.Affordances, a => a.AnchorId == matching!.ObjectId);
        Assert.Contains("This price already includes Team-Building Exercise's reduction of 1", offer.Description);
        Assert.True(offer.PlaysCard);
        Assert.DoesNotContain(game.Pending.Affordances, a => a.AnchorId == other!.ObjectId);
        Assert.Equal("0", Assert.Single(offer.CostOptions).Cost);
        game.Resolve(Decision.Take(offer.Id));
        Assert.Equal(DeckType.SupportsArea, matching!.Area.Type);
        Assert.Same(world.Seats[0].Hand, other!.Area);
        Assert.DoesNotContain(game.Pending!.Affordances, a => a.AnchorId == source.ObjectId);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("12001c", "12003", 2)]
    [InlineData("12001a", "12004", 1)]
    public void PlaysActualEventAtDiscountAndCompletesItsDeferredEffect(string form, string face, int cost)
    {
        Card? source = null, card = null, first = null, second = null, scheme = null, minion = null;
        var (game, world) = Playing(board => {
            Hand(board, "01003", 0);
            World.MoveToTop(board.Seats[0].IdentityCard, board.AreaOf(DeckType.RemovedArea));
            var identity = board.CreateCard("12001a,12001b,12001c", board.Seats[0].Hero);
            board.Seats[0].IdentityCard = identity;
            identity.TurnTo(form);
            source = InPlay(board, "12024");
            card = board.CreateCard(face, board.Seats[0].Hand);
            first = board.CreateCard("01003", board.Seats[0].Hand);
            if (cost == 2) second = board.CreateCard("01004", board.Seats[0].Hand);
            (scheme, minion) = EventBoard(board, face);
        }, abilities: AuthoredCards.Runner());
        long threat = scheme?.Tokens.GetValueOrDefault("k_threat") ?? 0;
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source!.ObjectId).Id));
        var occurrence = world.Agenda.Occurrence!;
        var parent = new PendingAbility(source!.ObjectId, AbilityType.Action, 0);
        Assert.False(source.Ready);
        var offer = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == card!.ObjectId);
        var price = Assert.Single(offer.CostOptions);
        Assert.Equal(cost.ToString(), price.Cost);
        Assert.Contains("This price already includes Team-Building Exercise's reduction of 1", offer.Description);
        Assert.True(offer.PlaysCard);
        int[] payment = second is null ? [first!.ObjectId] : [first!.ObjectId, second.ObjectId];
        game.Resolve(new Decision(offer.Id, [], payment, Allocations: ResourcePayment.Allocate(price, payment)));
        Assert.Equal(DeckType.RevealingArea, card!.Area.Type);
        Assert.Equal(ResolutionStatus.Pending, occurrence.Resolution.StatusOf(parent));
        var target = scheme ?? world.TheCardIn(DeckType.VillainArea)!;
        AssertSplash(minion);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == target.ObjectId).Id));
        AssertEventResult(scheme, target, threat);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(DeckType.DiscardPile, first!.Area.Type);
        if (second is not null) Assert.Equal(DeckType.DiscardPile, second.Area.Type);
        Assert.Equal(ResolutionStatus.Resolved, occurrence.Resolution.StatusOf(parent));
        Assert.False(world.Agenda.IsBusy);
    }

    private static (Card? Scheme, Card? Minion) EventBoard(World board, string face)
    {
        if (face != "12004")
            return (null, board.CreateCard("01102", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))));
        board.CreateCard("12007", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var scheme = board.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 4);
        return (scheme, null);
    }

    private static void AssertSplash(Card? minion)
    {
        if (minion is not null) Assert.Equal(1, minion.Damage);
    }

    private static void AssertEventResult(Card? scheme, Card target, long threat)
    {
        if (scheme is null) Assert.Equal(8, target.Damage);
        else Assert.Equal(threat - 3, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cost.4.1")]
    [Rule("rr:hit-points.3")]
    [Rule("rr:ally-limit")]
    public void ActualHankEntryDistinguishesZeroFromFourExcessResources(bool overpay)
    {
        // Excess resources "were not paid for that cost"; an ally with no remaining hit points is defeated.
        // MARVEL-885 records the owner's choice to resolve defeat before the ally limit.
        Card? source = null, hank = null;
        Card[] old = [], resources = [];
        var (game, world) = Playing(board => {
            Hand(board, "01003", 0);
            source = InPlay(board, "12024");
            hank = board.CreateCard("12011", board.Seats[0].Hand);
            var area = board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
            old = [board.CreateCard("01067", area), board.CreateCard("01068", area), board.CreateCard("01041", area)];
            if (overpay) resources = [board.CreateCard("01088", board.Seats[0].Hand), board.CreateCard("01089", board.Seats[0].Hand)];
        }, hero: true, abilities: AuthoredCards.Runner());
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source!.ObjectId).Id));
        var occurrence = world.Agenda.Occurrence!;
        var parent = new PendingAbility(source!.ObjectId, AbilityType.Action, 0);
        var offer = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == hank!.ObjectId);
        var price = Assert.Single(offer.CostOptions);
        Assert.Equal("0", price.Cost);
        Assert.Contains("Optional entry interrupt", offer.Description);
        Assert.Contains("overpaid", offer.Description);
        int[] payment = resources.Select(c => c.ObjectId).ToArray();
        game.Resolve(new Decision(offer.Id, [], payment, Allocations: ResourcePayment.Allocate(price, payment)));
        Assert.False(source.Ready);
        if (overpay) {
            Assert.Equal(DeckType.RevealingArea, hank!.Area.Type);
            Assert.Equal(ResolutionStatus.Pending, occurrence.Resolution.StatusOf(parent));
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == hank.ObjectId).Id));
            Assert.Equal(4, hank.Tokens.GetValueOrDefault("c_pym"));
            Assert.Equal(Steps.ChooseAllyForLimit, game.Pending!.Trigger);
            game.Resolve(Decision.Take(old[0].ObjectId));
            Assert.Equal(DeckType.AlliesArea, hank.Area.Type);
            Assert.Equal(DeckType.DiscardPile, old[0].Area.Type);
            Assert.All(resources, c => Assert.Equal(DeckType.DiscardPile, c.Area.Type));
        } else {
            Assert.Equal(DeckType.DiscardPile, hank!.Area.Type);
            Assert.Equal(0, hank.Tokens.GetValueOrDefault("c_pym"));
            Assert.All(old, c => Assert.Equal(DeckType.AlliesArea, c.Area.Type));
        }
        Assert.Equal(ResolutionStatus.Resolved, occurrence.Resolution.StatusOf(parent));
        Assert.False(world.Agenda.IsBusy);
    }
}
