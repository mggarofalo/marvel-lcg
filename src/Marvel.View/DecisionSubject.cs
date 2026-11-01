namespace Marvel.View;

/// <summary>How a decision relates to the card it concerns.</summary>
public enum DecisionSubject
{
    /// <summary>The prompt names no readable context card.</summary>
    None,

    /// <summary>The decision resolves the context card's effect.</summary>
    Resolving,

    /// <summary>The decision chooses among cards looked at by the context card.</summary>
    Inspecting,

    /// <summary>The decision chooses which engaged minion activates next.</summary>
    MinionActivation,
}
