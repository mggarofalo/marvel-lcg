using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class OtherVillainAttackRestrictionReviewTests
{
    [Theory]
    [InlineData("area")]
    [InlineData("power")]
    [InlineData("repeated")]
    [Rule("rr:villain-defeat.3.2")]
    public void CarriedTauntsKeepOnlyThePromotedHostAttackable(string projection)
    {
        // "Attachments, upgrades, status cards, counters, and non-damage tokens
        // on a villain carry over to the new stage."
        // Synthetic constant-only 07035 contract: "Players cannot attack other villains."
        var (world, groups, initial) = WreckingProjectionFixture.Create();
        var host = groups[2].Villain;
        var next = groups[2].Stages[1];
        var attachment = world.CreateCard("07035", world.AreaOf(DeckType.UpgradesArea,
            host.Area.PlayArea, host.ObjectId));
        var program = AbilityLowering.Book(AbilityCatalog.Parse("""
            {"cards":[{"card":"07035","abilities":[{
              "trigger":{"timing":"Constant"},
              "effect":{"grant":{"card":"attachedTo",
                "keyword":"other_villains_cannot_be_attacked","amount":1}}
            }]}]}
            """));
        world.Abilities = new AbilityRunner(program);
        var context = initial.Context with { Program = program };
        var scope = new AbilityAdmission.AbilityAdmissionScope(context, []);
        var query = new AbilityCardSelection.Query(AbilityCardQuery.AttackableEnemies);
        Assert.Equal(host, Assert.Single(BasicPowers.Attackable(world, world.Facts, 0)));
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        List<Card> selected;
        if (projection == "area")
        {
            var state = new AreaProjectionState(context);
            state.Departed.Add(host.ObjectId);
            state.Entered.Add(next.ObjectId);
            state.Hosts[attachment.ObjectId] = next.ObjectId;
            selected = AbilityProjectedStateQueries.ProjectedEvery(query, state, context);
        }
        else
        {
            var state = AbilityPowerStateFactory.Initial(scope);
            state.Discarded.Add(host.ObjectId);
            state.EncounterStages![host.ObjectId] = next.ObjectId;
            var trace = new AbilitySelectorTraceContext(state.CurrentVillain, scope,
                state.Discarded, state.Traits, state.Modifiers, state.Engagement)
                { EncounterStages = state.EncounterStages, EncounterOrder = state.EncounterOrder };
            selected = projection == "power"
                ? AbilityPowerSelectionTrace.PowerEvery(query, scope, state)
                : AbilitySelectorTrace.MatchingCandidates(query, trace).ToList();
        }
        Assert.Equal(new[] { next.ObjectId }, selected.Select(card => card.ObjectId));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
    }
}
