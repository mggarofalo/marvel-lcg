using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Norman Osborn's printed forced damage replacement.</summary>
public static class GreenGoblinDamageReplacement
{
    /// <summary>Project the printed replacement without executing its counter removal.</summary>
    public static DamageProjection? Preview(World world, Card target, long amount)
    {
        if (amount <= 0 || !DeckTypes.IsInPlay(target.Area.Type)
            || target.FaceId is not ("02001a" or "02002a" or "02003a"))
            return null;
        if (!world.Cards.Any(card => card.FaceId == "02006a"
                && DeckTypes.IsInPlay(card.Area.Type)))
            return new(new RuleProjection<long>.Unsupported(
                "Norman Osborn's damage replacement requires Criminal Enterprise"));
        return new(0, "Norman Osborn removes infamy instead of taking this damage");
    }

    /// <summary>Offer Norman's printed interrupt in the ordinary damage replacement batch.</summary>
    public static PendingAbility? Waiting(World world, DamageReplacement replacement)
    {
        Card target = replacement.Target;
        return replacement.CurrentAmount > 0
            && target.Incarnation == replacement.TargetIncarnation
            && DeckTypes.IsInPlay(target.Area.Type)
            && target.FaceId is ("02001a" or "02002a" or "02003a")
            ? new PendingAbility(target.ObjectId, AbilityType.ForcedInterrupt, -1, 1)
            {
                Native = NativeAbilityKind.GreenGoblinDamageReplacement,
                Would = true,
            }
            : null;
    }

    /// <summary>Describe the current whole-instance replacement.</summary>
    public static Affordance Describe(World world, PendingAbility ability, DamageReplacement replacement)
    {
        string label = $"Remove up to {replacement.CurrentAmount} infamy from Criminal Enterprise instead of taking damage";
        return new(ability.OfferId ?? ability.Card, "Remove_Counter", ability.Card,
            ability.Player, label, Description: label)
        { Timing = ability.Type, DisplayLabel = label, CommitLabel = label };
    }

    /// <summary>Consume the current damage instance and remove available counters.</summary>
    public static AbilityExecutionResult Resolve(
        World world, AbilityExecution invocation, DamageReplacement replacement)
    {
        Card target = replacement.Target;
        if (target.FaceId is not ("02001a" or "02002a" or "02003a"))
            throw new InvalidOperationException("Norman damage replacement is no longer eligible");
        Card environment = world.Cards.SingleOrDefault(card =>
            card.FaceId == "02006a" && DeckTypes.IsInPlay(card.Area.Type))
            ?? throw new RulesNotImplementedException(
                $"{target.FaceId} damage replacement requires in-play 02006a");
        long amount = replacement.CurrentAmount;
        var events = new List<GameEvent>();
        // 02001a–02003a: "When Norman Osborn would take any amount of damage,
        // remove that many infamy counters ... instead." rr:replacement-effect.1
        // replaces the whole damage instance; no excess damages the new face.
        GreenGoblinTransition.RemoveCounters(world, environment, "infamy", amount,
            Steps.DamageWouldBeDealt, events);
        replacement.Replace(invocation, 0);
        invocation.Occurrence.Resolution.Resolve(invocation.Ability);
        return invocation.Finish(events);
    }
}
