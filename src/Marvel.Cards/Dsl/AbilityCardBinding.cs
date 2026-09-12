using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

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
