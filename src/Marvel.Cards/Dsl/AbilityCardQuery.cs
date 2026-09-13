using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>Named card queries implemented by the engine.</summary>
public enum AbilityCardQuery
{
    /// <summary>The authored villain relation.</summary>
    Villain,
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
    /// <summary>The authored heroesAndAllies relation.</summary>
    HeroesAndAllies,
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
    /// <summary>The authored charactersYouControl relation.</summary>
    CharactersYouControl,
    /// <summary>The authored upgradesYouControl relation.</summary>
    UpgradesYouControl,
    /// <summary>The authored blackPantherUpgrades relation.</summary>
    BlackPantherUpgrades,
    /// <summary>The authored enemiesEngagedWithChosenPlayer relation.</summary>
    EnemiesEngagedWithChosenPlayer,
    /// <summary>The authored alliesYouControl relation.</summary>
    AlliesYouControl,
    /// <summary>The authored allies relation.</summary>
    Allies,
    /// <summary>The authored heroes relation.</summary>
    Heroes,
    /// <summary>The authored identities relation.</summary>
    Identities,
    /// <summary>The authored identitiesWithTechInDiscard relation.</summary>
    IdentitiesWithTechInDiscard,
    /// <summary>The authored topmostTechInChosenDiscard relation.</summary>
    TopmostTechInChosenDiscard,
    /// <summary>The authored characters relation.</summary>
    Characters,
    /// <summary>The authored drones relation.</summary>
    Drones,
    /// <summary>The authored dronesEngagedWithYou relation.</summary>
    DronesEngagedWithYou,
}
