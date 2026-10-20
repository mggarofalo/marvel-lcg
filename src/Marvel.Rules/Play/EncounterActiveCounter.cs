using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Legality and committed movement of a scenario's active villain counter.</summary>
public static class EncounterActiveCounter
{
    /// <summary>Only a different living physical villain can receive the counter.</summary>
    public static bool CanMove(World world, Card destination)
    {
        var topology = Topology(world);
        // rr:move.1: "it cannot move to its same (current) placement".
        // rr:move.2: "If there is no valid source or destination for a move,
        // the move cannot be made."
        return topology.Active.CurrentVillain is { } source
            && !ReferenceEquals(source, destination)
            && topology.Groups.Any(group => ReferenceEquals(group.CurrentVillain, destination));
    }

    /// <summary>Moves the counter without changing an activation already in progress.</summary>
    public static bool Move(World world, Card destination, string trigger, List<GameEvent> events)
    {
        if (!CanMove(world, destination)) return false;
        Topology(world).SelectActive(destination, trigger, events);
        return true;
    }

    private static EncounterTopology Topology(World world) =>
        world.EncounterTopology ?? throw new RulesNotImplementedException(
            "active-counter movement requires encounter topology");
}
