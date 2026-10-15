namespace Marvel.Rules.State;

/// <summary>Opaque continuation payload carried by a card-labelled basic power.</summary>
public sealed record CardPowerContinuation(
    int AbilityIndex, int PowerOrdinal, bool FinalStep,
    IReadOnlyList<int> Targets, IReadOnlyList<string> AbilityPath,
    string AbilityFace, IReadOnlyDictionary<string, long> AbilityResults,
    IReadOnlyList<int> Discarded,
    bool EachPlayerFrame, bool FinalPlayer, int AbilityPlayer);
