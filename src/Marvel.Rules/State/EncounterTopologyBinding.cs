namespace Marvel.Rules.State;

/// <summary>Installs validated scenario encounter membership before play begins.</summary>
public static class EncounterTopologyBinding
{
    /// <summary>Binds immutable ownership once, to the world whose components it describes.</summary>
    public static void BindEncounterTopology(this World world, EncounterTopology topology)
    {
        ArgumentNullException.ThrowIfNull(topology);
        if (!ReferenceEquals(topology.World, world) || world.EncounterTopology is not null)
            throw new InvalidOperationException("encounter topology must belong to this unbound world");
        world.EncounterTopology = topology;
    }
}
