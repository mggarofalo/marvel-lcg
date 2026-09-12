using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Server;

/// <summary>A one-time bearer invitation to one server-authorized seat.</summary>
public sealed record SeatInvitation(int Seat, string Invitation);
