using Marvel.View;

namespace Marvel.Godot;

/// <summary>Introduces an accepted attack before its consequences, without adding a historical event.</summary>
internal static class EventPlaybackPlan
{
    internal static IReadOnlyList<EventPresentation> From(IReadOnlyList<EventPresentation> entries,
        DecisionReceiptContext? receipt = null, WorldDescriptor? prior = null)
    {
        if (BasicAttack(receipt, prior) is { } basic) return [basic, .. entries];
        if (prior?.ActiveAttack is not null) return entries;
        return CompletedAttack(entries) is { } completed ? [completed, .. entries] : entries;
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

