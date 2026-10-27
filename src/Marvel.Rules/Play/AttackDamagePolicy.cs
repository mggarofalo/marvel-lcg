using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Attack-keyword consequences fixed before damage placement changes their sources.</summary>
internal sealed record AttackDamagePolicy(bool Overkill, bool Retaliate)
{
    internal ExcessDamageThreat? ExcessThreat { get; init; }

    internal static AttackDamagePolicy Capture(World world, AttackDamageRequest request, bool spillOnly = false) =>
        new(!spillOnly && (request.GrantedOverkill
                || AttackKeywords.Has(world, request.Attacker, request.Source, Keywords.Overkill, request.Basic)),
            !spillOnly && request.Retaliate
                && !AttackKeywords.Has(world, request.Attacker, request.Source, Keywords.Ranged, request.Basic))
        { ExcessThreat = spillOnly ? null : ExcessDamageThreat.Capture(world, request) };
}
