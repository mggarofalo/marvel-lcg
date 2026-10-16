using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Admits resource generation into the same owned execution tree as other card text.</summary>
public static class ResourceAbilityEntry
{
    /// <summary>Run one generator, retaining its receipt while owned costs are pending.</summary>
    public static ResourceExecutionResult ExecuteResource(
        this IResourceCardAbilities abilities, World world, int player, int card)
    {
        ResourceExecutionResult? result = null;
        world.Agenda.Executions.RunBatch(
            new Occurrence(0, ["ResourceAbility"], Subject: card, Player: player),
            entry =>
            {
                result = abilities.UseResource(world, entry, player, card);
                result.Generation.RequireOwner(world, entry);
                return result.Execution;
            });
        return result ?? throw new InvalidOperationException("resource resolver returned no result");
    }
}
