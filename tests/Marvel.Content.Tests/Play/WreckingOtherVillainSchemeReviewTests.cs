using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingOtherVillainSchemeReviewTests
{
    [Fact]
    [Rule("rr:villain-defeat.3.2")]
    public void CarriedSourceKeepsOtherSchemesAfterItsHostPromotes()
    {
        // "Attachments, upgrades, status cards, counters, and non-damage tokens
        // on a villain carry over to the new stage."
        // Synthetic selector contract; the source survives same-character promotion.
        var (world, groups, initial) = WreckingProjectionFixture.Create();
        var source = world.CreateCard("07007", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Villains, groups[0].Villain.ObjectId));
        var context = initial.Context.WithQuery(initial.Context.Query with
            { Source = source, SourceIncarnation = source.Incarnation });
        var scope = new AbilityAdmission.AbilityAdmissionScope(context, []);
        var query = new AbilityCardSelection.Query(AbilityCardQuery.OtherVillainSideSchemes);
        var expected = groups.Skip(1).Select(group => group.Scheme).ToArray();
        var area = new AreaProjectionState(context);
        area.Departed.Add(groups[0].Villain.ObjectId);
        area.Entered.Add(groups[0].Stages[1].ObjectId);
        area.Hosts[source.ObjectId] = groups[0].Stages[1].ObjectId;
        Assert.Equal(expected, AbilityProjectedStateQueries.ProjectedEvery(query, area, context));

        var power = AbilityPowerStateFactory.Initial(scope);
        power.Discarded.Add(groups[0].Villain.ObjectId);
        power.EncounterStages![groups[0].Villain.ObjectId] = groups[0].Stages[1].ObjectId;
        Assert.Equal(expected, AbilityPowerSelectionTrace.PowerEvery(query, scope, power));
        var trace = new AbilitySelectorTraceContext(power.CurrentVillain, scope, power.Discarded,
            power.Traits, power.Modifiers, power.Engagement)
            { EncounterStages = power.EncounterStages, EncounterOrder = power.EncounterOrder };
        Assert.Equal(expected, AbilitySelectorTrace.MatchingCandidates(query, trace));
    }

    [Fact]
    public void PairOrderSurvivesPhysicalReorderingAndAnotherVillainsPromotion()
    {
        // Synthetic 07007 selector contract: "each other villain's side scheme".
        // Engine choice: immutable group order, independently of physical area chronology.
        var (world, groups, initial) = WreckingProjectionFixture.Create();
        var source = world.CreateCard("07007", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Villains, groups[2].Villain.ObjectId));
        var context = initial.Context.WithQuery(initial.Context.Query with
            { Source = source, SourceIncarnation = source.Incarnation });
        var scope = new AbilityAdmission.AbilityAdmissionScope(context, []);
        var query = new AbilityCardSelection.Query(AbilityCardQuery.OtherVillainSideSchemes);
        foreach (var group in groups.AsEnumerable().Reverse())
            World.MoveToTop(group.Scheme, world.AreaOf(DeckType.SideSchemesArea));
        var expected = new[] { groups[0].Scheme, groups[1].Scheme, groups[3].Scheme };
        Assert.NotEqual(expected.Select(card => card.ObjectId),
            world.AreaOf(DeckType.SideSchemesArea).Cards
                .Where(card => card != groups[2].Scheme).Select(card => card.ObjectId));
        var area = new AreaProjectionState(context);
        area.Departed.Add(groups[0].Villain.ObjectId);
        area.Entered.Add(groups[0].Stages[1].ObjectId);
        var power = AbilityPowerStateFactory.Initial(scope);
        power.EncounterStages![groups[0].Villain.ObjectId] = groups[0].Stages[1].ObjectId;
        power = power with { EncounterOrder = [groups[3].Villain.ObjectId,
            groups[2].Villain.ObjectId, groups[1].Villain.ObjectId, groups[0].Stages[1].ObjectId] };

        Assert.Equal(expected, AbilityInitiationPrimitives.Every(query, scope));
        Assert.Equal(expected, AbilityProjectedStateQueries.ProjectedEvery(query, area, context));
        Assert.Equal(expected, AbilityPowerSelectionTrace.PowerEvery(query, scope, power));
        var trace = new AbilitySelectorTraceContext(power.CurrentVillain, scope, power.Discarded,
            power.Traits, power.Modifiers, power.Engagement)
            { EncounterStages = power.EncounterStages, EncounterOrder = power.EncounterOrder };
        Assert.Equal(expected, AbilitySelectorTrace.MatchingCandidates(query, trace));

        World.MoveToTop(groups[0].Villain, world.AreaOf(DeckType.RemovedArea));
        World.MoveToTop(groups[0].Stages[1], world.AreaOf(DeckType.VillainArea));
        Assert.Equal(expected, AbilityInitiationPrimitives.Every(query, scope));
    }
}
