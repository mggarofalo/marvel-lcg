using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ComposableCardSelectionTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void CurrentTraitsAndControlDetermineTheSelectedUpgrades()
    {
        var (world, source) = Board();
        var first = world.CreateCard("01047", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var other = world.CreateCard("01047", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var selector = new AbilityCardSelection.WithTrait(
            new AbilityCardSelection.Query(AbilityCardQuery.UpgradesYouControl), "BLACK_PANTHER");
        Assert.DoesNotContain("TECH", Cards.Traits(first.FaceId));
        Assert.Equal([first], Evaluate(world, source).Every(selector));
        World.MoveToTop(first, other.Area);
        Assert.Empty(Evaluate(world, source).Every(selector));
        Assert.Equal(0, first.Owner);
        World.MoveToTop(other, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 1));
        Assert.Equal([other], Evaluate(world, source).Every(selector));
        var granted = new AbilityCardSelection.WithTrait(selector, "SYNTHETIC");
        Assert.Empty(Evaluate(world, source).Every(granted));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Traits.Granted + "SYNTHETIC", 1, Affects: other.ObjectId));
        Assert.Equal([other], Evaluate(world, source).Every(granted));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Characteristics.Lost + Traits.Granted + "BLACK_PANTHER", Affects: other.ObjectId));
        Assert.Empty(Evaluate(world, source).Every(selector));
    }

    [Fact]
    public void NamedPublicAreasPreserveTopOrderAndQueriesDoNotAllocateOrMutate()
    {
        var (world, source) = Board();
        var owned = world.CreateCard("01008", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        var lower = world.CreateCard("01037", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
        var upper = world.CreateCard("01008", lower.Area);
        var last = new AbilityCardSelection.Last(new AbilityCardSelection.WithTrait(
            new AbilityCardSelection.InPlayerArea(DeckType.DiscardPile, AbilityPlayer.ChosenPlayer), "TECH"));
        var context = Context(world, source) with
        { PlayerSelectionBinding = new(world.Seats[1].IdentityCard, world.Seats[1].Hero.Id, world.Seats[1].IdentityCard.Incarnation) };
        var evaluation = new AbilitySelectorEvaluation(context);
        string digest = world.Digest().Canonical();
        int areas = world.Areas.Count;
        long words = world.Random.Generator.WordsConsumed;

        Assert.Same(upper, evaluation.Find(last));
        Assert.Equal([lower, upper], evaluation.Every(((AbilityCardSelection.WithTrait)last.Cards).Cards));
        Assert.DoesNotContain(owned, evaluation.Every(last));
        var identities = new AbilityCardSelection.WithMatchingPlayerArea(
            new AbilityCardSelection.Query(AbilityCardQuery.Identities), DeckType.DiscardPile, CardKind.Upgrade, "TECH");
        Assert.Equal(world.Seats.Select(seat => seat.IdentityCard), evaluation.Every(identities));
        Assert.Null(new AbilitySelectorEvaluation(context,
            areasRead => !areasRead.Contains(DeckType.DiscardPile)).Find(last));
        for (int i = 0; i < 3; i++) Assert.Empty(evaluation.Every(
            new AbilityCardSelection.InPlayerArea(DeckType.AlliesArea, AbilityPlayer.You)));
        Assert.Equal(areas, world.Areas.Count);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(digest, world.Digest().Canonical());
        World.MoveToTop(upper, world.Seats[1].Hand);
        Assert.Same(lower, evaluation.Find(last));
    }

    [Fact]
    public void ExplicitObjectOrderIsIndependentOfEngagementInsertionOrder()
    {
        var (world, source) = Board();
        var first = world.CreateCard("01089", world.Seats[0].Deck);
        var second = world.CreateCard("01089", world.Seats[0].Deck);
        Engage(world);
        Engage(world);
        var selected = new AbilityCardSelection.InObjectIdOrder(new AbilityCardSelection.FaceDown(
            new AbilityCardSelection.WithTrait(new AbilityCardSelection.Query(AbilityCardQuery.Minions), "DRONE")));
        Assert.Equal([second, first], first.Area.Cards);
        Assert.Equal([first, second], Evaluate(world, source).Every(selected));
        Assert.Equal([first, second], Evaluate(world, source).Every(selected));
    }

    private static void Engage(World world) =>
        FacedownMinions.EngageTop(world, 0, AuthoredCards.DroneProfile, "test", "Engage", []);

    [Fact]
    public void UnorderedLastAfterProjectedReengagementFailsBeforeOfferingAWrongTarget()
    {
        var (world, source) = Board();
        var moved = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var already = world.CreateCard("01103", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var area = new AbilityCardSelection.InPlayerArea(DeckType.EngagedEnemiesArea, AbilityPlayer.ChosenPlayer);
        var binding = Context(world, source) with
        { PlayerSelectionBinding = new(world.Seats[1].IdentityCard, world.Seats[1].Hero.Id, world.Seats[1].IdentityCard.Incarnation) };
        var expression = new AbilityExpressionContext(binding,
            System.Collections.Immutable.ImmutableDictionary<string, long>.Empty, [], "", 0, false, null);
        var admission = new AbilityAdmission.AbilityAdmissionScope(new AbilityAdmissionContext(
            AbilityLowering.Book(AuthoredCards.Book), world.ResourceAbilities, expression, new(), null), []);
        var trace = new AbilitySelectorTraceContext(-1, admission, [], [], [], new() { [moved.ObjectId] = 1 });
        var unordered = new AbilityCardSelection.Last(area);
        Assert.Contains("area order", Assert.Throws<RulesNotImplementedException>(() =>
            AbilitySelectorTrace.TraceSelectorMatches(unordered, already, trace)).Message);
        var ordered = new AbilityCardSelection.Last(new AbilityCardSelection.InObjectIdOrder(area));
        Assert.True(AbilitySelectorTrace.TraceSelectorMatches(ordered, already, trace));
        World.MoveToTop(moved, already.Area);
        Assert.Same(moved, new AbilitySelectorEvaluation(binding).Find(unordered));
    }

    [Fact]
    public void NestedOrderingAppliesAfterProjectedVillainReplacement()
    {
        // Object order is an engine choice. A stage replacement must sort by
        // the replacement's id, rather than the defeated stage's position.
        var (world, source) = Board();
        var old = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var minion = world.CreateCard("01162", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var next = world.CreateCard("01095", world.AreaOf(DeckType.VillainDeck));
        var selector = new AbilityCardSelection.Last(new AbilityCardSelection.WithTrait(
            new AbilityCardSelection.InObjectIdOrder(new AbilityCardSelection.Query(AbilityCardQuery.Enemies)), "BRUTE"));
        var expression = new AbilityExpressionContext(Context(world, source),
            System.Collections.Immutable.ImmutableDictionary<string, long>.Empty, [], "", 0, false, null);
        var admission = new AbilityAdmission.AbilityAdmissionScope(new AbilityAdmissionContext(
            AbilityLowering.Book(AuthoredCards.Book), world.ResourceAbilities, expression, new(), null), []);
        var trace = new AbilitySelectorTraceContext(next.ObjectId, admission, [old.ObjectId], [], [], []);

        Assert.True(Traits.Has(world, minion, "BRUTE", Cards));
        Assert.True(AbilitySelectorTrace.TraceSelectorMatches(selector, next, trace));
        Assert.False(AbilitySelectorTrace.TraceSelectorMatches(selector, minion, trace));
        World.MoveToTop(old, world.AreaOf(DeckType.RemovedArea));
        World.MoveToTop(next, world.AreaOf(DeckType.VillainArea));
        Assert.Same(next, Evaluate(world, source).Find(selector));
    }

    private static AbilitySelectorEvaluation Evaluate(World world, Card source) => new(Context(world, source));
    private static AbilityQueryContext Context(World world, Card source) =>
        new(world, source, new Occurrence(1, []), 0, source.Incarnation, null, null, null, []);

    private static (World World, Card Source) Board()
    {
        var world = new World(Cards, 2);
        for (int i = 0; i < 2; i++)
        {
            var seat = world.CreateSeat($"p{i}");
            seat.IdentityCard = world.CreateCard(i == 0 ? "01001a" : "01010a", seat.Hero);
        }
        return (world, world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0)));
    }
}
