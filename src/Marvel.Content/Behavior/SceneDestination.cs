using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>A typed destination; <see cref="Seat"/> is required for player places.</summary>
public sealed record SceneDestination(SceneZone Zone, int Seat = World.Scenario, int Host = -1);
