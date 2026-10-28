using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual authored face on nonpublic research boards; not public admission.
public sealed class MusterConditionalCompositionTests
{
    [Theory]
    [InlineData("01094", 1, 0)]
    [InlineData("01095", 2, 1)]
    [InlineData("01096", 3, 3)]
    public void StageSelectsBoundAndFriendlyRecipients(string villainFace, int maximum, int count)
    {
        var runner = Runner();
        var (world, source, payment, payment2) = Board(runner, villainFace);
        var hero = world.Seats[0].IdentityCard;
        var other = world.Seats[1].IdentityCard;
        var ally = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1)).Cards.Single();
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId, payment2.ObjectId], []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var offer = Assert.Single(prompt.Affordances);
        Assert.Equal(0, offer.Targets!.Min);
        Assert.Equal(maximum, offer.Targets.Max);
        Assert.Contains(hero.ObjectId, offer.Targets.Legal);
        Assert.Contains(other.ObjectId, offer.Targets.Legal);
        Assert.Contains(ally.ObjectId, offer.Targets.Legal);
        Assert.DoesNotContain(enemy.ObjectId, offer.Targets.Legal);
        var selected = new[] { hero.ObjectId, other.ObjectId, ally.ObjectId }.Take(count).ToArray();
        Sequence.Answer(world, world.Facts, runner, prompt, new Decision(offer.Id, selected), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.All(new[] { hero, other, ally }, card =>
            Assert.Equal(selected.Contains(card.ObjectId), Statuses.Has(world, card, Statuses.Tough)));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }

    [Fact]
    public void InvalidGroupPreservesGamePromptAndCanRetryWithZero()
    {
        var runner = Runner();
        var (world, source, payment, payment2) = Board(runner, "01095");
        var game = Game.Begin(world, world.Facts, runner);
        while (game.Pending!.Affordances.Any(a => a.Verb == Game.ResolveMulligans))
            game.Resolve(Decision.Take(game.Pending.Affordances.Single(a => a.Verb == Game.ResolveMulligans).Id));
        var action = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source.ObjectId);
        int[] paying = [payment.ObjectId, payment2.ObjectId];
        game.Resolve(new Decision(action.Id, [], paying,
            Allocations: ResourcePayment.Allocate(Assert.Single(action.Costs!), paying)));
        var prompt = game.Pending!;
        var offer = Assert.Single(prompt.Affordances);
        int target = world.Seats[0].IdentityCard.ObjectId;
        string before = SpellMasteryFixture.Snapshot(world);
        Assert.ThrowsAny<ArgumentException>(() => game.Resolve(new Decision(offer.Id, [target, target])));
        Assert.Equal(before, SpellMasteryFixture.Snapshot(world));
        Assert.Same(prompt, game.Pending);
        game.Resolve(new Decision(offer.Id, []));
        Assert.True(game.IsRootPrompt);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }

    [Fact]
    public void HeroFormAndAvengerTraitAreBothRequired()
    {
        var runner = Runner();
        var (world, source, _, _) = Board(runner, "01094");
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        world.Seats[0].IdentityCard.TurnTo("12001b");
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        World.MoveToTop(world.Seats[0].IdentityCard, world.Seats[0].Hand);
        world.Seats[0].IdentityCard = world.CreateCard("05001a,05001b", world.Seats[0].Hero);
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source.ObjectId);
    }
    [Fact]
    [Rule("rr:status-cards.1")]
    public void AlreadyToughCharacterIsExcludedFromTheChoice()
    {
        // "A character cannot have more than one status card of each type at a time."
        var runner = Runner();
        var (world, source, payment, payment2) = Board(runner, "01096");
        var hero = world.Seats[0].IdentityCard;
        Statuses.Give(world, hero, Statuses.Tough);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId, payment2.ObjectId], []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var offer = Assert.Single(prompt.Affordances);
        Assert.DoesNotContain(hero.ObjectId, offer.Targets!.Legal);
        Assert.Contains(world.Seats[1].IdentityCard.ObjectId, offer.Targets.Legal);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.2")]
    public void AllFriendlyCharactersToughSuppressesOfferButOneEligibleAllowsSameCost()
    {
        // "If the card or ability does not have at least one valid target, it cannot be played or initiated."
        var runner = Runner();
        var (blocked, source, payment, payment2) = Board(runner, "01095");
        var (control, controlSource, _, _) = Board(runner, "01095");
        foreach (var character in new[] { blocked.Seats[0].IdentityCard, blocked.Seats[1].IdentityCard,
                     blocked.AreaOf(DeckType.AlliesArea, PlayArea.Of(1)).Cards.Single() })
            Statuses.Give(blocked, character, Statuses.Tough);
        Statuses.Give(control, control.Seats[0].IdentityCard, Statuses.Tough);
        Statuses.Give(control, control.Seats[1].IdentityCard, Statuses.Tough);
        Assert.DoesNotContain(runner.Actions(blocked, 0), action => action.Card == source.ObjectId);
        Assert.Contains(runner.Actions(control, 0), action => action.Card == controlSource.ObjectId);
        Assert.Same(blocked.Seats[0].Hand, source.Area);
        Assert.Same(blocked.Seats[0].Hand, payment.Area);
        Assert.Same(blocked.Seats[0].Hand, payment2.Area);
    }

    [Fact]
    [Rule("rr:villain-defeat")]
    public void CurrentStageRatherThanRemovedPriorStageSetsBound()
    {
        // "Remove the current stage ... The next sequential stage of the villain deck is revealed."
        var runner = Runner();
        var (world, source, payment, payment2) = Board(runner, "01094");
        World.MoveToTop(world.TheCardIn(DeckType.VillainArea)!, world.AreaOf(DeckType.RemovedArea));
        world.CreateCard("01095", world.AreaOf(DeckType.VillainArea));
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId, payment2.ObjectId], []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(2, Assert.Single(prompt.Affordances).Targets!.Max);
    }

    [Fact]
    public void SyntheticMultipleVillainsUseActiveRatherThanFirstVillain()
    {
        var runner = Runner();
        var (world, source, payment, payment2) = Board(runner, "01094");
        var first = world.TheCardIn(DeckType.VillainArea)!;
        var second = world.CreateCard("01095", world.AreaOf(DeckType.VillainArea));
        EncounterDeckGroup Group(Card villain, string scheme) => new(villain,
            world.CreateCard(scheme, world.AreaOf(DeckType.SideSchemesArea)),
            world.CreateArea(DeckType.EncounterDeck), world.CreateArea(DeckType.EncounterDiscardPile),
            [], [villain]);
        var firstGroup = Group(first, "07004");
        var secondGroup = Group(second, "07019");
        world.BindEncounterTopology(new EncounterTopology(world, [firstGroup, secondGroup], second));
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId, payment2.ObjectId], []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(2, Assert.Single(prompt.Affordances).Targets!.Max);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(4, true)]
    public void SyntheticStageZeroCannotPlayAndAboveThreeIsCapped(int stage, bool offered)
    {
        var runner = Runner();
        var (world, source, payment, payment2) = Board(runner, "01094", stage);
        var actions = runner.Actions(world, 0).Where(a => a.Card == source.ObjectId).ToArray();
        if (!offered)
        {
            Assert.Empty(actions);
            Assert.Same(world.Seats[0].Hand, payment.Area);
            return;
        }
        runner.ExecuteAction(world, new PlayerAction(Assert.Single(actions), [payment.ObjectId, payment2.ObjectId], []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(3, Assert.Single(prompt.Affordances).Targets!.Max);
    }

    private static AbilityRunner Runner() => AuthoredCards.Runner();

    private static (World World, Card Source, Card Payment, Card Payment2) Board(AbilityRunner runner, string villain, int? stage = null)
    {
        string catalog = File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));
        if (stage is not null)
        {
            var root = JsonNode.Parse(catalog)!;
            var face = root["cards"]!.AsArray().Single(row => row!["card_id"]!.GetValue<string>() == villain)!;
            face["attributes"]!["Stage"] = stage.Value.ToString();
            catalog = root.ToJsonString();
        }
        var cards = CardCatalog.Parse(catalog);
        var world = new World(cards, 2, 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        var other = world.CreateSeat("p1");
        other.IdentityCard = world.CreateCard("01001a,01001b", other.Hero);
        other.IdentityCard.TurnTo("01001b");
        world.CreateCard("12002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        world.CreateCard(villain, world.AreaOf(DeckType.VillainArea));
        for (int i = 0; i < 12; i++) world.CreateCard("01087", seat.Deck);
        for (int i = 0; i < 12; i++) world.CreateCard("01087", other.Deck);
        world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 12; i++) world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        var source = world.CreateCard("12032", seat.Hand);
        var payment = world.CreateCard("01088", seat.Hand);
        var payment2 = world.CreateCard("01089", seat.Hand);
        world.Abilities = runner;
        return (world, source, payment, payment2);
    }
}
