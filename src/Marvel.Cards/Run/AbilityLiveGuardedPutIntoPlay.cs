using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Recognizes a bound card put into play, guarded by its own in-play test, that resolves on the live board after opaque defeat work.</summary>
/// <remarks>
/// The shape is "if the bound card is in play, put it into play", such as Get Over Here!'s
/// "if you have AERIAL and the chosen card is in play, put it into play engaged with you".
/// rr:ability.4: "resolve the ability one sentence at a time", and
/// rr:when-defeated-abilities.2.1: "A defeated card leaves play after its 'When Defeated'
/// ability is resolved", so the defeat window, recovery interrupt or encounter deal the
/// damage opened finishes before the test is read. Ruling 2c98d8b065c9e8b0 orders Hydra
/// Soldier's deal, any reshuffle of an emptied encounter deck and the soldier's discard
/// before the next instruction.
/// The test reads the bound incarnation on the live board when it resolves, and the
/// instruction runs only when that test holds. rr:in-play-and-out-of-play.3: "A card enters
/// play when it moves from an out-of-play area to a play area", so a card already in play
/// only moves between play areas: nothing enters play and no choice is asked. A card the
/// opaque work moved while out of play, such as an encounter card a deal or reshuffle
/// moved (rr:in-play-and-out-of-play.9: "Cards in the encounter deck, encounter discard
/// pile, ... and facedown encounter cards dealt to a player are out of play"), fails the
/// test and is never put into play. The board after the instruction is not projected, so
/// a later read of it still raises.
/// </remarks>
internal static class AbilityLiveGuardedPutIntoPlay
{
    internal static bool Resolves(AbilityEffect node) => node is AbilityEffect.Conditional
    {
        Else: null,
        Then: AbilityEffect.PutIntoPlay { Card: AbilityCardSelection.Bound card },
        Test: var test,
    } && Guards(test, card);

    private static bool Guards(AbilityCondition test, AbilityCardSelection card) => test switch
    {
        AbilityCondition.IsInPlay inPlay => inPlay.Card == card,
        AbilityCondition.All all => all.Operands.Any(operand => Guards(operand, card)),
        _ => false,
    };
}
