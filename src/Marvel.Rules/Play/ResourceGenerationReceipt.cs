using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Resources published only by the admitted generator after its costs complete.</summary>
public sealed class ResourceGenerationReceipt
{
    private readonly World world;
    private readonly AbilityExecution owner;
    private string? resources;

    /// <summary>Bind the result to this game's currently executing generator.</summary>
    public ResourceGenerationReceipt(World world, AbilityExecution owner)
    {
        world.Agenda.Executions.RequireActive(owner);
        this.world = world;
        this.owner = owner;
    }

    /// <summary>Whether the generator has committed its resource production.</summary>
    public bool IsComplete => resources is not null;
    /// <summary>The generated resources; unavailable while generator costs are pending.</summary>
    public string Resources => resources ?? throw new InvalidOperationException("resource generation is unfinished");

    /// <summary>Publish resources once from the exact live generator invocation.</summary>
    public void Complete(World world, AbilityExecution owner, string resources)
    {
        ArgumentNullException.ThrowIfNull(resources);
        if (!ReferenceEquals(this.world, world) || !ReferenceEquals(this.owner, owner) || IsComplete)
            throw new InvalidOperationException("resource generation requires its unused owning invocation");
        world.Agenda.Executions.RequireActive(owner);
        this.resources = resources;
    }

    internal void RequireOwner(World world, AbilityExecution batch)
    {
        if (!ReferenceEquals(this.world, world))
            throw new InvalidOperationException("resource receipt belongs to another game");
        for (var current = owner; current is not null; current = current.Parent)
            if (ReferenceEquals(current, batch)) return;
        throw new InvalidOperationException("resource receipt belongs to another generator invocation");
    }

}
