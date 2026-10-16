using System.Collections.Immutable;

namespace Marvel.Cards.Run;

/// <summary>The active structural path and player scope within an invocation.</summary>
internal sealed record AbilityCursor(ImmutableArray<AbilityStructuralFrame> Frames,
    int Position, int Player, bool HasContinuation, bool FinalStep,
    bool FinalPlayer, bool EachPlayerFrame);
