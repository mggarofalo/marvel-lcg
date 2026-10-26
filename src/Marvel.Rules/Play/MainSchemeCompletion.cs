using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Settles scheme completion after all completion interrupts finish.</summary>
internal static class MainSchemeCompletion
{
    internal static void Check(World world, ICardFacts facts,
        IThreatCardAbilities abilities, Card scheme, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(events);

        long held = scheme.Tokens.GetValueOrDefault("k_threat");
        long target = StateFields.Modified(
            world, scheme, "target_threat", facts, world.Players);
        if (target <= 0 || held < target || scheme.Area.Type != DeckType.MainSchemesArea)
        {
            // A side scheme has a target threat value too, and reaching it does
            // nothing: `rr:side-scheme.2` keeps a side scheme in play "until there is no
            // threat on it", which is the opposite direction.
            return;
        }

        if (AlreadyCompleting(world, scheme)) return;

        // rr:when-completed-abilities.1: "Forced Interrupt: When this scheme
        // is completed...". Its effects finish before committing completion.
        if (abilities.ObservesMainSchemeCompletion(world, scheme))
        {
            Schedule(world, scheme);
            return;
        }

        Commit(world, facts, abilities, scheme, events);
    }

    private static void Schedule(World world, Card scheme)
    {
        var current = world.Agenda.Current;
        var completion = new PhaseStep(new MainSchemeCompletionOperation(scheme.Incarnation),
            current?.Round ?? 0, current?.Number ?? 0, Subject: scheme.ObjectId,
            Seat: world.FirstPlayer) { OccurrenceId = world.Agenda.nextPlayerActionOccurrence-- };
        if (current is null) world.Agenda.Add(completion);
        else world.Agenda.Now(completion);
    }

    private static bool AlreadyCompleting(World world, Card scheme) =>
        scheme.Tokens.GetValueOrDefault("is_completed") != 0
        || world.Agenda.Outstanding.Any(step => step.Subject == scheme.ObjectId
            && step.Operation is MainSchemeCompletionOperation);

    internal static void Commit(World world, ICardFacts facts,
        IThreatCardAbilities abilities, Card scheme, List<GameEvent> events)
    {
        scheme.PlaceTokens("is_completed", 1);
        events.Add(new FieldSet(scheme.ObjectId, "is_completed", 0, 1)
        {
            Trigger = "main scheme completed",
            Verb = "Complete",
        });

        if (world.AreaOf(DeckType.MainSchemesDeck).Cards.Count > 0)
        {
            MainScheme.Advance(world, facts, abilities, scheme, "main scheme completed", events);
            return;
        }

        world.Finish(Outcome.VillainWins);
    }
}
