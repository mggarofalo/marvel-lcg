using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;
using static Marvel.Cards.Run.AbilityAdmission;

namespace Marvel.Content.Tests.Play;

public sealed class AbilityPowerFormTransitionTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:flip.1")]
    public void TwoOtherHeroChangesFollowTheProjectedFaceWithoutMutatingTheWorld()
    {
        // rr:flip.1: a three-sided card flips when its faceup side changes.
        var (world, scope, initial) = Setup();
        string digest = world.Digest().Canonical();
        var other = new AbilityEffect.ChangeForm(AbilityPlayer.You, new AbilityFormDestination.OtherHero());
        var giant = AbilityPowerOutcomeTrace.ChangeFormState(other, scope, false, initial);
        Assert.Equal("12001c", giant.IdentityFaces[0]);
        Assert.Equal(AdmissionResolution.Full, Assert.Single(AbilityPowerOutcomeTrace.PowerOutcomes(other, scope, false, false, giant)!));
        var tiny = AbilityPowerOutcomeTrace.ChangeFormState(other, scope, false, giant);
        Assert.Empty(tiny.IdentityFaces);
        Assert.Equal(0UL, tiny.FormsMayChange);
        Assert.True(AbilityPowerAlternatives.SameConcretePowerState(initial, tiny));
        Assert.Equal(digest, world.Digest().Canonical());
    }

    [Fact]
    public void ExactSameFaceHasNoOutcomeAndDistinctFacesAreNotMerged()
    {
        var (_, scope, initial) = Setup("12001b");
        var giant = Change("12001c");
        var tiny = Change("12001a");
        var projected = AbilityPowerOutcomeTrace.ChangeFormState(giant, scope, false, initial);
        Assert.Equal(AdmissionResolution.None, Assert.Single(AbilityPowerOutcomeTrace.PowerOutcomes(giant, scope, false, false, projected)!));
        Assert.True(AbilityPowerAlternatives.SameConcretePowerState(projected,
            AbilityPowerOutcomeTrace.ChangeFormState(giant, scope, false, projected)));
        Assert.Equal(AdmissionResolution.Full, Assert.Single(AbilityPowerOutcomeTrace.PowerOutcomes(tiny, scope, false, false, projected)!));
        var alternative = AbilityPowerOutcomeTrace.ChangeFormState(tiny, scope, false, initial);
        Assert.Equal(projected.FormsMayChange, alternative.FormsMayChange);
        Assert.False(AbilityPowerAlternatives.SameConcretePowerState(projected, alternative));
        Assert.Equal(2, AbilityPowerAlternatives.PowerPaths(AbilityPowerAlternatives.MergePowerAlternatives([projected, alternative])).Length);
    }

    [Fact]
    [Rule("rr:cannot.1")]
    public void ProhibitedChangeHasNoOutcomeOrMutation()
    {
        // rr:cannot.1: "The word cannot is absolute".
        var (world, scope, initial) = Setup();
        initial.Modifiers[(world.Seats[0].IdentityCard.ObjectId, FormChangePermissions.ProhibitedField)] = 1;
        var change = Change("12001c");
        Assert.Equal(AdmissionResolution.None, Assert.Single(AbilityPowerOutcomeTrace.PowerOutcomes(change, scope, false, false, initial)!));
        Assert.True(AbilityPowerAlternatives.SameConcretePowerState(initial,
            AbilityPowerOutcomeTrace.ChangeFormState(change, scope, false, initial)));
    }

    [Fact]
    public void LegacyUncertainSeatIsRejectedButFirstPlayerMarkerIsIndependent()
    {
        var (_, scope, initial) = Setup();
        var change = Change("12001c");
        var uncertain = initial with { FormsMayChange = 1 };
        Assert.Throws<RulesNotImplementedException>(() => AbilityPowerOutcomeTrace.ChangeFormState(change, scope, false, uncertain));
        Assert.Throws<RulesNotImplementedException>(() => AbilityPowerOutcomeTrace.PowerOutcomes(change, scope, false, false, uncertain));
        var marked = initial with { FormsMayChange = AbilityPowerOutcomeTrace.FirstPlayerRebinding };
        var giant = AbilityPowerOutcomeTrace.ChangeFormState(change, scope, false, marked);
        var tiny = AbilityPowerOutcomeTrace.ChangeFormState(Change("12001a"), scope, false, giant);
        Assert.Equal(AbilityPowerOutcomeTrace.FirstPlayerRebinding, tiny.FormsMayChange);
    }

    [Fact]
    public void NamedFormsUseTheProjectedFace()
    {
        var (_, scope, initial) = Setup();
        var giant = AbilityPowerOutcomeTrace.ChangeFormState(Change("12001c"), scope, false, initial);
        var hero = new AbilityEffect.ChangeForm(AbilityPlayer.You, new AbilityFormDestination.Form(Forms.Hero));
        var alter = new AbilityEffect.ChangeForm(AbilityPlayer.You, new AbilityFormDestination.Form(Forms.AlterEgo));
        Assert.Equal(AdmissionResolution.None, Assert.Single(AbilityPowerOutcomeTrace.PowerOutcomes(hero, scope, false, false, giant)!));
        Assert.Equal(AdmissionResolution.Full, Assert.Single(AbilityPowerOutcomeTrace.PowerOutcomes(alter, scope, false, false, giant)!));
        var changed = AbilityPowerOutcomeTrace.ChangeFormState(alter, scope, false, giant);
        Assert.Equal("12001b", changed.IdentityFaces[0]);
        Assert.Equal(AdmissionResolution.None, Assert.Single(AbilityPowerOutcomeTrace.PowerOutcomes(alter, scope, false, false, changed)!));
    }

    [Fact]
    public void ReboundChosenPlayerInvalidatesOnlyPotentiallyChangedFaceFacts()
    {
        var (_, scope, initial) = Setup();
        var giant = AbilityPowerOutcomeTrace.ChangeFormState(Change("12001c"), scope, false, initial);
        var chosen = new AbilityEffect.ChangeForm(AbilityPlayer.ChosenPlayer,
            new AbilityFormDestination.Form(Forms.AlterEgo));
        var uncertain = AbilityPowerOutcomeTrace.ChangeFormState(chosen, scope, true, giant);
        Assert.Empty(uncertain.IdentityFaces);
        Assert.Throws<RulesNotImplementedException>(() => AbilityPowerOutcomeTrace.ChangeFormState(Change("12001a"), scope, false, uncertain));
        Assert.Equal("12001c", giant.IdentityFaces[0]);
    }

    [Fact]
    public void TwoFaceNamedChangesRetainTheirKnownPath()
    {
        var (_, scope, initial) = Setup("01019a", "01019a,01019b");
        var alter = new AbilityEffect.ChangeForm(AbilityPlayer.You, new AbilityFormDestination.Form(Forms.AlterEgo));
        var hero = new AbilityEffect.ChangeForm(AbilityPlayer.You, new AbilityFormDestination.Form(Forms.Hero));
        var changed = AbilityPowerOutcomeTrace.ChangeFormState(alter, scope, false, initial);
        Assert.Equal("01019b", changed.IdentityFaces[0]);
        Assert.Equal(AdmissionResolution.Full, Assert.Single(AbilityPowerOutcomeTrace.PowerOutcomes(hero, scope, false, false, changed)!));
        Assert.True(AbilityPowerAlternatives.SameConcretePowerState(initial,
            AbilityPowerOutcomeTrace.ChangeFormState(hero, scope, false, changed)));
    }

    [Fact]
    public void OtherHeroProjectionPreservesReadinessAndPhysicalIdentityForReady()
    {
        var (world, scope, initial) = Setup();
        var hero = world.Seats[0].IdentityCard;
        hero.Exhaust();
        initial = AbilityPowerStateFactory.Initial(scope);
        var change = new AbilityEffect.ChangeForm(AbilityPlayer.You, new AbilityFormDestination.OtherHero());
        var ready = new AbilityEffect.CardAction(AbilityCardInstruction.Ready,
            new AbilityCardSelection.Bound(AbilityCardBinding.YourHero));
        var giant = AbilityPowerOutcomeTrace.ChangeFormState(change, scope, false, initial);
        Assert.Equal("12001c", giant.IdentityFaces[0]);
        Assert.Equal(PowerReadiness.Exhausted, AbilityPowerStateProjection.PowerReady(hero, giant));
        var readied = AbilityPowerStateMutation.ApplyPowerLeafState(ready, scope, false, giant);
        Assert.Equal(PowerReadiness.Ready, AbilityPowerStateProjection.PowerReady(hero, readied));
        Assert.Equal("12001c", readied.IdentityFaces[0]);
        Assert.False(hero.Ready);
        Assert.Equal("12001a", hero.FaceId);
    }

    private static AbilityEffect.ChangeForm Change(string face) =>
        new(AbilityPlayer.You, new AbilityFormDestination.Face(face));

    private static (World, AbilityAdmission.AbilityAdmissionScope, AbilityPowerState) Setup(
        string face = "12001a", string identity = "12001a,12001b,12001c")
    {
        var world = Hero(identity);
        var hero = world.Seats[0].IdentityCard;
        hero.TurnTo(face);
        var query = new AbilityQueryContext(world, hero, new Occurrence(1, []), 0,
            hero.Incarnation, null, null, null, []);
        var expression = new AbilityExpressionContext(query, ImmutableDictionary<string, long>.Empty,
            [], "", 0, false, null);
        var context = new AbilityAdmissionContext(AbilityLowering.Book(AbilityCatalog.Parse("{\"cards\":[]}")),
            world.ResourceAbilities, expression, new(), null);
        var scope = new AbilityAdmission.AbilityAdmissionScope(context, []);
        return (world, scope, AbilityPowerStateFactory.Initial(scope));
    }
}
