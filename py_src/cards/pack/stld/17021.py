from . import *

# * C.I.T.T.

def GetAbilities() -> Sequence['Ability']:

    def citt(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            citt
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCost(Cost("2"))
        .SetTarget(Unit2, trait="GUARDIAN", canbe_ready=True)
    ]

