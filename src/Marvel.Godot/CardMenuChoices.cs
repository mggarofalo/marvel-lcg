using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Arranges current source offers and authorized targets into local menu choices.</summary>
internal static class CardMenuChoices
{
    internal static IReadOnlyList<CardMenuEntry> Build(DecisionComposer composer,
        WorldDescriptor world, int source, IReadOnlyList<AffordancePresentation> offers) =>
        [.. offers.Where(offer => offer.CardAnchorId == source && offer.SelectionSourceId != source)
            .Select(offer => Action(composer, world, offer)),
            .. Group(world, [.. offers.Where(offer => offer.SelectionSourceId == source
                    && offer.CardAnchorId is not null)
                .Select(offer => Selection(composer, world, offer))])];

    private static CardMenuEntry Selection(DecisionComposer composer, WorldDescriptor world, AffordancePresentation offer)
    {
        var entry = Action(composer, world, offer);
        return entry with { Preview = offer.CardAnchorId,
            Label = EncounterTargetLabel(world, offer.CardAnchorId!.Value, entry.Label) };
    }

    private static CardMenuEntry Action(DecisionComposer composer, WorldDescriptor world,
        AffordancePresentation offer)
    {
        string label = DecisionCopy.Choice(offer);
        string detail = DecisionCopy.CompactActionSummary(offer);
        if (offer.Illegal is { } reason)
            return new(label, reason, null, [], Disabled: true);
        if (!offer.PlaysCard && offer.TargetRequest is { IsGrouped: false, AllowRepeated: false,
                IsSearch: false, Max: 1, Legal.Count: > 0 } request)
        {
            CardMenuEntry[] targets = [.. request.Legal.Select(id => Target(composer, world, offer, id))];
            return new(label, detail, null, Group(world, targets));
        }
        bool ready = offer.TargetRequest is not { Legal.Count: > 0 } && Ready(composer, offer.Id, null);
        return new(ready ? offer.CommitLabel ?? label : label + "…", detail,
            new(offer.Id, null, ready), []);
    }

    private static CardMenuEntry Target(DecisionComposer composer, WorldDescriptor world,
        AffordancePresentation offer, int target)
    {
        var windowChoice = offer.WindowChoices.FirstOrDefault(choice => choice.TargetId == target);
        string label = windowChoice?.Label ?? TargetName(world, target, offer.TargetRequest!.Legal);
        label = EncounterTargetLabel(world, target, label);
        string detail = windowChoice?.Description ?? offer.TargetRequest?.Details?.GetValueOrDefault(target)
            ?? DecisionCopy.CompactActionSummary(offer);
        bool ready = Ready(composer, offer.Id, target);
        return new(label, TargetCommitmentDetail(detail, ready),
            new(offer.Id, target, ready), [], Preview: target);
    }

    private static string TargetCommitmentDetail(string detail, bool ready) =>
        string.IsNullOrWhiteSpace(detail) ? (ready ? "Commit now" : "Choose target")
            : (ready ? "Commit: " : "Choose: ") + detail;

    private static string EncounterTargetLabel(WorldDescriptor world, int target, string label)
    {
        var group = world.EncounterGroups.FirstOrDefault(candidate => candidate.Villain == target);
        if (group is null) return label;
        string state = group.Active ? "active villain" : group.Defeated ? "defeated villain" : "inactive villain";
        return $"{label} · {state}";
    }

    private static string TargetName(WorldDescriptor world, int target, IReadOnlyList<int> legal)
    {
        CardDescriptor? card = Cards(world).FirstOrDefault(card => card.Id == target);
        string label = card?.Face?.Title ?? "Offered target";
        PlayerSummaryDescriptor? player = world.PlayerSummaries.FirstOrDefault(player => player.Identity == target);
        if (player is not null) return $"Player {player.Seat + 1} · {label}";
        CardDescriptor[] copies = [.. Cards(world).Where(candidate => legal.Contains(candidate.Id ?? -1))
            .Where(candidate => candidate.Face?.Title == label).DistinctBy(candidate => candidate.Id)];
        return copies.Length <= 1 ? label : $"{label} · copy {Array.FindIndex(copies, copy => copy.Id == target) + 1}";
    }

    private static List<CardMenuEntry> Group(WorldDescriptor world, CardMenuEntry[] targets)
    {
        var result = new List<CardMenuEntry>();
        var engaged = world.Relationships.Where(relation => relation.Kind == RelationshipKind.Engagement)
            .Where(relation => relation.Seat is not null).ToDictionary(relation => relation.Subject, relation => relation.Seat!.Value);
        result.AddRange(targets.Where(target => !engaged.ContainsKey(target.Preview!.Value)));
        foreach (var group in targets.Where(target => engaged.ContainsKey(target.Preview!.Value))
                     .GroupBy(target => engaged[target.Preview!.Value]).OrderBy(group => group.Key))
            result.Add(new($"Player {group.Key + 1} minions", "", null, [.. group]));
        return result;
    }

    internal static bool Ready(DecisionComposer composer, int offer, int? target)
    {
        var trial = new DecisionComposer(composer.Prompt);
        var operations = new TableDraftOperations(trial);
        if (!operations.TrySelectAffordance(offer)) return false;
        if (target is { } id && !trial.Targets.Contains(id) && !operations.TryAddTarget(id)) return false;
        return DecisionCommitmentAssessment.IsReadyWithoutPaymentChoice(trial);
    }

    internal static bool Select(DecisionComposer composer, TableDraftBinding binding, CardMenuChoice choice)
    {
        if (!binding.IsCurrent() || !composer.Prompt.Affordances.Any(offer => offer.Id == choice.Offer && offer.IsLegal))
            return false;
        if (!CanSelect(composer, choice)) return false;
        if (!binding.TrySelectAffordance(choice.Offer)) return false;
        return choice.Target is not { } id || composer.Targets.Contains(id) || binding.TryAddTarget(id);
    }

    private static bool CanSelect(DecisionComposer composer, CardMenuChoice choice)
    {
        var trial = new DecisionComposer(composer.Prompt);
        var operations = new TableDraftOperations(trial);
        if (!operations.TrySelectAffordance(choice.Offer)) return false;
        if (choice.Target is { } target && !trial.Targets.Contains(target) && !operations.TryAddTarget(target)) return false;
        return !choice.Commit || DecisionCommitmentAssessment.IsReadyWithoutPaymentChoice(trial);
    }

    private static IEnumerable<CardDescriptor> Cards(WorldDescriptor world) =>
        world.Areas.SelectMany(area => area.Cards);
}
