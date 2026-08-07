from . import *

# * Champions Mobile Bunker

def GetAbilities() -> Sequence['Ability']:

    def champions_mobile_bunker(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        for target in effect.targets:
            player = target.GetControlByPlayer()
            player.DrawUp(2, effect)
            player.DiscardHandCards(("Zero", 2), effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            champions_mobile_bunker
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Identity, trait="CHAMPION"),
    ]

