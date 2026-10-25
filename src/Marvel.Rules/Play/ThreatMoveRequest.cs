using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>One exact pair of schemes and the card instruction moving threat between them.</summary>
public sealed record ThreatMoveRequest(Card? From, Card? To, long Amount,
    Card? Source, string Trigger, int Player = -1);
