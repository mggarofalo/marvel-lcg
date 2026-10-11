using Marvel.Rules.Events;

namespace Marvel.View;

/// <summary>Roles established by an authorized occurrence, independent of table placement.</summary>
public enum ActivitySubjectRole
{
    /// <summary>An affected object with no supplied causal role.</summary>
    Subject,
    /// <summary>The established origin.</summary>
    Source,
    /// <summary>The established destination.</summary>
    Target,
    /// <summary>The character established as defender.</summary>
    Defender,
    /// <summary>The revealed boost contribution.</summary>
    Boost,
}
