from . import *

# * Jefferson Davis

def GetAbilities() -> Sequence['Ability']:

    def jefferson_davis(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            jefferson_davis
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Scheme2, least_threat=True),
    ]

