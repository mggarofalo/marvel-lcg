using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Recognizes a status given to the villain on the live board after opaque defeat work.</summary>
/// <remarks>
/// rr:ability.4: "resolve the ability one sentence at a time." The grant follows the
/// sentence whose defeat window is unprojected, and rr:when-defeated-abilities.2.1:
/// "A defeated card leaves play after its 'When Defeated' ability is resolved", so
/// that window finishes before the grant begins. rr:confuse-confused.2: "If an ability
/// 'confuses' a character, give that character a confused status card." The grant
/// chooses nothing and asks nothing: it reads the villain when it resolves, and
/// status execution skips a recipient that is no longer in play. The board after
/// the grant is not projected, so a later read of it still raises.
/// </remarks>
internal static class AbilityLiveStatusGrant
{
    internal static bool Resolves(AbilityEffect node) => node is AbilityEffect.GiveStatus
    {
        Cards: AbilityCardSelection.Query { Kind: AbilityCardQuery.Villain },
    };
}
