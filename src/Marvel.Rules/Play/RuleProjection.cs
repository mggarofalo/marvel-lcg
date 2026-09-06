using System.Collections.Immutable;

namespace Marvel.Rules.Play;

/// <summary>A read-only calculation's supported results or unsupported boundary.</summary>
/// <typeparam name="T">The domain facts calculated without executing the game.</typeparam>
/// <remarks>
/// These are engine-chosen result shapes, not gameplay or wire-format state.
/// A possible result does not select a future answer. Unsupported is not an
/// empty set of legal answers: a caller needing those facts must raise instead.
/// </remarks>
public abstract record RuleProjection<T>
{
    private RuleProjection() { }

    /// <summary>One supported result under the supplied read assumptions.</summary>
    /// <param name="Value">The calculated domain facts.</param>
    public sealed record Known(T Value) : RuleProjection<T>;

    /// <summary>Supported alternatives that have not been chosen by a player.</summary>
    public sealed record Possible : RuleProjection<T>
    {
        /// <summary>Preserve the alternatives in deterministic calculation order.</summary>
        public Possible(ImmutableArray<T> alternatives)
        {
            if (alternatives.IsDefaultOrEmpty)
            {
                throw new ArgumentException("Possible projection needs an alternative.", nameof(alternatives));
            }
            Alternatives = alternatives;
        }

        /// <summary>The remaining possibilities; enumeration does not choose one.</summary>
        public ImmutableArray<T> Alternatives { get; }
    }

    /// <summary>A reached calculation the engine cannot make without guessing.</summary>
    /// <param name="Reason">The missing behavior, suitable for a fail-closed diagnostic.</param>
    public sealed record Unsupported(string Reason) : RuleProjection<T>;

    /// <summary>Lift one calculated value without adding speculative alternatives.</summary>
    public static implicit operator RuleProjection<T>(T value) => new Known(value);
}
