using static Marvel.Cards.Run.AbilityAdmission;
namespace Marvel.Cards.Run;

// One projected board supplies all membership observations for a selector.
internal sealed record AbilitySelectorTraceContext(
    int CurrentVillain, AbilityAdmissionScope Cast, HashSet<int> Discarded,
    Dictionary<int, HashSet<string>> Traits,
    Dictionary<(int Card, string Field), long> Modifiers,
    Dictionary<int, int> Engagement);
