using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>One exact pair of schemes and the card instruction moving threat between them.</summary>
public sealed record ThreatMoveRequest(Card? From, Card? To, long Amount,
    Card? Source, string Trigger, int Player = -1)
{
    // rr:leaves-play.1: a card that leaves play is "a new copy of the card".
    internal int FromIncarnation { get; } = From?.Incarnation ?? -1;
    internal int ToIncarnation { get; } = To?.Incarnation ?? -1;
}
