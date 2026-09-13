using Marvel.Rules.Timing;

namespace Marvel.Rules.State;

/// <summary>Opaque continuation payload carried by a card-labelled basic power.</summary>
public sealed record CardPowerContinuation(
    int AbilityIndex, int PowerOrdinal, int ResumeFrom, bool FinalStep,
    IReadOnlyList<int> Targets, bool SurgeGained, IReadOnlyList<string> AbilityPath,
    string AbilityFace, IReadOnlyDictionary<string, long> AbilityResults,
    Occurrence AbilityOccurrence, IReadOnlyList<int> Discarded,
    bool EachPlayerFrame, bool FinalPlayer, int AbilityPlayer,
    bool AbilityHasContinuation);
