from . import *

# Targeting Screen

def GetAbilities() -> Sequence['Ability']:

    def targeting_screen(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        if initiator.IsControl(CardFinder(name="Milano")):
            value = 3
        else:
            value = 2
        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            targeting_screen
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Scheme2),
    ]

