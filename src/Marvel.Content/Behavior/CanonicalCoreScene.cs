using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>A legal Core Set deal from which one behavioral transcript begins.</summary>
public sealed record CoreSceneRequest(
    string Authority,
    string Campaign,
    IReadOnlyList<string> Heroes,
    uint Seed,
    IReadOnlyList<string>? ModularSets = null,
    IReadOnlyList<string>? PlayerDecks = null);

/// <summary>One physical card selected by printed face and zero-based copy number.</summary>
public sealed record SceneCard(string FaceId, int Copy = 0);

/// <summary>The legal places the behavioral state vocabulary may arrange directly.</summary>
public enum SceneZone
{
#pragma warning disable CS1591, SA1602
    PlayerDeck,
    PlayerHand,
    PlayerDiscard,
    Ally,
    Support,
    Upgrade,
    Attachment,
    EngagedMinion,
    Obligation,
    EncounterDeck,
    EncounterDiscard,
    SideScheme,
    Environment,
    SetAside,
#pragma warning restore CS1591, SA1602
}

/// <summary>A typed destination; <see cref="Seat"/> is required for player places.</summary>
public sealed record SceneDestination(SceneZone Zone, int Seat = World.Scenario, int Host = -1);

/// <summary>One deterministic arrangement applied after the legal deal.</summary>
public abstract record CoreSceneOperation
{
    /// <summary>The stable operation name included in a construction failure.</summary>
    public abstract string Name { get; }
}

/// <summary>Moves one existing physical card without changing its ownership.</summary>
public sealed record MoveSceneCard(SceneCard Card, SceneDestination Destination)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "move-card";
}

/// <summary>Replaces the current villain with one later stage from its legal deck.</summary>
public sealed record SetSceneVillain(SceneCard Card) : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "set-villain";
}

/// <summary>Where unselected draw-pile cards go while arranging a deck boundary.</summary>
public enum PlayerDeckRemainder
{
#pragma warning disable CS1591, SA1602
    Leave,
    Discard,
    Hand,
#pragma warning restore CS1591, SA1602
}

/// <summary>Stacks selected cards on a player's deck; the first id is the next card drawn.</summary>
public sealed record StackPlayerDeck(
    int Seat,
    IReadOnlyList<SceneCard> TopFirst,
    PlayerDeckRemainder Remainder = PlayerDeckRemainder.Leave)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "stack-player-deck";
}

/// <summary>Places exactly the selected player-deck cards in a player's hand.</summary>
public sealed record SetPlayerHand(
    int Seat,
    IReadOnlyList<SceneCard> Cards)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "set-player-hand";
}

/// <summary>Where unselected encounter cards go while arranging a deck boundary.</summary>
public enum EncounterDeckRemainder
{
#pragma warning disable CS1591, SA1602
    Leave,
    Discard,
    Dealt,
#pragma warning restore CS1591, SA1602
}

/// <summary>Stacks selected encounter cards; the first id is the next card drawn.</summary>
public sealed record StackEncounterDeck(
    IReadOnlyList<SceneCard> TopFirst,
    EncounterDeckRemainder Remainder = EncounterDeckRemainder.Leave,
    int Seat = 0)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "stack-encounter-deck";
}

/// <summary>Sets the damage already on an in-play character.</summary>
public sealed record SetSceneDamage(SceneCard Card, long Damage)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "set-damage";
}

/// <summary>Sets scheme threat or one printed all-purpose counter type to an exact value.</summary>
public sealed record SetSceneCounters(SceneCard Card, string Type, long Count)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "set-counters";
}

/// <summary>Sets rules-provided acceleration tokens beside the main scheme.</summary>
public sealed record SetSceneAccelerationTokens(long Count) : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "set-acceleration-tokens";
}

/// <summary>Shows one of the selected player's two printed identity faces.</summary>
public sealed record SetSceneForm(int Seat, string FaceId)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "set-form";
}

/// <summary>Sets whether an in-play card is ready.</summary>
public sealed record SetSceneReady(SceneCard Card, bool Ready)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "set-ready";
}

/// <summary>Creates one rules-provided status card on an in-play character.</summary>
public sealed record GiveSceneStatus(SceneCard Host, string Status)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "give-status";
}
