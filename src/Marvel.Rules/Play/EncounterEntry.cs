using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Places encounter cards between their entry interrupt and response windows.</summary>
public static class EncounterEntry
{
    /// <summary>Schedule entry before the containing ability resumes.</summary>
    public static void Schedule(World world, EncounterEntryRequest request,
        EffectCompletion completion, PhaseStep? continuation = null)
    {
        var current = world.Agenda.Current;
        var entry = new PhaseStep(new EncounterEntryOperation(request, completion),
            current?.Round ?? 0, current?.Number ?? 0, Subject: request.Card, Seat: request.Player);
        var steps = continuation is { } resume ? new[] { entry, resume } : [entry];
        if (current is not null) world.Agenda.Now(steps);
        else foreach (var step in steps) world.Agenda.Add(step);
    }

    internal static bool IsCurrent(World world, EncounterEntryRequest request) =>
        request.Card >= 0 && request.Card < world.Cards.Count
        && world.Cards[request.Card].Incarnation == request.Incarnation
        && world.Cards[request.Card].Area.Id == request.Area;

    internal static bool WillEnter(World world, Card card, int player, CardEntryPlacement? placement = null)
    {
        if (DeckTypes.IsInPlay(card.Area.Type) || Uniqueness.IsBlocked(world, world.Facts, card)) return false;
        return EffectiveCards.Kind(card, world.Facts) switch
        {
            CardKind.Minion or CardKind.Environment or CardKind.EncounterSideScheme => true,
            CardKind.Obligation => RevealKeywords.Names(world, world.Facts, card) is null or >= 0,
            CardKind.Attachment => placement?.AttachmentTarget is { } host ? HostInPlay(world, host)
                : world.PlacementAbilities.AttachmentTargets(world, card, player) is { Count: > 0 },
            CardKind.Ally or CardKind.Support => placement?.SetupController is not null,
            _ => false,
        };
    }

    private static bool HostInPlay(World world, int host) => host >= 0 && host < world.Cards.Count
        && DeckTypes.IsInPlay(world.Cards[host].Area.Type);

    internal static Prompt? Apply(World world, PhaseStep step, List<GameEvent> events)
    {
        var operation = step.Require<EncounterEntryOperation>();
        var request = operation.Entry;
        if (!IsCurrent(world, request)
            || request.Placement.AttachmentTarget is { } host && !HostInPlay(world, host))
        {
            operation.Completion.Complete(0, 0);
            return null;
        }
        var card = world.Cards[request.Card];
        var occurrence = world.Agenda.Occurrence
            ?? throw new InvalidOperationException("encounter entry has no occurrence");
        if (DeckTypes.IsInPlay(card.Area.Type))
        {
            bool engaged = MinionEngagement.Transfer(world, card, request.Player, occurrence, events);
            operation.Completion.Complete(0, engaged ? 1 : 0);
            if (engaged)
            {
                MinionEntryActivations.Quickstrike(world, world.Facts, card, request.Player, step.Round);
                world.Agenda.BeforeResponses(step, occurrence);
            }
            return null;
        }
        Reveal.Resolve(world, world.Facts, card, request.Player, events,
            occurrence, request.Placement);
        operation.Completion.Complete(0, DeckTypes.IsInPlay(card.Area.Type) ? 1 : 0);
        if (occurrence.Is(MinionEngagement.Engaged))
        {
            EncounterRevealProcedure.FinishEncounterRevealTail(
                world, card, request.Player, step, occurrence, events);
            world.Agenda.BeforeResponses(step, occurrence);
        }
        return null;
    }
}
