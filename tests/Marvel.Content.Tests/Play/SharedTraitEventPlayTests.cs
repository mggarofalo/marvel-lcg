using System.Text.Json.Nodes;
using Marvel.Content.Tests.Cards;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic actions, not admission of either printed card.
public sealed class SharedTraitEventPlayTests
{
    [Fact]
    public void PaysAndExecutesEventBeforeParentContinues()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01006","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"exhaust":"this"},"effect":{"seq":[{"playSharedHeroTraitCard":"game"},{"draw":{"player":"you","count":1}}]}}]},
              {"card":"01005","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "effect":{"chooseCard":{"from":{"query":"villain"},"effect":{"draw":{"player":"you","count":1}}}}}]}]}
            """));
        Card? source = null, card = null, first = null, second = null;
        var (game, world) = Playing(board => {
            Hand(board, "01003", 0);
            source = InPlay(board, "01006");
            card = board.CreateCard("01005", board.Seats[0].Hand);
            first = board.CreateCard("01003", board.Seats[0].Hand);
            second = board.CreateCard("01003", board.Seats[0].Hand);
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Traits.Granted + "AVENGER", Affects: card.ObjectId));
        }, hero: true, abilities: runner);
        int deckCount = world.Seats[0].Deck.Cards.Count;
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source!.ObjectId).Id));
        Assert.False(source!.Ready);
        var occurrence = world.Agenda.Occurrence!;
        var parent = new PendingAbility(source.ObjectId, AbilityType.Action, 0);
        Assert.Equal(ResolutionStatus.Pending, occurrence.Resolution.StatusOf(parent));
        var offer = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == card!.ObjectId);
        var price = Assert.Single(offer.CostOptions);
        Assert.Equal("2", price.Cost);
        int[] paid = [first!.ObjectId, second!.ObjectId];
        game.Resolve(new Decision(offer.Id, [], paid, Allocations: ResourcePayment.Allocate(price, paid)));
        Assert.Equal(DeckType.RevealingArea, card!.Area.Type);
        Assert.Equal(deckCount, world.Seats[0].Deck.Cards.Count);
        Assert.Equal(ResolutionStatus.Pending, occurrence.Resolution.StatusOf(parent));
        var target = Assert.Single(game.Pending!.Affordances);
        game.Resolve(Decision.Take(target.Id));
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(DeckType.DiscardPile, first.Area.Type);
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
        Assert.Equal(deckCount - 2, world.Seats[0].Deck.Cards.Count);
        Assert.Equal(ResolutionStatus.Resolved, occurrence.Resolution.StatusOf(parent));
        Assert.False(world.Agenda.IsBusy);
    }
    [Fact]
    public void RejectsLostTraitBeforeConsumingThePendingChoice()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01006","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"exhaust":"this"},"effect":{"playSharedHeroTraitCard":"game"}}]},
              {"card":"01004","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}}]}]}
            """));
        Card? source = null, card = null;
        var (game, world) = Playing(board => {
            Hand(board, "01003", 0);
            source = InPlay(board, "01006");
            card = board.CreateCard("01004", board.Seats[0].Hand);
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Traits.Granted + "AVENGER", Affects: card.ObjectId));
        }, hero: true, abilities: runner);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source!.ObjectId).Id));
        var prompt = game.Pending!;
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == card!.ObjectId);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Characteristics.Lost + Traits.Granted + "AVENGER", Affects: card!.ObjectId));
        world.RecordInformation(InformationKind.Search);
        var digest = world.Digest().Canonical();
        var agenda = world.Agenda.Outstanding.ToArray();
        var random = world.Random.Generator.GetState();
        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(offer.Id)));
        Assert.Same(prompt, game.Pending);
        Assert.False(source!.Ready);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(agenda, world.Agenda.Outstanding);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(InformationKind.Search, Assert.Single(new Resolution(world, prompt, []).Information).Kind);
    }
    [Fact]
    public void PlaysAuthoredHiveMindWithItsLiveTinyConditionAndThwart()
    {
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        var rows = book["cards"]!.AsArray();
        rows.Remove(rows.Single(row => row!["card"]!.GetValue<string>() == "01006"));
        rows.Add(JsonNode.Parse("""
            {"card":"01006","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"exhaust":"this"},"effect":{"playSharedHeroTraitCard":"game"}}]}
            """));
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        Card? source = null, card = null, resource = null, scheme = null;
        var (game, world) = Playing(board => {
            Hand(board, "01003", 0);
            source = InPlay(board, "01006");
            card = board.CreateCard("12004", board.Seats[0].Hand);
            resource = board.CreateCard("01003", board.Seats[0].Hand);
            board.CreateCard("12007", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Traits.Granted + "TINY", Affects: board.Seats[0].IdentityCard.ObjectId));
            scheme = board.TheCardIn(DeckType.MainSchemesArea)!;
            scheme.PlaceTokens("k_threat", 4);
        }, hero: true, abilities: runner);
        long threat = scheme!.Tokens.GetValueOrDefault("k_threat");
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source!.ObjectId).Id));
        var offer = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == card!.ObjectId);
        var price = Assert.Single(offer.CostOptions);
        Assert.Equal("1", price.Cost);
        int[] payment = [resource!.ObjectId];
        game.Resolve(new Decision(offer.Id, [], payment, Allocations: ResourcePayment.Allocate(price, payment)));
        Assert.Equal(DeckType.RevealingArea, card!.Area.Type);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == scheme.ObjectId).Id));
        Assert.Equal(threat - 3, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(DeckType.DiscardPile, resource.Area.Type);
        Assert.False(source!.Ready);
        Assert.False(world.Agenda.IsBusy);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExhaustedParentCannotFinanceTheEvent(bool independentGenerator)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01006","abilities":[
                {"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                 "cost":{"exhaust":"this"},"effect":{"playSharedHeroTraitCard":"game"}},
                {"trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
                 "cost":{"exhaust":"this"},"effect":{"generate":"YY"}}]},
              {"card":"01030","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
                "cost":{"exhaust":"this"},"effect":{"generate":"YY"}}]},
              {"card":"01005","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}}]}]}
            """));
        Card? source = null;
        var (_, world) = Playing(board => {
            Hand(board, "01003", 0);
            source = InPlay(board, "01006");
            var card = board.CreateCard("01005", board.Seats[0].Hand);
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Traits.Granted + "AVENGER", Affects: card.ObjectId));
            if (independentGenerator)
                board.CreateCard("01030", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, abilities: runner);
        foreach (var card in world.Seats[0].Hand.Cards.Where(c => c.FaceId != "01005").ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        Assert.Equal(independentGenerator, runner.Actions(world, 0).Any(a => a.Card == source!.ObjectId));
        Assert.True(source!.Ready);
    }
}
