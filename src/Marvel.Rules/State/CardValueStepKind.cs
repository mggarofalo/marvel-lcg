namespace Marvel.Rules.State;

/// <summary>The operation actually performed while evaluating a quantity.</summary>
public enum CardValueStepKind
{
    /// <summary>A resolved numeric modifier is added.</summary>
    Add,
    /// <summary>The characteristic is lost and cannot be modified.</summary>
    Lost,
    /// <summary>A printed dash is an unmodifiable zero.</summary>
    Unmodifiable,
    /// <summary>The completed quantity cannot be less than zero.</summary>
    MinimumZero,
    /// <summary>Resolve the intrinsic base before applying modifiers.</summary>
    DefineBase,
}
