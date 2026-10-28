using System.Collections.Immutable;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Schedules responses to physical cards spent in one completed cost.</summary>
public static class ResourceSpentWindow
{
    /// <summary>The completed physical-card-spend condition.</summary>
    public const string Condition = "WhenResourceCardSpent";

    /// <summary>Open a shared response window when a completed cost has an eligible response.</summary>
    public static bool Open(World world, IReadOnlyList<SpentResourceCard> cards, int player)
    {
        if (cards.Count == 0) return false;
        var agenda = world.Agenda;
        var captured = cards.ToImmutableArray();
        var probe = new Occurrence(0, [Condition], Player: player) { SpentResourceCards = captured };
        if (world.WindowAbilities.Waiting(world, probe, WindowKind.Response).Count == 0) return false;
        var occurrence = new Occurrence(agenda.nextPlayerActionOccurrence--, [Condition], Player: player)
            { SpentResourceCards = captured };
        var step = new PhaseStep(new CompletedCardEffectOperation { Occurrence = occurrence },
            agenda.Current?.Round ?? 0, 0);
        agenda.items.Insert(0, agenda.Frame(step, Stage.Responses, occurrence));
        agenda.scheduled = 0;
        return true;
    }
}
