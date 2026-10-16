using Marvel.Rules.Prompts;

namespace Marvel.PresentationProbe.Allowed;

public static class Allowed
{
    public static bool Accepts(TargetRequest targets, IReadOnlyList<int> selection) => targets.Allows(selection);
}
