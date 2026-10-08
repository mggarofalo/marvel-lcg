using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class CorePersistentFactsTests
{
    [Rule("rr:hit-points.2.3")]
    [Fact]
    public void MarkVArmorIsControlledAndItsMaximumHpContributionDoesNotHeal()
    {
        // "You get +6 hit points." Raising maximum HP does not remove damage.
        World world = PersistentFixture.Board();
        Card hero = world.Seats[0].IdentityCard;
        hero.TakeDamage(4);
        Card armor = world.CreateCard("01036", PersistentFixture.Controlled(world));
        CardPersistentDescriptor fact = PersistentFixture.Facts(world, armor);
        Assert.Equal(new CardRelationDescriptor("Controlled", null, 0), fact.Relation);
        CardContributionDescriptor bonus = Assert.Single(fact.Contributions);
        Assert.Equal((hero.ObjectId, "HP", "Add", 6L), (bonus.TargetId, bonus.Attribute, bonus.Operation, bonus.Amount));
        CardDescriptor host = PersistentFixture.Card(world, hero);
        Assert.Equal(15, host.Face!.EffectiveValues["HP"].CurrentValue);
        Assert.Equal(11, host.Face.Fields["health"]);
        Assert.Equal(4, host.State!.Damage);
        Assert.Empty(fact.Abilities);
        Assert.False(fact.HasUnresolvedAbilities);
    }

    [Fact]
    public void ArcReactorExhaustsItselfAndCombatTrainingUsesControlPlacement()
    {
        // Arc Reactor: "exhaust Arc Reactor → ready Iron Man"; Combat Training
        // says "Play under any player's control", not "attach to".
        World world = PersistentFixture.Board();
        Card reactor = world.CreateCard("01035", PersistentFixture.Controlled(world));
        Card training = world.CreateCard("01057", PersistentFixture.Controlled(world));
        CardPersistentDescriptor facts = PersistentFixture.Facts(world, reactor);
        Assert.Equal(new CardRelationDescriptor("Controlled", null, 0), facts.Relation);
        CardPersistentAbilityDescriptor action = Assert.Single(facts.Abilities);
        Assert.Equal("Action", action.Trigger.Timing);
        Assert.Equal("hero", action.Trigger.Form);
        Assert.Equal("Source", Assert.Single(action.Costs).Target);
        Assert.Equal("Exhaust", action.Costs[0].Operation);
        Assert.Equal("Ready", Assert.Single(action.Effects).Operation);
        Assert.Equal("ActingIdentity", action.Effects[0].Target);
        Assert.Equal("Controlled", PersistentFixture.Facts(world, training).Relation.Kind);
        Assert.Null(PersistentFixture.Facts(world, training).Relation.HostId);
    }

    [Rule("rr:attach-to")]
    [Rule("rr:modifiers")]
    [Fact]
    public void InspiredNamesItsActualAllyAndBothAlreadyAppliedBonuses()
    {
        // "Attached ally gets +1 THW and +1 ATK." Attached relationships name
        // the host, while the host's effective values already include the bonus.
        World world = PersistentFixture.Board();
        Card ally = world.CreateCard("01084", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        Card inspired = PersistentFixture.Attach(world, "01074", ally);
        CardPersistentDescriptor facts = PersistentFixture.Facts(world, inspired);
        Assert.Equal("Attached", facts.Relation.Kind);
        Assert.Equal(ally.ObjectId, facts.Relation.HostId);
        Assert.Equal(["ATK", "THW"], facts.Contributions.Select(item => item.Attribute));
        Assert.All(facts.Contributions, item => { Assert.Equal(ally.ObjectId, item.TargetId); Assert.Equal(1, item.Amount); });
        Assert.Equal(3, PersistentFixture.Card(world, ally).Face!.EffectiveValues["ATK"].CurrentValue);
        Assert.False(facts.HasUnresolvedAbilities);
    }

    [Rule("rr:hit-points.2.3")]
    [Fact]
    public void GeneticallyEnhancedRaisesDroneMaximumWithoutRemovingTwoDamage()
    {
        // "Attached minion gets +3 hit points." Advanced Ultron Drone prints 4.
        World world = PersistentFixture.Board();
        Card drone = world.CreateCard("01143", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        drone.TakeDamage(2);
        Card enhanced = PersistentFixture.Attach(world, "01163", drone);
        CardPersistentDescriptor facts = PersistentFixture.Facts(world, enhanced);
        Assert.Equal(drone.ObjectId, facts.Relation.HostId);
        CardContributionDescriptor bonus = Assert.Single(facts.Contributions);
        Assert.Equal("HP", bonus.Attribute);
        Assert.Equal(3, bonus.Amount);
        CardDescriptor host = PersistentFixture.Card(world, drone);
        Assert.Equal(7, host.Face!.EffectiveValues["HP"].CurrentValue);
        Assert.Equal(5, host.Face.Fields["health"]);
        Assert.Equal(2, host.State!.Damage);
        Assert.Empty(facts.Abilities);
    }

    [Fact]
    public void ConcussionBlastersKeepsRetaliateAndItsResolvingHerosRemovalCosts()
    {
        // "+1 ATK" and "Retaliate 1"; its Hero Action exhausts your hero and
        // spends two energy resources before discarding the attachment.
        World world = PersistentFixture.Board();
        Card villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        Card blasters = PersistentFixture.Attach(world, "01153", villain);
        CardPersistentDescriptor facts = PersistentFixture.Facts(world, blasters);
        Assert.Equal(("ATK", 1L), (Assert.Single(facts.Contributions).Attribute, facts.Contributions[0].Amount));
        CardPersistentAbilityDescriptor constant = Assert.Single(facts.Abilities, row => row.Trigger.Timing == "Constant");
        CardPersistentEffectDescriptor retaliate = Assert.Single(constant.Effects);
        Assert.Equal(("Grant", "Host", "retaliate", 1L), (retaliate.Operation, retaliate.Target, retaliate.Field, retaliate.Amount));
        CardPersistentAbilityDescriptor action = Assert.Single(facts.Abilities, row => row.Trigger.Timing == "Action");
        Assert.Equal("hero", action.Trigger.Form);
        Assert.Collection(action.Costs,
            cost => { Assert.Equal("Exhaust", cost.Operation); Assert.Equal("ActingIdentity", cost.Target); },
            cost => { Assert.Equal("SpendResources", cost.Operation); Assert.Equal("YY", cost.Resources); });
        Assert.Equal("Discard", Assert.Single(action.Effects).Operation);
        Assert.Equal("Source", action.Effects[0].Target);
    }

    [Fact]
    public void ChargeSeparatesItsLiveAttackBonusFromOverkillAndAfterAttackDiscard()
    {
        // "+3 ATK"; the attack gains overkill, then Charge is discarded after
        // the attack. Expiry of a grant and deferred execution stay distinct.
        World world = PersistentFixture.Board();
        Card rhino = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        Card charge = PersistentFixture.Attach(world, "01099", rhino);
        CardPersistentDescriptor facts = PersistentFixture.Facts(world, charge);
        Assert.Equal(3, Assert.Single(facts.Contributions).Amount);
        CardPersistentAbilityDescriptor attack = Assert.Single(facts.Abilities);
        Assert.Equal("WhenAttackInitiated", attack.Trigger.Event);
        Assert.Equal("attachedTo", attack.Trigger.Actor);
        Assert.Collection(attack.Effects,
            effect => { Assert.Equal("overkill", effect.Field); Assert.Equal("EndOfAttack", effect.Until); Assert.Null(effect.After); },
            effect => { Assert.Equal("Discard", effect.Operation); Assert.Equal("WhenAttackEnds", effect.After); Assert.Null(effect.Until); });
    }

    [Fact]
    public void RhinoSuitRedirectsTheWholePacketAndDiscardsAtAccumulatedDamageThreshold()
    {
        // "place it here instead"; "5 or more damage" discards the suit.
        // Five is a post-placement discard threshold, not five HP or prevention.
        World world = PersistentFixture.Board();
        Card rhino = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        Card suit = PersistentFixture.Attach(world, "01098", rhino);
        suit.TakeDamage(3);
        CardPersistentDescriptor facts = PersistentFixture.Facts(world, suit);
        Assert.Empty(facts.Contributions);
        CardPersistentAbilityDescriptor ability = Assert.Single(facts.Abilities);
        Assert.Equal("WhenDamageWouldBeDealt", ability.Trigger.Event);
        Assert.Equal("attachedTo", ability.Trigger.Subject);
        Assert.Collection(ability.Effects,
            effect => { Assert.Equal("RedirectAllDamage", effect.Operation); Assert.Equal("Source", effect.Target); Assert.Null(effect.Amount); },
            effect => { Assert.Equal("Discard", effect.Operation); Assert.Equal(new("Source", "damage", 5), effect.Condition); });
        Assert.Equal(3, PersistentFixture.Card(world, suit).State!.Damage);
    }

    [Fact]
    public void SpiderTracerRetainsTheHostDefeatTriggerAndChosenScheme()
    {
        // "When attached minion is defeated, remove 3 threat from a scheme."
        World world = PersistentFixture.Board();
        Card minion = world.CreateCard("01143", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Card tracer = PersistentFixture.Attach(world, "01007", minion);
        CardPersistentAbilityDescriptor ability = Assert.Single(PersistentFixture.Facts(world, tracer).Abilities);
        Assert.Equal("ForcedInterrupt", ability.Trigger.Timing);
        Assert.Equal("WhenCardDefeated", ability.Trigger.Event);
        Assert.Equal("attachedTo", ability.Trigger.Subject);
        CardPersistentEffectDescriptor effect = Assert.Single(ability.Effects);
        Assert.Equal(("RemoveThreat", "ChosenScheme", 3L), (effect.Operation, effect.Target, effect.Amount));
    }

    [Fact]
    public void UpgradedDronesAttachesToAnEnvironmentButOnlyAffectsFacedownDrones()
    {
        // "Attach to the Ultron Drones environment"; "Each facedown Drone
        // minion gets +1 ATK and +1 hit point." Advanced Ultron Drone is faceup.
        World world = PersistentFixture.Board();
        Card environment = world.CreateCard("01140", world.AreaOf(DeckType.EnvironmentArea));
        Card upgrade = PersistentFixture.Attach(world, "01142", environment);
        Area engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        Card drone = world.CreateCard("01091", engaged);
        drone.AssignProfile(AbilityLowering.Book(PersistentFixture.Abilities).Profiles["effective-drone"]);
        drone.TurnFaceDown();
        Card advanced = world.CreateCard("01143", engaged);
        CardPersistentDescriptor facts = PersistentFixture.Facts(world, upgrade);
        Assert.Equal(environment.ObjectId, facts.Relation.HostId);
        Assert.Equal("Attached", facts.Relation.Kind);
        Assert.Equal("Shared", PersistentFixture.Facts(world, environment).Relation.Kind);
        Assert.Equal(["ATK", "HP"], facts.Contributions.Select(item => item.Attribute));
        Assert.All(facts.Contributions, item => Assert.Equal(drone.ObjectId, item.TargetId));
        Assert.DoesNotContain(facts.Contributions, item => item.TargetId == advanced.ObjectId);
        string wire = JsonSerializer.Serialize(facts);
        Assert.DoesNotContain("01091", wire, StringComparison.Ordinal);
        Assert.DoesNotContain("Vibranium", wire, StringComparison.Ordinal);
    }
}
