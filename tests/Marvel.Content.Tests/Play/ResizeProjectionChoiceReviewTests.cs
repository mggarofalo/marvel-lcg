using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic uncertainty: a named-form choice may change the later branch.
public sealed class ResizeProjectionChoiceReviewTests : CoreCardTestBase
{
    [Fact]
    public void NamedFormChoicePreservesTheReachableAlterEgoBranch()
    {
        var world = Hero("01019a,01019b");
        var hero = world.Seats[0].IdentityCard;
        var query = new AbilityQueryContext(world, hero, new Occurrence(1, []), 0,
            hero.Incarnation, null, null, null, []);
        var expression = new AbilityExpressionContext(query, ImmutableDictionary<string, long>.Empty,
            [], "", 0, false, null);
        var context = new AbilityAdmissionContext(AbilityLowering.Book(AbilityCatalog.Parse("{\"cards\":[]}")),
            world.ResourceAbilities, expression, new() { PriorStepMayMutate = true, PriorFormsMayChange = 1 }, null);
        var projection = new AbilityAreaProjection([], null, new HashSet<DeckType>(), context, 1);
        var choice = new AbilityEffect.Choose([
            new AbilityEffect.ChangeForm(AbilityPlayer.You, new AbilityFormDestination.Form(Forms.AlterEgo)),
            new AbilityEffect.Sequence([])], ["change", "stay"]);
        var self = new AbilityCardSelection.Bound(AbilityCardBinding.You);
        var branch = new AbilityEffect.Conditional(new AbilityCondition.InForm(AbilityPlayer.You, Forms.AlterEgo),
            new AbilityEffect.GrantTrait(self, "SYNTHETIC", false, "endOfPhase"), null);
        var states = projection.TraceSequence([choice, branch], [new AreaProjectionState(context)]);
        Assert.Contains(states, state => state.HasTrait(context, hero, "SYNTHETIC"));
    }
}
