using Marvel.Rules.Prompts;

namespace Marvel.Tests;

/// <summary>Two options in one prompt could not be told apart.</summary>
/// <remarks>
/// Its own type because it is not a rules gap: the rules are fine and the
/// engine's answer-lookup is not. Throwing it out of the policy is what turns
/// every game played into a check of the invariant.
/// </remarks>
public sealed class AmbiguousPromptException(Prompt asked, int id)
    : Exception(
        $"'{asked.Label.Trim()}' offers two options with id {id} — "
        + string.Join(
            ", ",
            asked.Affordances.Select(option => $"{option.Id}:{option.Verb}@{option.AnchorId}")))
{
}
