using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>Named card queries implemented by the engine.</summary>
public enum AbilityCardQuery
{
    /// <summary>The public physical top of the unique typed Invocation deck.</summary>
    InvocationTop,
    /// <summary>Physical status cards on live characters.</summary>
    StatusCards,
    /// <summary>The authored villain relation.</summary>
    Villain,
    /// <summary>Controlled upgrades physically attached to the resolving hero.</summary>
    UpgradesOnYourHero,
    /// <summary>The side scheme paired with the current active villain.</summary>
    ActiveVillainSideScheme,
    /// <summary>The current villain physically paired with the attached scheme.</summary>
    VillainOfAttachedScheme,
    /// <summary>Living villains tied for the least threat on their corresponding schemes.</summary>
    VillainsWithLeastSchemeThreat,
    /// <summary>Living villains tied for the most threat on their corresponding schemes.</summary>
    VillainsWithMostSchemeThreat,
    /// <summary>The authored mainScheme relation.</summary>
    MainScheme,
    /// <summary>The authored yourAsideMinion relation.</summary>
    YourAsideMinion,
    /// <summary>The authored yourAsideSideScheme relation.</summary>
    YourAsideSideScheme,
    /// <summary>The authored minionsEngagedWithYou relation.</summary>
    MinionsEngagedWithYou,
    /// <summary>The authored identitiesWithinPerPlayerLimit relation.</summary>
    IdentitiesWithinPerPlayerLimit,
    /// <summary>The authored attachedToThis relation.</summary>
    AttachedToThis,
    /// <summary>Encounter attachment cards attached to the source, excluding upgrades and statuses.</summary>
    AttachmentsOnThis,
    /// <summary>In-play upgrades attached to the source.</summary>
    UpgradesOnThis,
    /// <summary>Encounter attachments hosted by the current villain.</summary>
    AttachmentsOnVillain,
    /// <summary>Out-of-play cards explicitly stored beneath this source.</summary>
    BeneathThis,
    /// <summary>The authored heroesAndAllies relation.</summary>
    HeroesAndAllies,
    /// <summary>Hero-form identities and allies; alter-egos are excluded.</summary>
    HeroAndAllyTargets,
    /// <summary>The authored sideSchemes relation.</summary>
    SideSchemes,
    /// <summary>The authored minions relation.</summary>
    Minions,
    /// <summary>The authored enemies relation.</summary>
    Enemies,
    /// <summary>The authored attackableEnemies relation.</summary>
    AttackableEnemies,
    /// <summary>The authored attackableMinions relation.</summary>
    AttackableMinions,
    /// <summary>The authored schemes relation.</summary>
    Schemes,
    /// <summary>The authored thwartableSchemes relation.</summary>
    ThwartableSchemes,
    /// <summary>The authored powerTargets relation.</summary>
    PowerTargets,
    /// <summary>The authored yourAsidePile relation.</summary>
    YourAsidePile,
    /// <summary>The authored upgradesAndSupportsYouControl relation.</summary>
    UpgradesAndSupportsYouControl,
    /// <summary>The authored identitySpecificInYourHand relation.</summary>
    IdentitySpecificInYourHand,
    /// <summary>The authored supportsYouControl relation.</summary>
    SupportsYouControl,
    /// <summary>Every support in play, independently of its controller.</summary>
    Supports,
    /// <summary>The authored charactersYouControl relation.</summary>
    CharactersYouControl,
    /// <summary>Controlled characters other than those exhausted for this ability cost.</summary>
    CharactersYouControlExceptCost,
    /// <summary>The authored upgradesYouControl relation.</summary>
    UpgradesYouControl,
    /// <summary>The authored enemiesEngagedWithChosenPlayer relation.</summary>
    EnemiesEngagedWithChosenPlayer,
    /// <summary>The current villain and each effective minion engaged with the resolving player.</summary>
    VillainAndMinionsEngagedWithYou,
    /// <summary>The authored alliesYouControl relation.</summary>
    AlliesYouControl,
    /// <summary>The authored allies relation.</summary>
    Allies,
    /// <summary>The authored heroes relation.</summary>
    Heroes,
    /// <summary>The authored identities relation.</summary>
    Identities,
    /// <summary>The authored characters relation.</summary>
    Characters,
    /// <summary>In-play environments other than the card entering in this occurrence.</summary>
    OtherEnvironments,
    /// <summary>Every in-play card controlled by the resolving player.</summary>
    CardsYouControl,
    /// <summary>All living villains, independently of the active counter.</summary>
    Villains,
    /// <summary>Live schemes paired with living villains other than this attachment's host.</summary>
    OtherVillainSideSchemes,
    /// <summary>Every physical in-play card reachable from the source.</summary>
    CardsInPlay,
}
