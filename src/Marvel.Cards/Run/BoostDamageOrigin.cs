using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>The authoritative activation captured when a boost ability begins.</summary>
internal sealed class BoostDamageOrigin
{
    private readonly EnemyActivation activation;

    private BoostDamageOrigin(EnemyActivation activation) => this.activation = activation;

    internal static BoostDamageOrigin? Capture(World world) =>
        world.Activation is { } active ? new(active) : null;

    // rr:activation.8: a new activation "resolves after the current activation
    // has finished resolving". The captured cause survives that boundary, but
    // new damage cannot join its frozen receipt or a different live activation.
    // This reference and nullable accounting-id representation are engine choices.
    internal int? AccountingId(World world) =>
        world.Activation?.Id == activation.Id ? activation.Id : null;
}
