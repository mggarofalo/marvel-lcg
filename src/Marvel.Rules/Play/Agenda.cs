using Marvel.Rules.Timing;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>How far through its three parts a step has got.</summary>
/// <remarks>
/// The parts are <c>rr:ability</c>'s: an interrupt window, the occurrence, a
/// response window. A step is in exactly one of them at any moment, which is
/// what makes the whole thing resumable.
/// </remarks>
public enum Stage
{
    /// <summary>Before it happens — <c>rr:ability.step.2</c>.</summary>
    Interrupts,

    /// <summary>It happens — <c>rr:ability.step.3</c>.</summary>
    Apply,

    /// <summary>After it happened — <c>rr:ability.step.4</c>.</summary>
    Responses,
}
