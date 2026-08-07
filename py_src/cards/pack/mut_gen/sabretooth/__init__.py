from cards.pack import *

ROBERT_KELLY_FINDER = CardFinder(name="Robert Kelly", card_type=Ally)

def RobertKellyCannotBeHealedByPlayerCardEffectsAndCannotHaveUpgradesAttached() -> List[Ability]:
    return AbilityFactory.UnitCannotHaveUpgradeAttached(ROBERT_KELLY_FINDER)

def IfRobertKellyLeavesPlayThePlayersLoseTheGame():

    def if_robert_kelly_leaves_play(effect: 'Effect', message: 'Message.WhenCardLeavePlay') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        Worlds.SetGameOver(False, effect)

    return AbilityFactory.WhenCardLeavePlay(
        AbilityType.NonKeywordBold,
        ROBERT_KELLY_FINDER,
        if_robert_kelly_leaves_play
    )

