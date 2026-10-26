using Marvel.Rules.Timing;

namespace Marvel.Rules.State;

/// <summary>
/// A hero or ally thwarting a scheme — <c>rr:thwart.1</c>.
/// </summary>
/// <remarks>
/// <para>
/// The thwart's half of <see cref="CharacterAttack"/>, and it exists for the
/// same reason that one does: something has to survive between the moment the
/// power is used and the moment its threat comes off, because a window sits in
/// between and a window can ask.
/// </para>
/// <para>
/// <b>Why a thwart needs one at all.</b> <c>rr:thwart</c> lists no numbered
/// steps the way <c>rr:attack-player-ability-type</c> does, so the case for a
/// window around it is made elsewhere: <c>rr:consequential-damage.1</c> puts an
/// ally's consequential damage "after resolving abilities that are triggered by
/// the ally <b>attacking or thwarting</b>". Abilities triggered by the ally
/// thwarting are abilities in a window, and the rule is written as though both
/// halves have one.
/// </para>
/// </remarks>
/// <param name="Thwarter">
/// The object id of the character thwarting. Not the seat, for
/// <c>rr:you-your.15</c>'s reason: an ally's thwart is not performed by that
/// player's identity, so a card acting on "the thwarting character" needs the
/// character itself.
/// </param>
/// <param name="Scheme">The scheme object id, or -1 for a labeled ability resolving only an independent sentence.</param>
/// <param name="Player">
/// The seat whose turn it is. <c>rr:you-your.6</c> is why this travels beside
/// the thwarter: an ability triggering "after <b>you</b> thwart" is about the
/// player, and a card in their play area reads it.
/// </param>
/// <param name="Amount">Fixed card-ability threat removal, or -1 for the thwart statistic.</param>
/// <param name="Source">The ability card, or -1 when it is the thwarter.</param>
/// <param name="Trigger">Event-stream provenance for the card ability.</param>
/// <param name="Continuation">Opaque interpreter work resumed when the power resolves.</param>
/// <param name="Origin">The containing occurrence whose response may own this nested power.</param>
/// <param name="SurgeGained">Whether the containing reveal has already gained Surge.</param>
/// <param name="ImminentThreat">The outer assignment an interrupt can prevent.</param>
/// <param name="TargetIncarnation">
/// The scheme copy that was chosen. Object ids survive leaving play, so this
/// distinguishes that target from the same physical card entering play again.
/// </param>
public sealed record CharacterThwart(
    int Thwarter,
    int Scheme,
    int Player,
    long Amount = -1,
    int Source = -1,
    string Trigger = "Thwart",
    CardPowerContinuation? Continuation = null,
    Occurrence? Origin = null,
    bool SurgeGained = false,
    ThreatPlacement? ImminentThreat = null,
    int TargetIncarnation = -1)
{
    /// <summary>Whether an interrupt substitutes ATK for this thwart's THW.</summary>
    public bool UsesAttack { get; init; }

    /// <summary>Whether this power reads its acting character's THW statistic.</summary>
    public bool UsesThwartStat { get; init; } = true;
}
