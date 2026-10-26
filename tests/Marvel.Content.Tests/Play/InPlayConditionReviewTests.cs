using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic condition contracts; these do not admit an expansion card or product.
public sealed class InPlayConditionReviewTests
{
    [Theory]
    [InlineData("42")]
    [InlineData("\"not-a-selector\"")]
    [InlineData("{\"query\":\"villain\",\"extra\":1}")]
    [InlineData("{\"card\":\"chosen\"}")]
    public void MalformedSelectorsFailBeforeGameplay(string selector)
    {
        var book = AbilityCatalog.Parse("""
            {"cards":[{"card":"synthetic","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "effect":{"if":{"test":{"isInPlay":SELECTOR},
                "then":{"draw":{"player":"you","count":1}}}}
            }]}]}
            """.Replace("SELECTOR", selector, StringComparison.Ordinal));
        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Book(book));
        Assert.Contains("isInPlay", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Rule("rr:in-play-and-out-of-play.4")]
    [Rule("rr:leaves-play.1")]
    public void DepartureDoesNotGloballyEraseRetainedPrintedFacts()
    {
        // Abilities "can only target, cards that are in play"; after departure
        // a card "is considered to be a new copy of the card" on reentry.
        var (world, _, original) = WreckingProjectionFixture.Create();
        var target = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var scope = new AbilityAdmission.AbilityAdmissionScope(original.Context.WithChosen(target), []);
        var chosen = new AbilityCardSelection.Bound(AbilityCardBinding.Chosen);
        var present = new AbilityCondition.IsInPlay(chosen);
        var hydra = new AbilityCondition.CardText(chosen, AbilityCardTextProperty.Trait, "HYDRA");
        Assert.True(AbilityInitiationPrimitives.Test(present, scope));

        // Arrange only the synthetic departure; encounter-deck cleanup is outside this contract.
        World.MoveToTop(target, world.AreaOf(DeckType.EncounterDiscardPile));

        Assert.False(AbilityInitiationPrimitives.Test(present, scope));
        Assert.True(AbilityInitiationPrimitives.Test(hydra, scope));
        Assert.True(AbilityInitiationPrimitives.Test(new AbilityCondition.IsKind(chosen, CardKind.Minion), scope));
        World.MoveToTop(target, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.False(AbilityInitiationPrimitives.Test(present, scope));
        Assert.True(AbilityInitiationPrimitives.Test(present,
            new AbilityAdmission.AbilityAdmissionScope(original.Context.WithChosen(target), [])));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void ProjectedDepartureOverridesTheUnchangedLiveBoard(bool discard, bool expected)
    {
        var (world, groups, original) = WreckingProjectionFixture.Create();
        var target = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var scope = new AbilityAdmission.AbilityAdmissionScope(original.Context.WithChosen(target), []);
        var condition = new AbilityCondition.IsInPlay(new AbilityCardSelection.Bound(AbilityCardBinding.Chosen));
        var trace = new VillainConditionTrace(groups[0].Villain, groups[0].Stages[1], scope,
            Changes(discard ? [target.ObjectId] : []), 0, 0);

        Assert.True(trace.TryTest(condition, out bool value));
        Assert.Equal(expected, value);
        Assert.True(AbilityInitiationPrimitives.Test(condition, scope));
        Assert.Equal(DeckType.EngagedEnemiesArea, target.Area.Type);
    }

    [Fact]
    public void ProjectedEntryMakesAnAbsentTitledMinionPresent()
    {
        var (world, groups, scope) = WreckingProjectionFixture.Create();
        var target = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDiscardPile));
        var condition = new AbilityCondition.IsInPlay(new AbilityCardSelection.Titled("Hydra Mercenary"));
        var trace = new VillainConditionTrace(groups[0].Villain, groups[0].Stages[1], scope, Changes([]), 0, 0);

        Assert.False(AbilityInitiationPrimitives.Test(condition, scope));
        Assert.True(trace.TryTest(condition, out bool value));
        Assert.True(value);
        Assert.Equal(DeckType.EncounterDiscardPile, target.Area.Type);
    }

    [Fact]
    public void UnresolvedVillainRelationshipStaysUnknownRatherThanReusingLiveTruth()
    {
        var (_, groups, scope) = WreckingProjectionFixture.Create();
        var condition = new AbilityCondition.IsInPlay(new AbilityCardSelection.Query(AbilityCardQuery.Villain));
        var trace = new VillainConditionTrace(groups[0].Villain, groups[0].Stages[1], scope,
            Changes([groups[0].Villain.ObjectId]), 0, 0);

        Assert.True(AbilityInitiationPrimitives.Test(condition, scope));
        Assert.False(trace.TryTest(condition, out _));
        Assert.True(AbilityConditionProjection.ValueReadsVillain(condition, scope));
    }

    private static VillainTraceChanges Changes(HashSet<int> discarded) => new(discarded,
        new Dictionary<int, long>(), new Dictionary<int, long>(), new Dictionary<(int, string), long>(),
        new Dictionary<int, HashSet<string>>(), new HashSet<(int, string)>(), new Dictionary<int, int>());
}
