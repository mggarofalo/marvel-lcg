using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic projection contracts use the real faces without admitting the pack.
public sealed class ProjectedIdentityFaceTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:form-change-form.2")]
    [Rule("rr:modifiers.2")]
    public void ProjectedFaceChangesPrintedTraitsAndValuesButKeepsLastingEffects()
    {
        // rr:form-change-form.2: "only the form changes" and lasting effects remain.
        // rr:modifiers.2: additive and subtractive modifiers are calculated together.
        var world = Hero("12001a,12001b,12001c");
        var hero = world.Seats[0].IdentityCard;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Traits.Granted + "SYNTHETIC",
            Affects: hero.ObjectId));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Characteristics.Lost + Traits.Granted + "AVENGER", Affects: hero.ObjectId));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "hand_size", 2,
            Affects: hero.ObjectId));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "hand_size_maximum", 5,
            Affects: hero.ObjectId));
        var context = Context(world, hero);
        var state = new AreaProjectionState(context);
        state.SetFace(hero, "12001c");

        Assert.True(state.HasTrait(context, hero, "GIANT"));
        Assert.False(state.HasTrait(context, hero, "TINY"));
        Assert.True(state.HasTrait(context, hero, "SYNTHETIC"));
        Assert.False(state.HasTrait(context, hero, "AVENGER"));
        Assert.Equal(3, state.ModifiedOf(context, hero, "attack"));
        Assert.Equal(5, state.ModifiedOf(context, hero, "hand_size"));
        state.Modifiers[(hero.ObjectId, "hand_size")] = -2;
        Assert.Equal(4, state.ModifiedOf(context, hero, "hand_size"));
        Assert.Equal("12001a", hero.FaceId);
        Assert.Equal(2, Modified(world, hero, "attack"));
        Assert.Equal(5, Modified(world, hero, "hand_size"));
    }

    [Fact]
    public void ProjectedAlternativesKeepDistinctFacesWhenClonedAndMerged()
    {
        var world = Hero("12001a,12001b,12001c");
        var hero = world.Seats[0].IdentityCard;
        var context = Context(world, hero);
        var tiny = new AreaProjectionState(context);
        var giant = tiny.Clone();
        giant.SetFace(hero, "12001c");
        var copied = giant.Clone();
        Assert.Equal(2, AreaProjectionState.Distinct([tiny, giant, copied]).Count);
        Assert.Equal("12001c", copied.FaceOf(hero));
        Assert.Equal("12001a", tiny.FaceOf(hero));
        copied.SetFace(hero, "12001a");
        Assert.Single(AreaProjectionState.Distinct([tiny, copied]));
        Assert.Throws<ArgumentException>(() => copied.SetFace(hero, "01001a"));
    }

    [Fact]
    public void ProjectedFormsAndDashUseTheRequestedFaceWithoutFlipping()
    {
        var world = Hero("12001a,12001b,12001c");
        var hero = world.Seats[0].IdentityCard;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "attack", 3,
            Affects: hero.ObjectId));
        Assert.Contains(Forms.AlterEgo, Forms.OfIdentityFace(world, world.Seats[0], Cards, "12001b"));
        Assert.DoesNotContain(Forms.Hero, Forms.OfIdentityFace(world, world.Seats[0], Cards, "12001b"));
        Assert.Equal(0, CardValues.EvaluateRaw(world, hero, "attack", Cards, 1, "12001b").CurrentValue);
        Assert.Equal(5, CardValues.EvaluateRaw(world, hero, "attack", Cards, 1, "12001a").CurrentValue);
        Assert.Equal("12001a", hero.FaceId);
    }

    [Fact]
    public void ProjectedFaceWithUnprojectedConstantFailsClosed()
    {
        const string book = """
            {"cards":[{"card":"12001c","abilities":[{
              "trigger":{"timing":"Constant"},
              "effect":{"grant":{"card":"this","keyword":"attack","amount":1}}
            }]}]}
            """;
        var world = Hero("12001a,12001b,12001c");
        var hero = world.Seats[0].IdentityCard;
        var context = Context(world, hero, book);
        var state = new AreaProjectionState(context);
        state.SetFace(hero, "12001c");
        Assert.Throws<RulesNotImplementedException>(() => state.ModifiedOf(context, hero, "attack"));
        Assert.Throws<RulesNotImplementedException>(() => state.HasTrait(context, hero, "GIANT"));
        Assert.Equal("12001a", hero.FaceId);
    }

    [Fact]
    [Rule("rr:form-change-form.2")]
    public void AreaTraceFollowsExactFacesConditionsAndDependentOutcomes()
    {
        // rr:form-change-form.2: "only the form changes"; the physical identity remains.
        var world = Hero("12001a,12001b,12001c");
        var hero = world.Seats[0].IdentityCard;
        var context = Context(world, hero);
        var state = new AreaProjectionState(context);
        var projection = new AbilityAreaProjection([], null, new HashSet<DeckType>(), context, 1);
        var giant = new AbilityEffect.ChangeForm(AbilityPlayer.You, new AbilityFormDestination.Face("12001c"));
        var alter = new AbilityEffect.ChangeForm(AbilityPlayer.You, new AbilityFormDestination.Face("12001b"));
        var tiny = new AbilityEffect.ChangeForm(AbilityPlayer.You, new AbilityFormDestination.Face("12001a"));
        var self = new AbilityCardSelection.Bound(AbilityCardBinding.You);
        var mark = new AbilityEffect.GrantTrait(self, "SYNTHETIC", false, "endOfPhase");
        var conditional = new AbilityEffect.Conditional(
            new AbilityCondition.CardText(self, AbilityCardTextProperty.Trait, "GIANT"), mark, null);
        string digest = world.Digest().Canonical();

        state = Assert.Single(projection.Trace(giant, [state]));
        Assert.Equal("12001c", state.FaceOf(hero));
        Assert.True(AbilityProjectedConditions.Test(new AbilityCondition.InForm(AbilityPlayer.You, Forms.Hero), state, context));
        state = Assert.Single(projection.Trace(conditional, [state]));
        Assert.True(state.HasTrait(context, hero, "SYNTHETIC"));
        Assert.Equal(AbilityProjectionResolution.None, AbilityProjectedStateQueries.ProjectedResolution(giant, state, context));
        state = Assert.Single(projection.Trace(alter, [state]));
        Assert.True(AbilityProjectedConditions.Test(new AbilityCondition.InForm(AbilityPlayer.You, Forms.AlterEgo), state, context));
        state = Assert.Single(projection.Trace(tiny, [state]));
        Assert.Equal("12001a", state.FaceOf(hero));
        Assert.Equal(digest, world.Digest().Canonical());
    }

    [Fact]
    public void AreaTraceDependentChangeRunsContinuationOnlyWhenTheFaceChanges()
    {
        var world = Hero("12001a,12001b,12001c");
        var hero = world.Seats[0].IdentityCard;
        var context = Context(world, hero);
        var projection = new AbilityAreaProjection([], null, new HashSet<DeckType>(), context, 1);
        var change = new AbilityEffect.ChangeForm(AbilityPlayer.You, new AbilityFormDestination.Face("12001c"));
        var self = new AbilityCardSelection.Bound(AbilityCardBinding.You);
        var mark = new AbilityEffect.GrantTrait(self, "SYNTHETIC", false, "endOfPhase");
        var dependent = new AbilityEffect.Dependent(change, mark, true);
        var success = Assert.Single(projection.Trace(dependent, [new AreaProjectionState(context)]));
        Assert.True(success.HasTrait(context, hero, "SYNTHETIC"));
        var alreadyGiant = new AreaProjectionState(context);
        alreadyGiant.SetFace(hero, "12001c");
        var skipped = Assert.Single(projection.Trace(dependent, [alreadyGiant]));
        Assert.False(skipped.HasTrait(context, hero, "SYNTHETIC"));
        Assert.Equal("12001a", hero.FaceId);
    }

    [Fact]
    public void AreaTraceRejectsUnresolvedChoiceWithFormChange()
    {
        var world = Hero("12001a,12001b,12001c");
        var hero = world.Seats[0].IdentityCard;
        var context = Context(world, hero);
        var projection = new AbilityAreaProjection([], null, new HashSet<DeckType>(), context, 1);
        var change = new AbilityEffect.ChangeForm(AbilityPlayer.You,
            new AbilityFormDestination.Face("12001c"));
        var choice = new AbilityEffect.Choose([change, change], ["first", "second"]);
        var state = new AreaProjectionState(context);

        Assert.Throws<RulesNotImplementedException>(() => projection.Trace(choice, [state]));
        Assert.Equal("12001a", state.FaceOf(hero));
        Assert.Equal("12001a", hero.FaceId);
        var named = new AbilityEffect.ChangeForm(AbilityPlayer.You,
            new AbilityFormDestination.Form(Forms.AlterEgo));
        var existingChoice = new AbilityEffect.Choose([named, named], ["first", "second"]);
        Assert.Same(state, Assert.Single(projection.Trace(existingChoice, [state])));
    }

    [Fact]
    public void TraitScopedEffectsFailClosedAfterAProjectedFaceChange()
    {
        var world = Hero("12001a,12001b,12001c");
        var hero = world.Seats[0].IdentityCard;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "attack", 2,
            Scope: ContinuousEffect.CharactersWithTrait + "GIANT"));
        var context = Context(world, hero);
        var state = new AreaProjectionState(context);
        state.SetFace(hero, "12001c");
        Assert.Throws<RulesNotImplementedException>(() => state.ModifiedOf(context, hero, "attack"));
        Assert.Throws<RulesNotImplementedException>(() => state.HasTrait(context, hero, "GIANT"));
    }

    [Fact]
    [Rule("rr:cannot.1")]
    public void AreaTraceLeavesFaceUnchangedWhenChangeIsProhibited()
    {
        // rr:cannot.1: "The word cannot is absolute".
        var world = Hero("12001a,12001b,12001c");
        var hero = world.Seats[0].IdentityCard;
        var context = Context(world, hero);
        var state = new AreaProjectionState(context);
        state.Modifiers[(hero.ObjectId, FormChangePermissions.ProhibitedField)] = 1;
        var projection = new AbilityAreaProjection([], null, new HashSet<DeckType>(), context, 1);
        var change = new AbilityEffect.ChangeForm(AbilityPlayer.You, new AbilityFormDestination.Face("12001c"));
        Assert.Equal(AbilityProjectionResolution.None, AbilityProjectedStateQueries.ProjectedResolution(change, state, context));
        Assert.Equal("12001a", Assert.Single(projection.Trace(change, [state])).FaceOf(hero));
    }

    private static AbilityAdmissionContext Context(World world, Card source, string book = "{\"cards\":[]}")
    {
        var query = new AbilityQueryContext(world, source, new Occurrence(1, []), 0,
            source.Incarnation, null, null, null, []);
        var expression = new AbilityExpressionContext(query, ImmutableDictionary<string, long>.Empty,
            [], "", 0, false, null);
        return new AbilityAdmissionContext(AbilityLowering.Book(AbilityCatalog.Parse(book)),
            world.ResourceAbilities, expression, new(), null);
    }
}
