using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class StunnedFriendlyCountTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    private static AbilityRunner Runner() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"02037","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{"placeThreat":{"scheme":"this","amount":{"mul":[2,
            {"count":{"withStatus":{"cards":{"query":"heroesAndAllies"},"status":"stunned"}}}
          ]}}}
        }]}]}
        """));

    [Rule("rr:friendly")]
    [Rule("rr:character")]
    [Rule("rr:stun-stunned.3")]
    [Fact]
    public void MessOfThingsCountsOnlyStunnedFriendlyCharactersAcrossSeats()
    {
        // "Friendly is a blanket term that refers to cards the players control."
        // "Identities (heroes and alter-egos), allies, villains, and minions are all characters."
        // "A character is stunned if it has a stunned status card."
        var world = Board();
        var scheme = world.CreateCard("02037", world.AreaOf(DeckType.SideSchemesArea));
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var unstunnedAlly = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var enemy = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var outOfPlay = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea,
            PlayArea.Of(1), cardOwner: 1));
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Stunned);
        Statuses.Give(world, ally, Statuses.Stunned);
        Statuses.Give(world, enemy, Statuses.Stunned);
        Statuses.Give(world, outOfPlay, Statuses.Stunned);
        World.MoveToTop(outOfPlay, world.Seats[1].Hand);
        Statuses.Give(world, unstunnedAlly, Statuses.Confused);
        var runner = Runner();
        world.Abilities = runner;
        string digest = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        var selection = new AbilityCardSelection.WithStatus(
            new AbilityCardSelection.Query(AbilityCardQuery.HeroesAndAllies), Statuses.Stunned);
        var query = new AbilitySelectorEvaluation(new AbilityQueryContext(world, scheme,
            new Occurrence(1, []), 0, scheme.Incarnation, null, null, null, []));
        Assert.Equal([world.Seats[0].IdentityCard, ally], query.Every(selection));
        Assert.Equal([world.Seats[0].IdentityCard, ally], query.Every(selection));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);

        var resolution = runner.ExecuteReveal(world, scheme, 0);
        Sequence.Finish(world, Cards, runner, resolution.Events.ToList());
        Assert.True(resolution.IsComplete);
        Assert.Equal(4, scheme.Tokens.GetValueOrDefault("k_threat"));
        World.MoveToTop(Statuses.On(world, ally, Statuses.Stunned).Single(),
            world.AreaOf(DeckType.RemovedArea));
        Assert.Equal([world.Seats[0].IdentityCard], query.Every(selection));
    }

    [Rule("rr:stun-stunned.3.1")]
    [Fact]
    public void OneStunnedCardOnSteadyCharacterDoesNotCountUntilSecond()
    {
        // "A character with the steady keyword is stunned only if it has two stunned status cards."
        var world = Board();
        var scheme = world.CreateCard("02037", world.AreaOf(DeckType.SideSchemesArea));
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "steady", 1,
            Affects: ally.ObjectId));
        Statuses.Give(world, ally, Statuses.Stunned);
        var selected = new AbilityCardSelection.WithStatus(
            new AbilityCardSelection.Query(AbilityCardQuery.HeroesAndAllies), Statuses.Stunned);
        var query = new AbilityQueryContext(world, scheme, new Occurrence(1, []), 0,
            scheme.Incarnation, null, null, null, []);
        var expressions = new AbilityExpressionContext(query, ImmutableDictionary<string, long>.Empty,
            [], "", 0, false, null);
        var admission = new AbilityAdmissionContext(AbilityLowering.Book(AuthoredCards.Book),
            world.ResourceAbilities, expressions, new(), null);
        var projected = new AreaProjectionState(admission);
        string digest = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        Assert.Empty(AbilityProjectedStateQueries.ProjectedEvery(selected, projected, admission));
        projected.Status[(ally.ObjectId, Statuses.Stunned)] = 2;
        Assert.Equal([ally], AbilityProjectedStateQueries.ProjectedEvery(selected, projected, admission));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        var runner = Runner();
        world.Abilities = runner;
        var first = runner.ExecuteReveal(world, scheme, 0);
        Sequence.Finish(world, Cards, runner, first.Events.ToList());
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("k_threat"));
        Statuses.Give(world, ally, Statuses.Stunned);
        projected.Status[(ally.ObjectId, Statuses.Stunned)] = 0;
        digest = world.Digest().Canonical();
        words = world.Random.Generator.WordsConsumed;
        Assert.Empty(AbilityProjectedStateQueries.ProjectedEvery(selected, projected, admission));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        var second = runner.ExecuteReveal(world, scheme, 0);
        Sequence.Finish(world, Cards, runner, second.Events.ToList());
        Assert.Equal(2, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:stun-stunned.3")]
    [Fact]
    public void ProjectedStatusFilterRequiresACharacterInPlay()
    {
        // "A character is stunned if it has a stunned status card."
        var world = Board();
        var scheme = world.CreateCard("02037", world.AreaOf(DeckType.SideSchemesArea));
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Statuses.Give(world, ally, Statuses.Stunned);
        World.MoveToTop(ally, world.Seats[0].Hand);
        var selection = new AbilityCardSelection.WithStatus(
            new AbilityCardSelection.InAreas([AbilitySearchArea.YourHand], null, null, null),
            Statuses.Stunned);
        var admission = Admission(world, scheme);
        var projected = new AreaProjectionState(admission);

        Assert.Empty(AbilityProjectedStateQueries.ProjectedEvery(selection, projected, admission));
        projected.Entered.Add(ally.ObjectId);
        Assert.Equal([ally], AbilityProjectedStateQueries.ProjectedEvery(selection, projected, admission));
        projected.Departed.Add(ally.ObjectId);
        Assert.Empty(AbilityProjectedStateQueries.ProjectedEvery(selection, projected, admission));
    }

    [Rule("rr:friendly")]
    [Rule("rr:stun-stunned.3")]
    [Fact]
    public void NewlyEnteredAllyParticipatesInProjectedFriendlyCount()
    {
        // "Friendly is a blanket term that refers to cards the players control."
        // "A character is stunned if it has a stunned status card."
        var world = Board();
        var scheme = world.CreateCard("02037", world.AreaOf(DeckType.SideSchemesArea));
        var ally = world.CreateCard("01002", world.Seats[0].Hand);
        var admission = Admission(world, scheme);
        var projected = new AreaProjectionState(admission);
        var friendly = new AbilityCardSelection.Query(AbilityCardQuery.HeroesAndAllies);
        var allies = new AbilityCardSelection.Query(AbilityCardQuery.Allies);
        var stunned = new AbilityCardSelection.WithStatus(friendly, Statuses.Stunned);

        Assert.DoesNotContain(ally, AbilityProjectedStateQueries.ProjectedEvery(friendly, projected, admission));
        projected.Entered.Add(ally.ObjectId);
        projected.Status[(ally.ObjectId, Statuses.Stunned)] = 1;
        Assert.Contains(ally, AbilityProjectedStateQueries.ProjectedEvery(allies, projected, admission));
        Assert.Contains(ally, AbilityProjectedStateQueries.ProjectedEvery(stunned, projected, admission));
        projected.Status[(ally.ObjectId, Statuses.Stunned)] = 0;
        Assert.DoesNotContain(ally, AbilityProjectedStateQueries.ProjectedEvery(stunned, projected, admission));
        projected.Departed.Add(ally.ObjectId);
        Assert.DoesNotContain(ally, AbilityProjectedStateQueries.ProjectedEvery(allies, projected, admission));
    }

    [Rule("rr:stun-stunned.3")]
    [Rule("rr:stun-stunned.3.1")]
    [Fact]
    public void PowerTraceUsesProjectedStatusCountAndSteadyThreshold()
    {
        // "A character is stunned if it has a stunned status card."
        // "A character with the steady keyword is stunned only if it has two stunned status cards."
        var world = Board();
        var scheme = world.CreateCard("02037", world.AreaOf(DeckType.SideSchemesArea));
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var admission = Admission(world, scheme);
        var scope = new AbilityAdmission.AbilityAdmissionScope(admission, []);
        var selection = new AbilityCardSelection.WithStatus(
            new AbilityCardSelection.Query(AbilityCardQuery.HeroesAndAllies), Statuses.Stunned);
        var state = AbilityPowerStateFactory.Initial(scope);

        Assert.Empty(AbilityPowerSelectionTrace.PowerEvery(selection, scope, state));
        state.StatusCounts[(ally.ObjectId, Statuses.Stunned)] = 1;
        Assert.Contains(ally, AbilityPowerSelectionTrace.PowerEvery(selection, scope, state));
        state.Modifiers[(ally.ObjectId, "steady")] = 1;
        Assert.DoesNotContain(ally, AbilityPowerSelectionTrace.PowerEvery(selection, scope, state));
        state.StatusCounts[(ally.ObjectId, Statuses.Stunned)] = 2;
        Assert.Contains(ally, AbilityPowerSelectionTrace.PowerEvery(selection, scope, state));
        state.StatusCounts[(ally.ObjectId, Statuses.Stunned)] = 0;
        Assert.DoesNotContain(ally, AbilityPowerSelectionTrace.PowerEvery(selection, scope, state));
        Statuses.Give(world, ally, Statuses.Stunned);
        state.Modifiers.Remove((ally.ObjectId, "steady"));
        Assert.DoesNotContain(ally, AbilityPowerSelectionTrace.PowerEvery(selection, scope, state));
        state.StatusCounts.Remove((ally.ObjectId, Statuses.Stunned));
        Assert.Contains(ally, AbilityPowerSelectionTrace.PowerEvery(selection, scope, state));
    }

    [Rule("rr:stun-stunned.3")]
    [Fact]
    public void VillainTransitionKeepsStatusDependentCountsAndConditionsUnknown()
    {
        // "A character is stunned if it has a stunned status card."
        var world = Board();
        var scheme = world.CreateCard("02037", world.AreaOf(DeckType.SideSchemesArea));
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var scope = new AbilityAdmission.AbilityAdmissionScope(Admission(world, scheme), []);
        var selection = new AbilityCardSelection.WithStatus(
            new AbilityCardSelection.Query(AbilityCardQuery.HeroesAndAllies), Statuses.Stunned);
        var statuses = new HashSet<(int Card, string Status)>();
        var changes = new VillainTraceChanges([], new Dictionary<int, long>(),
            new Dictionary<int, long>(), new Dictionary<(int Card, string Field), long>(),
            new Dictionary<int, HashSet<string>>(), statuses, new Dictionary<int, int>());

        Assert.False(AbilityProjection.TryTraceCount(selection, scheme, scope,
            changes, 0, out _));
        statuses.Add((ally.ObjectId, Statuses.Stunned));
        Assert.False(AbilityProjection.TryTraceCount(selection, scheme, scope,
            changes, 0, out _));
        var trace = new VillainConditionTrace(scheme, scheme, scope, changes, 0, world.FirstPlayer);
        var condition = new AbilityCondition.Exists(selection);
        var amount = new AbilityNumber.Conditional(condition,
            new AbilityNumber.Constant(2), new AbilityNumber.Constant(0));
        Assert.False(trace.TryTest(condition, out _));
        Assert.True(trace.AmountCanDiffer(amount, 16));
        Statuses.Give(world, ally, Statuses.Stunned);
        Assert.False(trace.TryTest(condition, out _));
        Assert.True(trace.AmountCanDiffer(amount, 16));
    }

    private static AbilityAdmissionContext Admission(World world, Card source)
    {
        var query = new AbilityQueryContext(world, source, new Occurrence(1, []), 0,
            source.Incarnation, null, null, null, []);
        var expressions = new AbilityExpressionContext(query, ImmutableDictionary<string, long>.Empty,
            [], "", 0, false, null);
        return new AbilityAdmissionContext(AbilityLowering.Book(AuthoredCards.Book),
            world.ResourceAbilities, expressions, new(), null);
    }

    private static World Board()
    {
        var world = new World(Cards, 2);
        for (int i = 0; i < 2; i++)
        {
            var seat = world.CreateSeat($"p{i}");
            seat.IdentityCard = world.CreateCard(i == 0 ? "01001a" : "01010a", seat.Hero);
        }
        return world;
    }
}
