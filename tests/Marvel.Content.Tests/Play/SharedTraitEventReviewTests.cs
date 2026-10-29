using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SharedTraitEventReviewTests
{
    [Fact]
    public void ActualGiantStompResolvesSplashAndChosenEnemyBeforeParentDraw()
    {
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        var rows = book["cards"]!.AsArray();
        rows.Remove(rows.Single(row => row!["card"]!.GetValue<string>() == "01006"));
        rows.Add(JsonNode.Parse("""
            {"card":"01006","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"exhaust":"this"},"effect":{"seq":[{"playSharedHeroTraitCard":"game"},{"draw":{"player":"you","count":1}}]}}]}
            """));
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        Card? source = null, card = null, resource1 = null, resource2 = null, first = null, second = null;
        var (game, world) = Playing(board => {
            Hand(board, "01003", 0);
            source = InPlay(board, "01006");
            card = board.CreateCard("12003", board.Seats[0].Hand);
            resource1 = board.CreateCard("01003", board.Seats[0].Hand);
            resource2 = board.CreateCard("01003", board.Seats[0].Hand);
            first = board.CreateCard("01102", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            second = board.CreateCard("01102", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Traits.Granted + "GIANT", Affects: board.Seats[0].IdentityCard.ObjectId));
        }, hero: true, abilities: runner);
        int deck = world.Seats[0].Deck.Cards.Count;
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source!.ObjectId).Id));
        var offer = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == card!.ObjectId);
        var price = Assert.Single(offer.CostOptions);
        Assert.Equal("2", price.Cost);
        int[] payment = [resource1!.ObjectId, resource2!.ObjectId];
        game.Resolve(new Decision(offer.Id, [], payment, Allocations: ResourcePayment.Allocate(price, payment)));
        Assert.Equal(1, first!.Damage);
        Assert.Equal(1, second!.Damage);
        Assert.Equal(deck, world.Seats[0].Deck.Cards.Count);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == villain.ObjectId).Id));
        Assert.Equal(8, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, card!.Area.Type);
        Assert.Equal(deck - 1, world.Seats[0].Deck.Cards.Count);
        Assert.False(world.Agenda.IsBusy);
    }
    [Fact]
    public void SuspendedGeneratorResumesChildAndParentExactlyOnce()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01006","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"exhaust":"this"},"effect":{"seq":[{"playSharedHeroTraitCard":"game"},{"placeCounters":{"card":"you","counter":"parent","count":1}}]}}]},
              {"card":"01005","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "effect":{"placeCounters":{"card":"you","counter":"child","count":1}}}]},
              {"card":"01030","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
                "cost":{"dealDamage":{"cards":"this","amount":2}},"effect":{"generate":"YY"}}]},
              {"card":"01092","abilities":[{"trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}}]}]}
            """));
        Card? source = null, card = null, generator = null;
        var (game, world) = Playing(board => {
            Hand(board, "01003", 0);
            source = InPlay(board, "01006");
            card = board.CreateCard("01005", board.Seats[0].Hand);
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Traits.Granted + "AVENGER", Affects: card.ObjectId));
            generator = board.CreateCard("01030", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            generator.TakeDamage(2);
            InPlay(board, "01092");
        }, hero: true, abilities: runner);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source!.ObjectId).Id));
        var offer = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == card!.ObjectId);
        int[] payment = [generator!.ObjectId];
        game.Resolve(new Decision(offer.Id, [], payment, Allocations: ResourcePayment.Allocate(Assert.Single(offer.CostOptions), payment)));
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(DeckType.RevealingArea, card!.Area.Type);
        Assert.Equal(0, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_child"));
        Assert.Equal(0, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_parent"));
        game.Resolve(Decision.Decline);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_child"));
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_parent"));
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EventCancellationKeepsChildPaymentAndResumesParent(bool cancel)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01006","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"exhaust":"this"},"effect":{"seq":[{"playSharedHeroTraitCard":"game"},{"placeCounters":{"card":"you","counter":"parent","count":1}}]}}]},
              {"card":"01005","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "effect":{"placeCounters":{"card":"you","counter":"child","count":1}}}]},
              {"card":"05010","abilities":[{"trigger":{"event":"WhenEventPlayInitiated","timing":"Interrupt","subject":"you","form":"hero"},
                "cost":{"exhaust":"this"},"effect":{"cancelOccurrence":"trigger"}}]}]}
            """));
        Card? source = null, card = null, payment1 = null, payment2 = null, interrupt = null;
        var (game, world) = Playing(board => {
            Hand(board, "01003", 0);
            source = InPlay(board, "01006");
            card = board.CreateCard("01005", board.Seats[0].Hand);
            payment1 = board.CreateCard("01003", board.Seats[0].Hand);
            payment2 = board.CreateCard("01003", board.Seats[0].Hand);
            interrupt = board.CreateCard("05010", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Traits.Granted + "AVENGER", Affects: card.ObjectId));
        }, hero: true, abilities: runner);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source!.ObjectId).Id));
        var offer = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == card!.ObjectId);
        int[] payment = [payment1!.ObjectId, payment2!.ObjectId];
        game.Resolve(new Decision(offer.Id, [], payment, Allocations: ResourcePayment.Allocate(Assert.Single(offer.CostOptions), payment)));
        var opportunity = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(interrupt!.ObjectId, opportunity.AnchorId);
        game.Resolve(cancel ? Decision.Take(opportunity.Id) : Decision.Decline);
        Assert.Equal(cancel ? 0 : 1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_child"));
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_parent"));
        Assert.Equal(DeckType.DiscardPile, card!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment1.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment2.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void StaleSelectedPaymentIsRejectedWithoutConsumingTheChoice()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"exhaust":"this"},"effect":{"playSharedHeroTraitCard":"game"}}]},
              {"card":"01005","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "effect":{"draw":{"player":"you","count":1}}}]}]}
            """));
        Card? source = null, card = null, first = null, second = null;
        var (game, world) = Playing(board => {
            Hand(board, "01003", 0); source = InPlay(board, "01006");
            card = board.CreateCard("01005", board.Seats[0].Hand);
            first = board.CreateCard("01003", board.Seats[0].Hand);
            second = board.CreateCard("01003", board.Seats[0].Hand);
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Traits.Granted + "AVENGER", Affects: card.ObjectId));
        }, hero: true, abilities: runner);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source!.ObjectId).Id));
        var prompt = game.Pending!;
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == card!.ObjectId);
        int[] paid = [first!.ObjectId, second!.ObjectId];
        var allocations = ResourcePayment.Allocate(Assert.Single(offer.CostOptions), paid);
        World.MoveToTop(second, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        world.RecordInformation(InformationKind.Search);
        string before = world.Digest().Canonical();
        var agenda = world.Agenda.Outstanding.ToArray();
        var random = world.Random.Generator.GetState();
        Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(offer.Id, [], paid, Allocations: allocations)));
        Assert.Same(prompt, game.Pending);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(agenda, world.Agenda.Outstanding);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(InformationKind.Search, Assert.Single(new Resolution(world, prompt, []).Information).Kind);
    }
    [Fact]
    public void MatchingUnsupportedActionRaisesRatherThanDisappearing()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"exhaust":"this"},"effect":{"playSharedHeroTraitCard":"game"}}]},
              {"card":"01005","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"spendAny":1},"effect":{"draw":{"player":"you","count":1}}}]}]}
            """));
        var (_, world) = Playing(board => {
            Hand(board, "01003", 0); InPlay(board, "01006");
        }, hero: true, abilities: runner);
        var card = world.CreateCard("01005", world.Seats[0].Hand);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Traits.Granted + "AVENGER", Affects: card.ObjectId));
        var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        Assert.Contains("without an arrow cost", error.Message, StringComparison.Ordinal);
    }

}
