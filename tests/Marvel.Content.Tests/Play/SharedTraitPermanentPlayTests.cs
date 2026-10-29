using Marvel.Content.Tests.Cards;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic ability: the actual card is not admitted by this test book.
public sealed class SharedTraitPermanentPlayTests
{
    [Fact]
    public void DoesNotOfferActionWhenMatchingPermanentCannotBePaidFor()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{"playSharedHeroTraitPermanent":"game"}""",
            cost: """{"exhaust":"this"}""");
        Card? source = null;
        var (game, _) = Playing(board =>
        {
            Hand(board, "01001b", 5);
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("03024", board.Seats[0].Hand);
        }, hero: true, abilities: runner);

        Assert.DoesNotContain(game.Pending!.Affordances, item => item.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Fact]
    public void ExhaustsSourceAndPlaysMatchingPermanentAtReducedCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{"playSharedHeroTraitPermanent":"game"}""",
            cost: """{"exhaust":"this"}""");
        Card? source = null;
        Card? matching = null;
        Card? other = null;
        var (game, world) = Playing(board =>
        {
            Hand(board, "01003", 0);
            source = InPlay(board, AuthoredCards.AuntMay);
            matching = board.CreateCard("03019", board.Seats[0].Hand);
            other = board.CreateCard("01002", board.Seats[0].Hand);
            board.CreateCard("03019", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, abilities: runner);

        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.False(source!.Ready);
        var occurrence = world.Agenda.Occurrence!;
        var ability = new PendingAbility(source!.ObjectId, AbilityType.Action, 0);
        Assert.Equal(ResolutionStatus.Pending, occurrence.Resolution.StatusOf(ability));
        var offer = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(matching!.ObjectId, offer.AnchorId);
        Assert.Equal("0", Assert.Single(offer.CostOptions).Cost);
        game.Resolve(Decision.Take(offer.Id));

        Assert.Equal(ResolutionStatus.Resolved, occurrence.Resolution.StatusOf(ability));
        Assert.Equal(DeckType.SupportsArea, matching.Area.Type);
        Assert.Same(world.Seats[0].Hand, other!.Area);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void PaysTheRemainingCostWithARealResource()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{"playSharedHeroTraitPermanent":"game"}""",
            cost: """{"exhaust":"this"}""");
        Card? source = null;
        Card? card = null;
        Card? payment = null;
        var (game, world) = Playing(board =>
        {
            Hand(board, "01003", 0);
            source = InPlay(board, AuthoredCards.AuntMay);
            card = board.CreateCard("03024", board.Seats[0].Hand);
            payment = board.CreateCard("01003", board.Seats[0].Hand);
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        var offer = Assert.Single(game.Pending!.Affordances);
        var price = Assert.Single(offer.CostOptions);
        Assert.Equal("1", price.Cost);
        var allocations = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.Allocate(price, [payment!.ObjectId]));
        game.Resolve(Decision.Take(offer.Id, [], [payment.ObjectId], new Dictionary<string, long>(), allocations));

        Assert.Equal(DeckType.SupportsArea, card!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void ParentContinuesExactlyOnceAfterThePermanentEnters()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{"seq":[{"playSharedHeroTraitPermanent":"game"},{"draw":{"player":"you","count":1}}]}""",
            cost: """{"exhaust":"this"}""");
        Card? source = null;
        Card? card = null;
        Card? draw = null;
        var (game, world) = Playing(board =>
        {
            Hand(board, "01003", 0);
            source = InPlay(board, AuthoredCards.AuntMay);
            card = board.CreateCard("03019", board.Seats[0].Hand);
        }, hero: true, abilities: runner);
        draw = world.Seats[0].Deck.Cards[^1];
        int handCount = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        var offer = Assert.Single(game.Pending!.Affordances);
        game.Resolve(Decision.Take(offer.Id));

        Assert.Equal(DeckType.SupportsArea, card!.Area.Type);
        Assert.Same(world.Seats[0].Hand, draw!.Area);
        Assert.Equal(handCount, world.Seats[0].Hand.Cards.Count);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void ParentWaitsForSuspendedResourcePaymentAndResumesOnce()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01006","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"exhaust":"this"},"effect":{"seq":[{"playSharedHeroTraitPermanent":"game"},{"draw":{"player":"you","count":1}}]}}]},
              {"card":"01030","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
                "cost":{"dealDamage":{"cards":"this","amount":2}},"effect":{"generate":"Y"}}]},
              {"card":"01092","abilities":[{"trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}}]}
            ]}
            """));
        Card? source = null, card = null, generator = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, "01006");
            card = board.CreateCard("03024", board.Seats[0].Hand);
            generator = board.CreateCard("01030", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            generator.TakeDamage(2);
            InPlay(board, "01092");
        }, hero: true, abilities: runner);
        int deckCount = world.Seats[0].Deck.Cards.Count;
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source!.ObjectId).Id));
        var occurrence = world.Agenda.Occurrence!;
        var ability = new PendingAbility(source!.ObjectId, AbilityType.Action, 0);
        var offer = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == card!.ObjectId);
        var cost = Assert.Single(offer.CostOptions);
        var paid = new[] { generator!.ObjectId };
        game.Resolve(new Decision(offer.Id, [], paid, Allocations: ResourcePayment.Allocate(cost, paid)));

        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(ResolutionStatus.Pending, occurrence.Resolution.StatusOf(ability));
        Assert.Equal(DeckType.RevealingArea, card!.Area.Type);
        Assert.Equal(deckCount, world.Seats[0].Deck.Cards.Count);
        game.Resolve(Decision.Decline);
        Assert.Equal(ResolutionStatus.Resolved, occurrence.Resolution.StatusOf(ability));
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.Equal(DeckType.SupportsArea, card.Area.Type);
        Assert.Equal(deckCount - 1, world.Seats[0].Deck.Cards.Count);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void RejectsChangedFormAfterSourceExhaustWithoutFurtherMutation()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{"playSharedHeroTraitPermanent":"game"}""",
            cost: """{"exhaust":"this"}""");
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            Hand(board, "01003", 0);
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("03019", board.Seats[0].Hand);
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        var offer = Assert.Single(game.Pending!.Affordances);
        world.Seats[0].IdentityCard.TurnTo("01001b");
        world.RecordInformation(InformationKind.Search);
        var before = world.Digest().Canonical();
        var pending = game.Pending;
        var agenda = world.Agenda.Outstanding.ToArray();
        long words = world.Random.Generator.WordsConsumed;
        var random = world.Random.Generator.GetState();

        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(offer.Id)));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(agenda, world.Agenda.Outstanding);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.False(source!.Ready);
        Assert.Same(pending, game.Pending);
        Assert.Equal(InformationKind.Search, Assert.Single(new Resolution(world, pending, []).Information).Kind);
        Assert.Same(offer, Assert.Single(game.Pending!.Affordances));
    }

    [Fact]
    public void RejectsStalePaymentBeforeClearingInformationOrPendingWork()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{"playSharedHeroTraitPermanent":"game"}""",
            cost: """{"exhaust":"this"}""");
        Card? source = null, card = null, payment = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            card = board.CreateCard("03024", board.Seats[0].Hand);
            payment = board.CreateCard("01003", board.Seats[0].Hand);
        }, hero: true, abilities: runner);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source!.ObjectId).Id));
        var prompt = game.Pending!;
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == card!.ObjectId);
        var allocations = ResourcePayment.Allocate(Assert.Single(offer.CostOptions), [payment!.ObjectId]);
        World.MoveToTop(payment, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        world.RecordInformation(InformationKind.Search);
        var digest = world.Digest().Canonical();
        var agenda = world.Agenda.Outstanding.ToArray();
        var random = world.Random.Generator.GetState();

        Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(offer.Id, [], [payment.ObjectId], Allocations: allocations)));
        Assert.Same(prompt, game.Pending);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(agenda, world.Agenda.Outstanding);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(InformationKind.Search, Assert.Single(new Resolution(world, prompt, []).Information).Kind);
    }
}
