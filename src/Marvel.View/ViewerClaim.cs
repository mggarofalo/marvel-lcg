using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Marvel.Server")]

namespace Marvel.View;

/// <summary>What a client says it is displaying.</summary>
/// <param name="Seat">One seat, or null when the client did not name one.</param>
/// <param name="HotSeat">Whether one device is shared by the table.</param>
/// <param name="Watch">Whether the client asks to watch the table.</param>
/// <remarks>
/// This is a request, never authority. The server's <see cref="IVisibilityPolicy"/>
/// decides which of these seats the resulting session may actually see.
/// </remarks>
public sealed record ViewerClaim(int? Seat = null, bool HotSeat = false, bool Watch = false);
