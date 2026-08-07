from cards.pack import *

def AfterTheinfinityStoneDeckRunsOutGiveThisFacedownBoostCard(num: int) -> 'Ability':
    def action(effect: 'Effect', message: 'Message.AfterDeckRunOut') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        Faces.GiveFacedownBoostCards([this], num, effect)

    return AbilityFactory.AfterDeckRunOut(
        AbilityType.ForcedResponse,
        lambda effect: GetInfinityStone(effect).deck,
        action
    )

from cards.pack.mts.infinity_gauntlet import GetInfinityStone
Unused(GetInfinityStone)
