using Marvel.View;

namespace Marvel.Godot;

/// <summary>Introduces an accepted attack before its consequences, without adding a historical event.</summary>
internal static class EventPlaybackPlan
{
    internal static IReadOnlyList<EventPresentation> From(IReadOnlyList<EventPresentation> entries,
        DecisionReceiptContext? receipt = null, WorldDescriptor? prior = null)
    {
        entries = WithAttackIntent(entries, prior?.ActiveAttack);
        if (WithDetails(entries).Any(entry => entry.Activity?.AttackBoundary == true)) return entries;
        if (BasicAttack(receipt, prior) is { } basic)
        {
            return [IdentifyBasicAttack(basic, entries), .. entries];
        }
        if (prior?.ActiveAttack is not null) return entries;
        return CompletedAttack(entries) is { } completed ? [completed, .. entries] : entries;
    }

    private static EventPresentation IdentifyBasicAttack(EventPresentation basic, IReadOnlyList<EventPresentation> entries)
    {
        var identity = WithDetails(entries).FirstOrDefault(entry => entry.Activity?.AttackIdentity is not null)?.Activity?.AttackIdentity;
        return basic with { Activity = basic.Activity! with { AttackIdentity = identity } };
    }

    private static EventPresentation[] WithAttackIntent(IReadOnlyList<EventPresentation> entries,
        ActiveAttackDescriptor? prior)
    {
        var names = new Dictionary<Marvel.Rules.Events.AttackIdentity, string>();
        if (prior?.Identity is { } identity) names[identity] = $"{prior.Source.Name} → {prior.Target.Name}";
        foreach (EventPresentation entry in WithDetails(entries))
            if (entry.Activity is { AttackIdentity: { } id, AttackBoundary: not null } activity)
                names[id] = string.Join(" → ", activity.Subjects.Select(subject => subject.Name));
        return entries.Select(entry => entry.Activity?.AttackIdentity is { } id
            ? entry with { Intention = names.GetValueOrDefault(id) } : entry).ToArray();
    }

    private static EventPresentation? CompletedAttack(IReadOnlyList<EventPresentation> entries)
    {
        EventPresentation[] attacks = [.. WithDetails(entries).Where(entry => entry.Activity?.Operation == "Attack_Completed")];
        // A single completed attack establishes one unambiguous accepted source/target.
        // Several activations retain their engine order rather than guessing their effect boundaries.
        if (attacks.Length != 1 || attacks[0].Activity is not { } attack) return null;
        ActivitySubject? source = attack.Subjects.FirstOrDefault(subject => subject.Role == ActivitySubjectRole.Source);
        ActivitySubject? target = attack.Subjects.FirstOrDefault(subject => subject.Role == ActivitySubjectRole.Target);
        if (source is null || target is null) return null;
        string caption = $"{source.Name} → {target.Name}";
        var introduction = new EventPresentation(caption, "Accepted attack", attacks[0].Anchors, EventMotionKind.Attack)
        {
            CueSummary = caption,
            Activity = attack with { Quantities = [], Operation = "Attack_Presentation" },
        };
        return introduction;
    }
    private static EventPresentation? BasicAttack(DecisionReceiptContext? receipt, WorldDescriptor? prior)
    {
        if (receipt is not { ActionVerb: "Attack", SourceCardId: { } source, TargetCardIds.Count: 1 }
            || prior is null) return null;
        int target = receipt.TargetCardIds[0];
        var sourceCard = prior.Areas.SelectMany(area => area.Cards).FirstOrDefault(card => card.Id == source);
        var targetCard = prior.Areas.SelectMany(area => area.Cards).FirstOrDefault(card => card.Id == target);
        if (sourceCard?.Face is null || targetCard?.Face is null) return null;
        string caption = $"{sourceCard.Face.Title} → {targetCard.Face.Title}";
        return new EventPresentation(caption, "Accepted attack", [source, target], EventMotionKind.Attack)
        {
            CueSummary = caption,
            Activity = new EventActivityDescriptor([
                new(source, sourceCard.Face.Title, ActivitySubjectRole.Source),
                new(target, targetCard.Face.Title, ActivitySubjectRole.Target)], [])
                { Operation = "Attack_Presentation" },
        };
    }

    private static IEnumerable<EventPresentation> WithDetails(IEnumerable<EventPresentation> entries)
    {
        foreach (EventPresentation entry in entries)
        {
            yield return entry;
            foreach (EventPresentation detail in WithDetails(entry.Details)) yield return detail;
        }
    }
}

