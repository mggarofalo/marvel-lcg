using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>A checked relation selecting cards without executing authored code.</summary>
public abstract record AbilityCardSelection
{
    private AbilityCardSelection() { }

    /// <summary>A card bound by the current resolution.</summary>
    public sealed record Bound(AbilityCardBinding Binding) : AbilityCardSelection;
    /// <summary>A named engine query.</summary>
    public sealed record Query(AbilityCardQuery Kind) : AbilityCardSelection;
    /// <summary>A printed-title reference.</summary>
    public sealed record Titled(string Title) : AbilityCardSelection;
    /// <summary>Enemies carrying a named trait.</summary>
    public sealed record EnemiesWithTrait(string Trait) : AbilityCardSelection;
    /// <summary>Selected cards carrying a named trait.</summary>
    public sealed record WithTrait(AbilityCardSelection Cards, string Trait) : AbilityCardSelection;
    /// <summary>Cards without another attached copy of the source.</summary>
    public sealed record WithoutAnotherCopyAttached(AbilityCardSelection Cards) : AbilityCardSelection;
    /// <summary>Selected cards removable by the effect.</summary>
    public sealed record Discardable(AbilityCardSelection Cards) : AbilityCardSelection;
    /// <summary>All selected cards tied at the requested rank extreme.</summary>
    public sealed record Ranked(AbilityCardSelection Cards, AbilityCardRank By, bool Maximum) : AbilityCardSelection;
    /// <summary>Cards matching printed criteria in an ordered collection of areas.</summary>
    public sealed record InAreas(ImmutableArray<AbilitySearchArea> Areas, CardKind? Kind,
        string? Trait, string? Title) : AbilityCardSelection;
}

/// <summary>Card bindings implemented by the ability resolver.</summary>
public enum AbilityCardBinding
{
    /// <summary>The authored this binding.</summary>
    This,
    /// <summary>The authored that binding.</summary>
    That,
    /// <summary>The authored trigger.actor binding.</summary>
    TriggerActor,
    /// <summary>The authored trigger.target binding.</summary>
    TriggerTarget,
    /// <summary>The authored chosen binding.</summary>
    Chosen,
    /// <summary>The authored yourHero binding.</summary>
    YourHero,
    /// <summary>The authored yourAlterEgo binding.</summary>
    YourAlterEgo,
    /// <summary>The authored defeater binding.</summary>
    Defeater,
    /// <summary>The authored activatingEnemy binding.</summary>
    ActivatingEnemy,
    /// <summary>The authored defeated binding.</summary>
    Defeated,
    /// <summary>The authored you binding.</summary>
    You,
    /// <summary>The authored attachedTo binding.</summary>
    AttachedTo,
    /// <summary>The authored trigger.subject binding.</summary>
    TriggerSubject,
}

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

/// <summary>Values by which an authored selector can rank cards.</summary>
public enum AbilityCardRank
{
    /// <summary>Printed cost.</summary>
    Cost,
    /// <summary>Modified attack value.</summary>
    Attack,
    /// <summary>Printed health, including a facedown card's rules-defined base.</summary>
    PrintedHealth,
}

/// <summary>Areas supported by ability searches.</summary>
public enum AbilitySearchArea
{
    /// <summary>The encounter deck.</summary>
    EncounterDeck,
    /// <summary>The encounter discard pile.</summary>
    EncounterDiscardPile,
    /// <summary>The scenario's set-aside area.</summary>
    ScenarioSetAside,
    /// <summary>The resolving player's deck.</summary>
    YourDeck,
}
