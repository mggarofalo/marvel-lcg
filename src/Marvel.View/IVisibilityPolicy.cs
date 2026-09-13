using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Marvel.Server")]

namespace Marvel.View;

/// <summary>Chooses the private seats a newly opened server session may see.</summary>
public interface IVisibilityPolicy
{
    /// <summary>Authorizes a client assertion against server-owned policy.</summary>
    ViewScope Authorize(ViewerClaim? claim, int players);

    /// <summary>Returns separately scoped seats that the opener may invite.</summary>
    IReadOnlyList<SeatScope> AdditionalScopes(ViewerClaim? claim, int players);
}
