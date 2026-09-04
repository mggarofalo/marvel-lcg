using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>A checked question about an ability's resolution and board.</summary>
public abstract record AbilityCondition
{
    private AbilityCondition() { }

    /// <summary>All operands must hold, in authored order.</summary>
    public sealed record All(ImmutableArray<AbilityCondition> Operands) : AbilityCondition;
    /// <summary>At least one operand must hold, in authored order.</summary>
    public sealed record Any(ImmutableArray<AbilityCondition> Operands) : AbilityCondition;
    /// <summary>The operand must not hold.</summary>
    public sealed record Negated(AbilityCondition Operand) : AbilityCondition;
    /// <summary>A question with no variable argument.</summary>
    public sealed record Flag(AbilityConditionFact Kind) : AbilityCondition;
    /// <summary>The payment included a resource of this kind.</summary>
    public sealed record PaidWithResource(char Resource) : AbilityCondition;
    /// <summary>A discarded card generated a resource of this kind.</summary>
    public sealed record DiscardedWithResource(char Resource) : AbilityCondition;
    /// <summary>The interrupted threat has this cause.</summary>
    public sealed record CausedThreat(ThreatCause Cause) : AbilityCondition;
    /// <summary>The selection contains at least one card.</summary>
    public sealed record Exists(AbilityCardSelection Cards) : AbilityCondition;
    /// <summary>The resolver can discard to thwart one of these schemes.</summary>
    public sealed record LegalPractice(AbilityCardSelection Schemes) : AbilityCondition;
    /// <summary>The resolver can automatically thwart the selected scheme.</summary>
    public sealed record AutomaticThwart(AbilityCardSelection Scheme) : AbilityCondition;
    /// <summary>A card with this printed title is in play.</summary>
    public sealed record TitleInPlay(string Title) : AbilityCondition;
    /// <summary>The left numeric expression is at least the right.</summary>
    public sealed record AtLeast(AbilityNumber Value, AbilityNumber Count) : AbilityCondition;
    /// <summary>The selected player is in this supported form.</summary>
    public sealed record InForm(AbilityPlayer Player, string Form) : AbilityCondition;
    /// <summary>The current activation is an attack, or otherwise a scheme.</summary>
    public sealed record ActivationIs(bool Attack) : AbilityCondition;
    /// <summary>A named property of a card matches the authored text.</summary>
    public sealed record CardText(AbilityCardSelection Card, AbilityCardTextProperty Property, string Text) : AbilityCondition;
    /// <summary>The selected card has the specified kind.</summary>
    public sealed record IsKind(AbilityCardSelection Card, CardKind Kind) : AbilityCondition;
    /// <summary>The occurrence defeated the selected card.</summary>
    public sealed record WasDefeated(AbilityCardSelection Card) : AbilityCondition;
    /// <summary>The selected card is the resolver's identity.</summary>
    public sealed record IsYourIdentity(AbilityCardSelection Card) : AbilityCondition;
}

/// <summary>Supported conditions with fixed authored arguments.</summary>
public enum AbilityConditionFact
{
    /// <summary>The current Special is the last in the ordered group.</summary>
    FinalStep,
    /// <summary>An ally can be played from a discard pile.</summary>
    CanMakeTheCall,
    /// <summary>The completed attack damaged its target.</summary>
    AttackDamaged,
    /// <summary>The game was set up in expert mode.</summary>
    InExpertMode,
    /// <summary>The resolver caused the defeat.</summary>
    DefeatedByYou,
    /// <summary>The resolver's identity defended the completed attack.</summary>
    HeroDefended,
    /// <summary>The current attack has no defender.</summary>
    UndefendedAttack,
    /// <summary>The defeat was caused by consequential damage.</summary>
    DefeatedByConsequentialDamage,
}

/// <summary>Card text and state properties that conditions can compare.</summary>
public enum AbilityCardTextProperty
{
    /// <summary>A status card.</summary>
    Status,
    /// <summary>A live trait.</summary>
    Trait,
    /// <summary>The printed encounter set.</summary>
    Set,
    /// <summary>The printed title.</summary>
    Title,
}

/// <summary>Player relations implemented by the ability resolver.</summary>
public enum AbilityPlayer
{
    /// <summary>The occurrence's player.</summary>
    TriggerPlayer,
    /// <summary>The resolving player.</summary>
    You,
    /// <summary>The source's controller.</summary>
    Controller,
    /// <summary>The selected player's identity owner.</summary>
    ChosenPlayer,
    /// <summary>The player the source is engaged with.</summary>
    EngagedPlayer,
    /// <summary>The first player.</summary>
    FirstPlayer,
}
