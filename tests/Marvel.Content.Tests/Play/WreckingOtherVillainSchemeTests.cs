using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingOtherVillainSchemeTests
{
    private static readonly AbilityCardSelection Query =
        new AbilityCardSelection.Query(AbilityCardQuery.OtherVillainSideSchemes);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void SelectsOnlyOtherLivingPairsAcrossLiveAndProjectedBoards(int host)
    {
        // Synthetic selector contract for 07007: "each other villain's side scheme".
        var (world, groups, scope) = Create(host);
        var unrelated = world.CreateCard(groups[0].Scheme.FaceId, world.AreaOf(DeckType.SideSchemesArea));
        var expected = groups.Where((_, index) => index != host).Select(group => group.Scheme).ToArray();
        var power = AbilityPowerStateFactory.Initial(scope);
        var area = new AreaProjectionState(scope.Context);
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed,
            world.Agenda.Outstanding.ToArray());

        Assert.Equal(expected, AbilityInitiationPrimitives.Every(Query, scope));
        Assert.Equal(expected, AbilityPowerSelectionTrace.PowerEvery(Query, scope, power));
        Assert.Equal(expected, AbilityProjectedStateQueries.ProjectedEvery(Query, area, scope.Context));
        var trace = new AbilitySelectorTraceContext(power.CurrentVillain, scope, power.Discarded,
            power.Traits, power.Modifiers, power.Engagement)
            { EncounterStages = power.EncounterStages, EncounterOrder = power.EncounterOrder };
        Assert.Equal(expected, AbilitySelectorTrace.MatchingCandidates(Query, trace));
        Assert.DoesNotContain(unrelated, AbilityInitiationPrimitives.Every(Query, scope));
        Assert.Equal(before.Item1, world.Digest().Canonical());
        Assert.Equal(before.Item2, world.Random.Generator.WordsConsumed);
        Assert.Equal(before.Item3, world.Agenda.Outstanding);

        int other = host == 0 ? 2 : 0;
        power.EncounterStages![groups[other].Villain.ObjectId] = -1;
        area.Departed.Add(groups[other].Villain.ObjectId);
        expected = expected.Where(card => card != groups[other].Scheme).ToArray();
        Assert.Equal(expected, AbilityPowerSelectionTrace.PowerEvery(Query, scope, power));
        Assert.Equal(expected, AbilityProjectedStateQueries.ProjectedEvery(Query, area, scope.Context));
        World.MoveToTop(groups[other].Villain, world.AreaOf(DeckType.RemovedArea));
        Assert.Equal(expected, AbilityInitiationPrimitives.Every(Query, scope));
    }

    [Fact]
    public void ProjectedReattachmentAndHostDefeatUseProjectedRelations()
    {
        var (_, groups, scope) = Create(0);
        var area = new AreaProjectionState(scope.Context);
        area.Hosts[scope.Source.ObjectId] = groups[3].Villain.ObjectId;
        Assert.Equal(groups.Take(3).Select(group => group.Scheme),
            AbilityProjectedStateQueries.ProjectedEvery(Query, area, scope.Context));
        area.Departed.Add(groups[3].Villain.ObjectId);
        Assert.Empty(AbilityProjectedStateQueries.ProjectedEvery(Query, area, scope.Context));
        var power = AbilityPowerStateFactory.Initial(scope);
        power.EncounterStages![groups[0].Villain.ObjectId] = -1;
        Assert.Empty(AbilityPowerSelectionTrace.PowerEvery(Query, scope, power));
    }

    [Fact]
    public void MissingLivePairRaisesAndDepartedSourceHasNoTargets()
    {
        var (world, groups, scope) = Create(0);
        var area = new AreaProjectionState(scope.Context);
        area.Departed.Add(groups[2].Scheme.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() =>
            AbilityProjectedStateQueries.ProjectedEvery(Query, area, scope.Context));
        var power = AbilityPowerStateFactory.Initial(scope);
        power.Discarded.Add(groups[2].Scheme.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => AbilityPowerSelectionTrace.PowerEvery(Query, scope, power));
        World.MoveToTop(groups[2].Scheme, world.AreaOf(DeckType.RemovedArea));
        Assert.Throws<RulesNotImplementedException>(() => AbilityInitiationPrimitives.Every(Query, scope));
        World.MoveToTop(scope.Source, world.AreaOf(DeckType.RemovedArea));
        Assert.Empty(AbilityInitiationPrimitives.Every(Query, scope));
        area.Departed.Add(scope.Source.ObjectId);
        Assert.Empty(AbilityProjectedStateQueries.ProjectedEvery(Query, area, scope.Context));
        power.Discarded.Add(scope.Source.ObjectId);
        Assert.Empty(AbilityPowerSelectionTrace.PowerEvery(Query, scope, power));
    }

    [Fact]
    public void MissingTopologyOrUnpairedHostRaises()
    {
        var (_, _, initial) = WreckingProjectionFixture.Create();
        Assert.Throws<RulesNotImplementedException>(() => AbilityInitiationPrimitives.Every(Query, initial));
        var world = new World(initial.World.Facts, 1, 1);
        var source = world.CreateCard("07007", world.AreaOf(DeckType.UpgradesArea));
        Assert.Throws<RulesNotImplementedException>(() => AbilityOtherVillainSchemeQuery.Live(
            initial.Context.Query with { World = world, Source = source, SourceIncarnation = source.Incarnation }));
    }

    [Fact]
    public void AuthoredQueryLowersToTypedSelectionAndRegistersBothAreaDependencies()
    {
        var book = AbilityCatalog.Parse("""
        {"cards":[{"card":"07007","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{"placeThreat":{"scheme":{"query":"otherVillainSideSchemes"},"amount":1}}
        }]}]}
        """);
        var program = AbilityLowering.Book(book);
        var effect = Assert.IsType<AbilityEffect.PlaceThreat>(Assert.Single(program.Effects.Values));
        Assert.Equal(Query, effect.Schemes);
        Assert.Equal(new[] { DeckType.VillainArea, DeckType.SideSchemesArea }.Order(),
            AbilitySelectionAreaReads.For(Query).Order());
        Assert.True(AbilityProjection.SelectorMembershipCanChange(Query));
    }

    private static (World World, EncounterDeckGroup[] Groups, AbilityAdmission.AbilityAdmissionScope Scope) Create(int host)
    {
        var (world, groups, initial) = WreckingProjectionFixture.Create();
        var source = world.CreateCard("07007", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Villains, groups[host].Villain.ObjectId));
        var context = initial.Context.WithQuery(initial.Context.Query with
            { Source = source, SourceIncarnation = source.Incarnation });
        return (world, groups, new(context, []));
    }
}
