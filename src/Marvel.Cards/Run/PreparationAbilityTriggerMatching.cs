using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Matches the retained completed-ability source rather than its current board location.</summary>
internal static class PreparationAbilityTriggerMatching
{
    internal static bool Matches(AbilityTrigger trigger, Occurrence occurrence)
    {
        if (trigger.Event is not { } condition || !occurrence.Is(condition)) return false;
        if (trigger.Also is { } also && !occurrence.Is(also)) return false;
        if (condition != PreparationAbilityResolvedOperation.Condition) return true;
        if (occurrence.ActorFacts?.PreparationAbility is not { } receipt) return false;
        return trigger.Subject != AbilitySubjects.You || receipt.Controller == occurrence.Player;
    }
}
