using Marvel.Rules.State;

namespace Marvel.Sim;

/// <summary>Creates one game's policy from card facts and per-seat policy seeds.</summary>
internal delegate ISimulationPolicy SimulationPolicyFactory(
    ICardFacts facts, IReadOnlyList<uint> seatSeeds);
