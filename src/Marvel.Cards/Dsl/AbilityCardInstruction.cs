using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>Instructions parameterized by one card relation.</summary>
public enum AbilityCardInstruction
{
    /// <summary>Exhaust every selected card.</summary>
    Exhaust,
    /// <summary>Ready every eligible selected card.</summary>
    Ready,
    /// <summary>Discard the selected card.</summary>
    Discard,
    /// <summary>Remove the selected card from the game.</summary>
    RemoveFromGame,
    /// <summary>Return the selected card to hand.</summary>
    ReturnToHand,
    /// <summary>Return the selected card to its owner's hand.</summary>
    ReturnOwnedToHand,
    /// <summary>Add the selected card to the resolver's hand.</summary>
    AddToHand,
    /// <summary>Reveal the selected encounter card.</summary>
    Reveal,
    /// <summary>Attach the source to the selected host.</summary>
    AttachTo,
    /// <summary>Give the selected enemy an additional boost card.</summary>
    GiveAdditionalBoost,
    /// <summary>Place prevented damage on the selected card.</summary>
    SoakDamage,
    /// <summary>Replace imminent threat with damage to the selected card.</summary>
    ReplaceThreatWithDamage,
    /// <summary>Resolve the Specials on the selected cards.</summary>
    ResolveSpecials,
    /// <summary>Prohibit threat removal from the selected scheme.</summary>
    PreventThreatRemoval,
    /// <summary>Prohibit readying the selected card.</summary>
    PreventReady,
    /// <summary>Declare the selected character as defender.</summary>
    DeclareDefender,
}
