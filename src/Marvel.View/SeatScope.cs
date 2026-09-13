using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Marvel.Server")]

namespace Marvel.View;

/// <summary>A server-authorized seat and its private-information scope.</summary>
public sealed record SeatScope(int Seat, ViewScope Scope);
