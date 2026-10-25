using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingActiveSchemeTests
{
    [Theory]
    [InlineData("Wrecker", 0)]
    [InlineData("Thunderball", 1)]
    [InlineData("Piledriver", 2)]
    [InlineData("Bulldozer", 3)]
    public void CompiledSequencePlacesThreatOnTheNewActivePair(string villain, int index)
    {
        // Synthetic composition isolates the active relation after movement, independent of source ownership.
        var (world, groups, _) = WreckingProjectionFixture.Create();
        var source = world.CreateCard("07043", world.AreaOf(DeckType.RevealingArea));
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
        {"cards":[{"card":"07043","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{"seq":[{"moveActiveCounter":{"titled":"TARGET"}},
            {"placeThreat":{"scheme":{"query":"activeVillainSideScheme"},"amount":2}}]}
        }]}]}
        """.Replace("TARGET", villain, StringComparison.Ordinal)));
        runner.ExecuteReveal(world, source, 0);
        Sequence.Finish(world, world.Facts, runner, []);
        for (int group = 0; group < groups.Length; group++)
            Assert.Equal(group == index ? 2 : 0, groups[group].Scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    public void MissingOrRemovedPairNeverFallsBackToAnotherScheme()
    {
        var (world, groups, scope) = WreckingProjectionFixture.Create();
        var area = new AreaProjectionState(scope.Context);
        area.Departed.Add(groups[1].Scheme.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => AbilityActiveSchemeQuery.InAreas(world, area));
        var power = AbilityPowerStateFactory.Initial(scope);
        power.Discarded.Add(groups[1].Scheme.ObjectId);
        var query = new AbilityCardSelection.Query(AbilityCardQuery.ActiveVillainSideScheme);
        Assert.Throws<RulesNotImplementedException>(() => AbilityPowerSelectionTrace.PowerFind(query, scope, power));
        World.MoveToTop(groups[1].Scheme, world.AreaOf(DeckType.RemovedArea));
        Assert.Throws<RulesNotImplementedException>(() => AbilityActiveSchemeQuery.Live(world));
    }

    [Fact]
    public void AnOrdinaryScenarioHasNoImplicitActiveSideScheme()
    {
        var (configured, _, _) = WreckingProjectionFixture.Create();
        var world = new World(configured.Facts, 1, 1);
        Assert.Throws<RulesNotImplementedException>(() => AbilityActiveSchemeQuery.Live(world));
    }

    [Fact]
    public void NoLivingVillainProducesNoProjectedScheme()
    {
        var (world, _, scope) = WreckingProjectionFixture.Create();
        var area = new AreaProjectionState(scope.Context) { ActiveVillain = -1 };
        Assert.Empty(AbilityActiveSchemeQuery.InAreas(world, area));
        var power = AbilityPowerStateFactory.Initial(scope) with { CurrentVillain = -1 };
        Assert.Empty(AbilityPowerSelectionTrace.PowerEvery(
            new AbilityCardSelection.Query(AbilityCardQuery.ActiveVillainSideScheme), scope, power));
    }
}
